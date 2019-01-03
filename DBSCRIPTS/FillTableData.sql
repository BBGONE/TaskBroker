USE [rebus2_test]
GO

INSERT INTO [PPS].[Executor]
           ([ExecutorID]
           ,[Description]
           ,[FullTypeName]
           ,[Active]
           ,[IsMessageDecoder]
           ,[IsOnDemand]
           ,[ExecutorSettingsSchema]
           )
     VALUES
           (1
           ,'Flush Settings Executor'
           ,'TaskBroker.SSSB.Executors.FlushSettingsExecutor,TaskBroker'
           ,1
           ,0
           ,1
           ,''
           )
GO

INSERT INTO [PPS].[Executor]
           ([ExecutorID]
           ,[Description]
           ,[FullTypeName]
           ,[Active]
           ,[IsMessageDecoder]
           ,[IsOnDemand]
           ,[ExecutorSettingsSchema]
           )
     VALUES
           (10
           ,'Test Executor'
           ,'TaskBroker.SSSB.Executors.TestExecutor,TaskBroker'
           ,1
           ,0
           ,1
           ,''
           )
GO

USE [rebus2_test]
GO

INSERT INTO [PPS].[OnDemandTask]
           ([OnDemandTaskID]
           ,[Name]
           ,[Description]
           ,[Active]
           ,[ExecutorID]
           ,[SheduleID]
           ,[SettingID]
           ,[SSSBServiceName]
           )
     VALUES
           (1
           ,'Flush Settings'
           ,'Flushing Settings Cache'
           ,1
           ,1
           ,NULL
           ,NULL
           ,NULL
           )
GO
INSERT INTO [PPS].[OnDemandTask]
           ([OnDemandTaskID]
           ,[Name]
           ,[Description]
           ,[Active]
           ,[ExecutorID]
           ,[SheduleID]
           ,[SettingID]
           ,[SSSBServiceName]
           )
     VALUES
           (10
           ,'Test'
           ,'Test OnDemandTask Execution'
           ,1
           ,10
           ,NULL
           ,NULL
           ,NULL
           )
GO
