CREATE TYPE ClientATCQuestionsType
   AS TABLE
   ( 
	[atcQuestId] [int] NULL,
	[yes] [bit] NULL,
	[no] [bit] NULL,
	[na] [bit] NULL,
	[cmt] [varchar](300) NULL
	);
GO