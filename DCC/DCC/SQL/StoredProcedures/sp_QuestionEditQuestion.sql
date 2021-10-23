USE [THPL]
GO
/****** Object:  StoredProcedure [dbo].[sp_QuestionEditQuestion]    Script Date: 10/7/2019 1:40:45 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_QuestionEditQuestion]
@questionId AS integer
AS
BEGIN
	SET NOCOUNT ON
	SELECT * FROM Question WHERE questionId=@questionId
	SELECT * FROM QuestionValues
END