SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[sp_SendTest] (@BatchID int, @category nvarchar(100), @infoType nvarchar(100), @context UNIQUEIDENTIFIER)
as
BEGIN
SET XACT_ABORT ON;
SET NOCOUNT ON;

  DECLARE @task_id INT, @SSSBServiceName NVarchar(128);
  DECLARE @ch UNIQUEIDENTIFIER;
  DECLARE @msg XML;
  DECLARE @RC INT;
  DECLARE @now varchar(20);
  DECLARE @mustCommit  BIT;
  SET @mustCommit =0;
  SET @task_id = 1;
  SET @now = CONVERT(Nvarchar(20),GetDate(),120);
   
IF (@@TRANCOUNT = 0)
 BEGIN
  BEGIN TRANSACTION;
  SET @mustCommit = 1;
 END;
 
BEGIN TRY
  SELECT @SSSBServiceName = SSSBServiceName
  FROM PPS.OnDemandTask
  WHERE OnDemandTaskID = @task_id;
  
  SET @SSSBServiceName = Coalesce(@SSSBServiceName, 'PPS_OnDemandTaskService');
  
  DECLARE @param TABLE
  (
	[paramID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [name] nVarchar(100)  NOT NULL,
    [value] nVarchar(max) NOT NULL
  );

  INSERT INTO @param([name], [value])
  VALUES(N'Category', @category);
  INSERT INTO @param([name], [value])
  VALUES(N'InfoType', @infoType);
  INSERT INTO @param([name], [value])
  VALUES(N'ClientContext', CAST(@context as Nvarchar(255)));
  INSERT INTO @param([name], [value])
  VALUES(N'UserId','EprstUser');
  INSERT INTO @param([name], [value])
  VALUES(N'BatchID', @BatchID);
  
  WITH CTE(res)
      AS
      ( 
        SELECT @task_id as task, @now as [date], 'true' as [multy-step],
        ( SELECT [name] as [@name], [value] as [@value]
          FROM @param
          FOR XML PATH('param'), TYPE, ROOT('params')
        )
        FOR XML PATH ('timer'), ELEMENTS, TYPE
      )
      SELECT @msg = res FROM CTE;

  BEGIN DIALOG CONVERSATION @ch
  FROM SERVICE [PPS_OnDemandTaskService]
  TO SERVICE @SSSBServiceName
  ON CONTRACT [PPS_OnDemandTaskContract]
  WITH LIFETIME = 1200, ENCRYPTION = OFF;

  /*
  <timer>
  <task>1</task>
  <date>2018-12-23 14:05:18</date>
  <multy-step>true</multy-step>
  <params>
    <param name="Category" value="category" />
    <param name="InfoType" value="test" />
    <param name="ClientContext" value="A5BA0DCE-E883-47E8-90C7-26AB0AD4B19F" />
    <param name="UserId" value="EprstUser" />
    <param name="BatchID" value="2" />
  </params>
</timer>
  */
  SEND ON CONVERSATION @ch MESSAGE TYPE [PPS_OnDemandTaskMessageType](@msg);
      

 -- END CONVERSATION @ch;
 
 IF (@mustCommit = 1)
    COMMIT;
END TRY
BEGIN CATCH
    -- ROLLBACK IF ERROR AND there's active transaction
    IF (XACT_STATE() <> 0)
    BEGIN
      ROLLBACK TRANSACTION;
    END;
    
    --rethrow handled error
    EXEC dbo.usp_RethrowError;
END CATCH

SET NOCOUNT OFF;
END
GO