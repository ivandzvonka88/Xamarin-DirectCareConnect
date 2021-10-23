CREATE TYPE ClientNotesATCObjectivesType
   AS TABLE
   ( 
	[clATCNoteId] [int] NULL,
	[careId] [int] NULL,
	[score] [varchar](3) NOT NULL
	);
GO