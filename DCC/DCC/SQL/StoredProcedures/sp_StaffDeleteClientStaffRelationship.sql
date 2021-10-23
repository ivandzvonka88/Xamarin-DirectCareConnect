USE [THPL]
GO
/****** Object:  StoredProcedure [dbo].[sp_StaffDeleteClientStaffRelationship]    Script Date: 10/2/2019 10:26:14 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_StaffDeleteClientStaffRelationship]
@userLevel VARCHAR(100),
@userprId INTEGER,
@prId INTEGER,
@relId INTEGER

AS
BEGIN
	DECLARE @accessGranted AS INT = 0

	/* See if we have access to targeted staff member*/
	IF @userLevel='SuperAdmin' OR  @userLevel='HumanResources' OR (@userprId = @prid)
		SET @accessGranted = 1;
	ELSE
		WITH StaffList AS
		(SELECT S1.prid,1 AS eL
		FROM THPLipr AS S1
		WHERE prid=@userPrid
		UNION ALL
		SELECT S2.prid,SL.eL+1 AS eL
		FROM THPLipr AS S2
		INNER JOIN StaffList AS SL
		ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId
		WHERE S2.prid<>0 AND S2.prID IS NOT NULL)
		SELECT @accessGranted = COUNT(*)  FROM StaffList AS I WHERE prId=@prId AND prId<>@userPrid;
	
	IF @accessGranted <> 0
	BEGIN
		DELETE FROM ClientStaffRelationships WHERE relId=@relId
		
		SELECT C.fn,C.ln,CI.relId,CI.clsvId,CI.pridr,AR.atcRelationship
        FROM ClientStaffRelationships AS CI
        JOIN THPLcl AS C ON C.clsvId=CI.clsvid AND c.deleted=0
		JOIN AtcRelationships AS AR ON AR.atcRelId=CI.pridr
        WHERE CI.prId=@prId ORDER BY C.ln ASC,C.fn ASC
	END
	
END