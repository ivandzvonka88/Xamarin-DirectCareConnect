USE [DDDEZ]
GO
/****** Object:  StoredProcedure [dbo].[sp_StaffUpdateCredential]    Script Date: 9/17/2019 3:11:15 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_StaffVerifyCredential]
@userLevel VARCHAR(100),
@userprId INTEGER,
@prId INTEGER,
@credId INTEGER

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
		UPDATE StaffCredentials SET verified=1,verifier=@userprId,verificationDate=GETDATE() WHERE credId=@credId
		
		SELECT *,
		CASE 
			WHEN R<>1 THEN 'Superseded'
			WHEN verified=1 AND validTo > DATEADD(DAY,30, GETDATE()) THEN 'Verified'
			WHEN verified=0 AND validTo > DATEADD(DAY,30, GETDATE()) THEN 'Not Verified'
			WHEN validTo < GETDATE() THEN 'Expired'
			WHEN validTo > DATEADD(DAY,-30, GETDATE()) THEN 'Expiring'
		ELSE 'Missing'
		END AS status,
		CASE 
			WHEN R<>1 THEN 6
			WHEN verified=1 AND validTo > DATEADD(DAY,30, GETDATE()) THEN 5
			WHEN verified=0 AND validTo > DATEADD(DAY,30, GETDATE()) THEN 4				
			WHEN validTo < GETDATE() THEN 2
			WHEN validTo > DATEADD(DAY,-30, GETDATE()) THEN 3
			WHEN validTo IS NULL THEN 1
		END AS priority
		FROM(
			SELECT A.*,ROW_NUMBER() OVER(PARTITION BY credTypeId ORDER BY verified ASC,validTo DESC)AS R 
			FROM
				(
				SELECT ICI.credName,ICI.credTypeId,IC.credId,IC.docId,IC.validFrom,IC.validTo,IC.verified,IC.verificationDate,IC.fileExtension,I2.fn,I2.ln			
				FROM THPLipr AS S
				JOIN CredentialIds AS ICI ON
				(S.isSuperAdmin<>0 AND ICI.superadmin<>0)OR
				(S.isHumanResources<>0 AND ICI.humanResources<>0)OR
				(S.isDirector<>0 AND ICI.director<>0)OR
				(S.isAssistantDirector<>0 AND ICI.assistantdirector<>0)OR
				(S.isSupervisor<>0 AND ICI.supervisor<>0)OR
				(S.isProvider<>0 AND ICI.provider<>0)OR
				(S.profLicReq<>0 AND ICI.credTypeId=7)OR
				(S.profLiabilityReq<>0 AND ICI.credTypeId=8)OR
				(S.providesTransport='Y' AND ICI.credTypeId=9)OR
				(S.ownVehicle='Y' AND (ICI.credTypeId=10 OR ICI.credTypeId=11))
				LEFT JOIN StaffCredentials AS IC ON IC.prId=S.prId AND IC.credTypeId=ICI.credTypeId
				LEFT JOIN THPLipr AS I2 ON I2.prid=IC.verifier
				WHERE S.prid=@prId
				)AS A
			) AS B
		ORDER BY priority asc,credname ASC, validTo ASC;

	END
	ELSE
		SELECT TOP 0 NULL AS x;

END