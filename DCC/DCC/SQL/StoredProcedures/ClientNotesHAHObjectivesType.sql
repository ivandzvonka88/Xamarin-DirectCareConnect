USE [THPL]
GO

/****** Object:  UserDefinedTableType [dbo].[ClientNotesHAHObjectivesType]    Script Date: 9/30/2019 11:28:58 AM ******/
CREATE TYPE [dbo].[ClientNotesHAHObjectivesType] AS TABLE(
	[clHAHNoteId] [int] NULL,
	[objectiveId] [int] NULL,
	[note] [varchar](200) NULL,
	[score] [varchar](5) NOT NULL
)
GO


