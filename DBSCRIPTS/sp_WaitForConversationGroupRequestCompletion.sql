USE [InogTmp]
GO
/****** Object:  StoredProcedure [PPS].[sp_WaitForConversationGroupRequestCompletion]    Script Date: 25.12.2018 12:53:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [PPS].[sp_WaitForConversationGroupRequestCompletion] (@context UNIQUEIDENTIFIER, @timeOut INT = 300)
AS
BEGIN
 SET XACT_ABORT ON;
 SET NOCOUNT ON;

 DECLARE @metaID INT, @ctx Nvarchar(255), @cnt INT, @IsContextConversationHandle BIT, @RC INT, @isCanceled BIT;
 SET @ctx = Cast(@context as Nvarchar(255));
 SET @isCanceled=0;

 DECLARE  @ErrorMessage NVARCHAR(4000), @ErrorNumber INT;

 SELECT @metaID = MetaDataID, @cnt = RequestCount, @isCanceled = IsCanceled, @ErrorMessage = [Error],
   @IsContextConversationHandle=IsContextConversationHandle
   FROM [PPS].[MetaData] 
   WHERE [Context] = @context;

 IF (@isCanceled = 1 OR @ErrorMessage IS NOT NULL)
 BEGIN
    IF (@ErrorMessage IS NOT NULL)
	   RAISERROR (@ErrorMessage, 16,1);
	  ELSE
	   RAISERROR (N'The operation %s cancelled', 16,1, @ctx);

	RETURN;
 END;

DECLARE @cg UNIQUEIDENTIFIER;
DECLARE @ch UNIQUEIDENTIFIER;
DECLARE @msg VARBINARY(MAX);
DECLARE @messagetypename NVARCHAR(255);
DECLARE @startTime DATETIME, @elapsedTime DATETIME;
DECLARE @tempTbl TABLE
(
   RequestCompleted INT NOT NULL
);
DECLARE @i int, @exitWhile BIT;
SET @i=0; 
SET @exitWhile =0;
SET @startTime = GETDATE();
SET @elapsedTime =0;


WHILE(@exitWhile = 0)
BEGIN
 SET @cg = NULL;
 SET @ch = NULL;
 SET @messagetypename = NULL;
 SET @isCanceled = 0;
 SET @ErrorMessage = NULL;

 BEGIN TRY
  BEGIN TRANSACTION;
    WAITFOR (
     RECEIVE TOP(1)
      @cg = conversation_group_id,
      @ch = conversation_handle,
      @messagetypename = message_type_name,
      @msg = message_body
     FROM PPS_MessageSendQueue
     WHERE conversation_group_id = @context
     ), TIMEOUT 10000;

     IF (@ch IS NOT NULL)
     BEGIN
      END CONVERSATION @ch;
     END;
   COMMIT TRANSACTION;
  
   BEGIN TRANSACTION; 
   EXEC @RC = sp_getapplock @Resource =  @ctx, @LockMode = 'Exclusive', @LockTimeout= 5000;
   IF (@RC < 0)
   BEGIN
   RAISERROR (N'sp_getapplock failed to lock the resource: %s the reason: %d', 16, 1, @ctx, @RC);
   END
       
   SET @elapsedTime = DATEDIFF (ss, @startTime, GETDATE());

   SELECT @cnt = RequestCount, @isCanceled = IsCanceled, @ErrorMessage = [Error]
   FROM [PPS].[MetaData] 
   WHERE [Context] = @context;

   IF (@ErrorMessage IS NOT NULL)
   BEGIN
      RAISERROR (@ErrorMessage, 16,1);
   END
   ELSE IF (@isCanceled = 1)
   BEGIN
      RAISERROR (N'The operation %s cancelled', 16,1, @ctx);
   END      
   ELSE IF (@ch IS NULL)
   BEGIN
     -- NOOP CONTINUE
     SET @exitWhile = 0;
   END
   ELSE IF (@messagetypename = N'PPS_StepCompleteMessageType')
   BEGIN
     UPDATE a
     SET RequestCompleted= RequestCompleted + 1
     OUTPUT inserted.RequestCompleted INTO @tempTbl
     FROM PPS.MetaData as a
     WHERE Context= @context;
     
	 SELECT top(1) @i = RequestCompleted
     FROM @tempTbl;
     DELETE FROM @tempTbl;

     IF (@i = @cnt)
     BEGIN
       SET @exitWhile=1;
     END;
   END
   ELSE IF (@messagetypename = N'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')
   BEGIN
    SET @exitWhile=1;
   END
   ELSE IF (@messagetypename = N'http://schemas.microsoft.com/SQL/ServiceBroker/Error')
   BEGIN
     DECLARE @errmsg XML;
     SELECT @errmsg = CAST(@msg as XML);
     SET @ErrorNumber = (SELECT @errmsg.value(N'declare namespace
     bns="http://schemas.microsoft.com/SQL/ServiceBroker/Error";
     (/bns:Error/bns:Code)[1]', 'int'));
     SET @ErrorMessage = (SELECT @errmsg.value('declare namespace
     bns="http://schemas.microsoft.com/SQL/ServiceBroker/Error";
     (/bns:Error/bns:Description)[1]', 'nvarchar(3000)'));
     RAISERROR (N'Error: %s with the number: %s', 16, 1, @ErrorMessage, @ErrorNumber);
   END
   ELSE IF (@elapsedTime > @timeOut)
   BEGIN
      RAISERROR (N'The operation %s ended with timeout', 16,1, @ctx);
   END   
   ELSE
   BEGIN
     RAISERROR (N'Unknown message type %s', 16, 1, @messagetypename);
   END;
   EXEC sp_releaseapplock @Resource =  @ctx;
   COMMIT TRANSACTION;
 END TRY
 BEGIN CATCH
     SET @exitWhile = 1;

     IF (XACT_STATE() <> 0)
     BEGIN
      ROLLBACK TRANSACTION;
     END;
    
    EXEC dbo.usp_RethrowError;
 END CATCH
END; --WHILE
END
