USE [rebus2_test]
GO

DECLARE @RC int, @BatchID int, @category nvarchar(100), @infoType nvarchar(100), @context uniqueidentifier;
DECLARE @ch UNIQUEIDENTIFIER;

SET @BatchID=1;
SET @category='category';
SET @infoType='test';
SET @context= NEWID();

EXECUTE @RC = [dbo].[sp_SendTest] 
   @BatchID
  ,@category
  ,@infoType
  ,@context, 
  @ch OUTPUT;

SELECT @ch;
GO


