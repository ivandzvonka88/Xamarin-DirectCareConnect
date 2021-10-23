USE [THPL]
GO
/****** Object:  StoredProcedure [dbo].[sp_QuestionGetValueOptions]    Script Date: 10/7/2019 1:40:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_QuestionGetValueOptions]
AS
BEGIN
	SET NOCOUNT ON
	SELECT * FROM QuestionValues
END