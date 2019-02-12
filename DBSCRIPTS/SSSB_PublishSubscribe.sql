CREATE MESSAGE TYPE PPS_SubscribeMessageType
AUTHORIZATION dbo
VALIDATION = NONE 
GO
CREATE MESSAGE TYPE PPS_UnSubscribeMessageType
AUTHORIZATION dbo
VALIDATION = NONE
GO
CREATE MESSAGE TYPE PPS_HeartBeatMessageType
AUTHORIZATION dbo
VALIDATION = NONE
GO
CREATE CONTRACT PPS_PublishSubscribeContract
(
 [PPS_SubscribeMessageType] SENT BY INITIATOR,
 [PPS_UnSubscribeMessageType] SENT BY INITIATOR,
 [PPS_HeartBeatMessageType]  SENT BY INITIATOR,
 [PPS_OnDemandTaskMessageType] SENT BY TARGET
)
GO
CREATE QUEUE PPS_PublisherQueue
WITH STATUS = ON
GO
CREATE QUEUE PPS_SubscriberQueue
WITH STATUS = ON
GO
CREATE SERVICE PPS_PublisherService
AUTHORIZATION dbo
ON QUEUE PPS_PublisherQueue
(
  [PPS_PublishSubscribeContract]
)
GO
CREATE SERVICE PPS_SubscriberService
AUTHORIZATION dbo
ON QUEUE PPS_SubscriberQueue
(
  [PPS_PublishSubscribeContract]
)
GO
CREATE TABLE PPS.Subscriber (
	[SubscriberID] nvarchar(40) NOT NULL,
	[ConversationHandle] uniqueidentifier NOT NULL,
	[LastHeartBeat] [datetime] NULL,
	[LastError] NVARCHAR(4000) NULL,
	[CreateDate] [datetime] NOT NULL DEFAULT (getdate()),
	[ModifiedDate] [datetime] NOT NULL DEFAULT (getdate()),
	CONSTRAINT PK_Subscriber PRIMARY KEY NONCLUSTERED ([SubscriberID])
);
GO
CREATE TABLE PPS.Subscription (
	[SubscriptionID] INT NOT NULL IDENTITY(1,1),
	[SubscriberID] nvarchar(40) NOT NULL,
	[Topic] NVARCHAR(128) NOT NULL,
	[CreateDate] [datetime] NOT NULL DEFAULT (getdate()),
	CONSTRAINT PK_Subscription PRIMARY KEY NONCLUSTERED ([SubscriptionID]),
	CONSTRAINT UK_Subscription UNIQUE CLUSTERED ([SubscriberID], [Topic]));
GO
CREATE TABLE [PPS].[SubscriptionError](
	[SubscriptionErrorID] [int] IDENTITY(1,1) NOT NULL,
	[ConversationHandle] [uniqueidentifier] NULL,
	[Error] [nvarchar](4000) NULL,
	[CreateDate] [datetime] NOT NULL DEFAULT (getdate()),
 CONSTRAINT [PK_SubscriptionError] PRIMARY KEY NONCLUSTERED 
(
	[SubscriptionErrorID] ASC
)
)
GO
ALTER TABLE PPS.Subscription WITH CHECK ADD  CONSTRAINT [FK_Subscription_Subscriber] FOREIGN KEY([SubscriberID])
REFERENCES PPS.Subscriber ([SubscriberID])
ON DELETE CASCADE
GO
CREATE NONCLUSTERED INDEX IX_Subscription_Topic ON PPS.Subscription ([Topic]);
GO
SET ARITHABORT ON;
GO
SET CONCAT_NULL_YIELDS_NULL ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_NULLS ON;
GO
SET ANSI_PADDING ON;
GO
SET ANSI_WARNINGS ON;
GO
SET NUMERIC_ROUNDABORT OFF;
GO
CREATE PROCEDURE [PPS].[sp_PublisherHandler]
AS
BEGIN
    SET NOCOUNT ON;
	DECLARE @cg UNIQUEIDENTIFIER, @ch UNIQUEIDENTIFIER, @messagetypename NVARCHAR(256), @messagebody varbinary(max), @result INT;
	DECLARE @msg XML, @SubscriberID NVARCHAR(40), @Topic NVARCHAR(128), @SubscriberCh UNIQUEIDENTIFIER;
	DECLARE @ErrorMessage NVARCHAR(4000), @ErrorNumber INT;

	BEGIN TRY
		BEGIN TRANSACTION;
		RECEIVE TOP(1)
		@cg = conversation_group_id,
		@ch = conversation_handle,
		@messagetypename = message_type_name,
		@messagebody = message_body
		FROM PPS_PublisherQueue;


		IF (@messagetypename = N'PPS_SubscribeMessageType' OR  @messagetypename = N'PPS_UnSubscribeMessageType' OR @messagetypename = N'PPS_HeartBeatMessageType')
		BEGIN
			SET @msg = CAST(@messagebody AS XML);
			SELECT @SubscriberID = @msg.value('(/message/@subscriberId)[1]', 'NVARCHAR(40)'),
			@Topic = @msg.value('(/message/topic)[1]', 'NVARCHAR(128)');

			EXEC @result = sp_getapplock @Resource = @SubscriberID, @LockMode = 'Exclusive';
			IF (@result < 0)
			BEGIN
			   RAISERROR (N'sp_getapplock failed to lock the resource: %s the reason: %d', 16, 1, @SubscriberID, @result);
			END
			ELSE
			BEGIN
			  SELECT @SubscriberCh = ConversationHandle
			  FROM PPS.Subscriber
			  WHERE SubscriberID = @SubscriberID;

			  IF (@messagetypename = N'PPS_SubscribeMessageType')
			  BEGIN
				IF (@SubscriberCh IS NULL)
				BEGIN
				   INSERT INTO PPS.Subscriber (SubscriberID, [ConversationHandle],[LastHeartBeat]) 
				   VALUES (@SubscriberID, @ch, GetUtcDate());
				END
				ELSE
				BEGIN
				   IF (@SubscriberCh != @ch)
				   BEGIN
				     UPDATE PPS.Subscriber
				     SET [ConversationHandle]= @ch, [ModifiedDate]= GetDate(), [LastHeartBeat]= GetUtcDate(), LastError = NULL
				     WHERE SubscriberID= @SubscriberID;
				   END;
			    END;

				SELECT @result = COUNT(*) FROM PPS.Subscription
			    WHERE SubscriberID= @SubscriberID AND Topic= @Topic;

				IF (@result = 0)
				BEGIN
				   INSERT INTO PPS.Subscription (SubscriberID, Topic) 
				   VALUES (@SubscriberID, @Topic);
				END;
			  END --@messagetypename = N'PPS_SubscribeMessageType'
			  ELSE IF (@messagetypename = N'PPS_UnSubscribeMessageType')
			  BEGIN
			     IF (@SubscriberCh IS NOT NULL AND @SubscriberCh != @ch)
				 BEGIN
				   UPDATE PPS.Subscriber
				   SET [ConversationHandle] = @ch, [ModifiedDate] = GetDate(), [LastHeartBeat]= GetUtcDate(), LastError = NULL
				   WHERE SubscriberID= @SubscriberID;
				 END;

			     DELETE PPS.Subscription
          	     WHERE SubscriberID= @SubscriberID AND Topic= @Topic;
			  END
			  ELSE IF (@messagetypename = N'PPS_HeartBeatMessageType')
			  BEGIN
   				 IF (@SubscriberCh IS NOT NULL AND @SubscriberCh != @ch)
				 BEGIN
				   UPDATE PPS.Subscriber
				   SET [ConversationHandle] = @ch, [ModifiedDate] = GetDate(), [LastHeartBeat]= GetUtcDate(), LastError = NULL
				   WHERE SubscriberID= @SubscriberID;
				 END
				 ELSE
				 BEGIN
			        UPDATE PPS.Subscriber
				    SET [LastHeartBeat]= GetUtcDate()
				    WHERE SubscriberID= @SubscriberID;
				 END;
			  END;

			  EXEC @result = sp_releaseapplock @Resource = @SubscriberID;
			END; 
		END
		ELSE IF (@messagetypename = 'http://schemas.microsoft.com/SQL/ServiceBroker/Error')
		BEGIN
	        SET @msg = CAST(@messagebody AS XML);
            SET @ErrorMessage = (SELECT @msg.value('declare namespace
     bns="http://schemas.microsoft.com/SQL/ServiceBroker/Error";
     (/bns:Error/bns:Description)[1]', 'nvarchar(3000)'));

	        INSERT INTO [PPS].[SubscriptionError]([ConversationHandle],[Error])
            VALUES(@ch,  @ErrorMessage);

		    UPDATE PPS.Subscriber 
			SET [LastError]= @ErrorMessage
            WHERE [ConversationHandle] = @ch;

			END CONVERSATION @ch;
	    END
		ELSE IF (@messagetypename = 'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')
		BEGIN
		    DELETE PPS.Subscriber
            WHERE [ConversationHandle] = @ch;
			END CONVERSATION @ch;
	    END;

		COMMIT;
	END TRY
	BEGIN CATCH
        SELECT @ErrorNumber = ERROR_NUMBER(), @ErrorMessage = ERROR_MESSAGE();

		IF (XACT_STATE() < 0)
		BEGIN
			ROLLBACK TRANSACTION;
		END
		ELSE IF (XACT_STATE() > 0)
		BEGIN
			END CONVERSATION @ch WITH ERROR = @ErrorNumber DESCRIPTION = @ErrorMessage;
			COMMIT TRANSACTION;
		END;

		INSERT INTO [PPS].[SubscriptionError]([ConversationHandle],[Error])
        VALUES(@ch,  @ErrorMessage);
           

		IF (@SubscriberID IS NOT NULL)
		BEGIN
		  UPDATE PPS.Subscriber 
		  SET [LastError]= @ErrorMessage
          WHERE SubscriberID = @SubscriberID AND [LastError] IS NULL;
		END
		ELSE
		BEGIN
		  UPDATE PPS.Subscriber 
		  SET [LastError]= @ErrorMessage
          WHERE [ConversationHandle] = @ch AND [LastError] IS NULL;
		END;
	END CATCH
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [PPS].[sp_Publish] (@Topic NVARCHAR(128), @msg XML)
AS
BEGIN
SET XACT_ABORT ON;
SET NOCOUNT ON;
 DECLARE @noop BIT, @ch UNIQUEIDENTIFIER, @SubscriberID NVARCHAR(40), @result INT, @LastHeartBeat DateTime, @now DateTime;
 DECLARE @diff INT, @HeartBeatTimeOut INT;
 SET @HeartBeatTimeOut = 600; --Ten minutes

 SET @now= GETUTCDATE();

 DECLARE queuesCursor CURSOR FAST_FORWARD
 FOR SELECT [SubscriberID]
 FROM PPS.Subscription
 WHERE ISNULL(@Topic,N'') = N'' OR Topic= @Topic
 GROUP BY [SubscriberID];

  OPEN queuesCursor;

  FETCH NEXT FROM queuesCursor INTO @SubscriberID;

  WHILE (@@fetch_status = 0)
  BEGIN
	BEGIN TRY
      BEGIN TRANSACTION;
	   	EXEC @result = sp_getapplock @Resource = @SubscriberID, @LockMode = 'Exclusive';
		IF (@result < 0)
		BEGIN
		   RAISERROR (N'sp_getapplock failed to lock the resource: %s the reason: %d', 16, 1, @SubscriberID, @result);
		END;
		
		SET @ch = NULL;
		
		SELECT @ch = [ConversationHandle], @LastHeartBeat = [LastHeartBeat]
		FROM PPS.Subscriber
		WHERE SubscriberID= @SubscriberID AND LastError IS NULL;

		SET @now= GETUTCDATE();
		SET @diff= DATEDIFF(second,@LastHeartBeat, @now);

		IF (@ch IS NOT NULL AND @diff < @HeartBeatTimeOut)
		BEGIN
		  SET @noop = @noop;
          SEND ON CONVERSATION @ch MESSAGE TYPE [PPS_OnDemandTaskMessageType](@msg);
		END;

		EXEC @result = sp_releaseapplock @Resource = @SubscriberID;
	  COMMIT;
	END TRY
	BEGIN CATCH
      IF (XACT_STATE() <> 0)
      BEGIN
        ROLLBACK TRANSACTION;
      END;
      
	  UPDATE PPS.Subscriber 
	  SET [LastError]= ERROR_MESSAGE()
	  WHERE SubscriberID = @SubscriberID;
	 END CATCH

	 FETCH NEXT FROM queuesCursor INTO @SubscriberID;
  END;

 CLOSE queuesCursor;
 DEALLOCATE queuesCursor;

SET NOCOUNT OFF;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [PPS].[sp_SendFlushSettings] (@settingsType int, @settingsID int)
AS
BEGIN
SET XACT_ABORT ON;
SET NOCOUNT ON;
  DECLARE @task_id INT, @now NVARCHAR(20), @msg XML;
  SET @now = CONVERT(Nvarchar(20),GetDate(),120);
  SET @task_id = 1;

  DECLARE @param TABLE
  (
	[paramID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [name] Varchar(100) NOT NULL,
    [value] Varchar(40) NOT NULL
  );

  INSERT INTO @param([name], [value])
  VALUES('settingsType', Cast(@settingsType as Nvarchar(40)));
  INSERT INTO @param([name], [value])
  VALUES('settingsID', Cast(@settingsID as Nvarchar(40)));
 
  WITH CTE(res)
   AS
   ( 
     SELECT @task_id as task, @now as [date], 'false' as [multy-step],
     ( SELECT [name] as [@name], [value] as [@value]
       FROM @param
       FOR XML PATH('param'), TYPE, ROOT('params')
      )
     FOR XML PATH ('timer'), ELEMENTS, TYPE
   )
   SELECT @msg = res FROM CTE;

   EXEC [PPS].[sp_Publish] N'ADMIN', @msg;
 
SET NOCOUNT OFF;
END
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER QUEUE PPS_PublisherQueue 
WITH ACTIVATION (
	STATUS = ON,
	MAX_QUEUE_READERS = 1,
	PROCEDURE_NAME = PPS.sp_PublisherHandler, 
	EXECUTE AS OWNER
);
GO