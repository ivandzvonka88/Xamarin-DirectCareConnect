USE [THPL]
GO
/****** Object:  StoredProcedure [dbo].[sp_TaskResetProviderNote]    Script Date: 9/18/2019 12:16:39 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_TaskResetProviderNote]
@userLevel VARCHAR(100),
@userprId INTEGER,
@prId INTEGER,
@docId INTEGER,
@docType VARCHAR(100)
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
		If @docType = 'ATCServiceNote'
		BEGIN
			UPDATE clientNotesATC SET lostSession=0;
		END
		ELSE IF @docType= 'HAHServiceNote'
		BEGIN
			UPDATE clientNotesHAH SET lostSession=0;
		END
		ELSE IF @docType= 'RSPServiceNote'
		BEGIN
			UPDATE clientNotesRSP SET lostSession=0;
		END

		BEGIN
			/* Hab progress report incomplete */
			SELECT S.fn AS sfn,S.ln AS sln,S.prId,C.fn AS cfn,C.ln AS cln,C.clsvId, 
			CHPR.svc + ' progress report for ' + C.fn + ' ' + C.ln + ' not completed, due ' + CONVERT(varchar(11),CHPR.dueDt,101) AS msg,
			'HabReport' As docType,
			CHPR.progressHabId AS docId,
			completed,
			verified,
			0 AS lostSession
			FROM ClientStaffRelationships AS CSR 
			JOIN ClientHABProgressReport AS CHPR ON CHPR.clsvId=CSR.clsvId AND CHPR.completed=0 AND deleted=0
			JOIN THPLipr AS S ON S.prId=CSR.prId
			JOIN THPLcl AS C ON C.clsvId=CHPR.clsvId
			WHERE CSR.prid=@prId
			UNION
			/* Late notes*/
			SELECT S.fn AS sfn,S.ln AS sln,S.prId,C.fn AS cfn,C.ln AS cln,C.clsvId,
			CASE 
				WHEN N.lostSession<> 0 THEN N.svc + ' Client Note for ' + C.fn + ' ' + C.ln + ' session lost'
				ELSE N.svc+ ' Client Note for ' + C.fn + ' ' + C.ln + ' incomplete'
			END AS msg,
			docType,
			docId,
			completed,
			CAST(0 AS bit) AS verified,
			lostSession	
			FROM
				(
				SELECT prId,clsvId,svc,clRspNoteId AS docId,Completed,deleted,lostSession,'RSPServiceNote' AS docType
				FROM ClientNotesRSP 
				WHERE prId=@prId AND (completed=0 OR lostSession<>0) AND deleted=0
				UNION
				SELECT prId,clsvId,svc,clAtcNoteId AS docId,Completed,deleted,lostSession,'ATCServiceNote' AS docType
				FROM ClientNotesATC
				WHERE prId=@prId AND (completed=0 OR lostSession<>0) AND deleted=0
				UNION
				SELECT prId,clsvId,svc,clHahNoteId AS docId,Completed,deleted,lostSession,'HAHServiceNote' AS docType
				FROM ClientNotesHAH
				WHERE prId=@prId AND (completed=0 OR lostSession<>0) AND deleted=0
				) AS N
			JOIN THPLipr AS S ON S.prid=@prId
			JOIN THPLcl AS C ON C.clsvId=N.clsvId
		END



	/* 2 STAFF ALERTS - late Notes Alert/ATC Monitoring Alerts/HAB progress Note Alerts */
If @userLevel='SuperAdmin' OR @userLevel='HumanResources'
BEGIN 
	/* Hab progress report incomplete by provider */
	SELECT S.fn AS sfn,S.ln AS sln, S.prId,	
	S.fn + ' ' + S.ln + ' ' + CHPR.svc + ' progress report for ' + C.fn + ' ' + C.ln + ' not completed, due ' + CONVERT(varchar(11),CHPR.dueDt,101)AS msg,
	CASE 
		WHEN SR.HabProgressReportRedAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportRedAlertDays,dueDt)<GETDate() THEN 1
		WHEN SR.HabProgressReportAmberAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportAmberAlertDays,dueDt)<GETDate() THEN 1
	END AS priority
	FROM StaffRoles AS SR
	JOIN ClientHABProgressReport AS CHPR ON CHPR.completed=0 AND CHPR.deleted=0 AND
	((SR.HabProgressReportAmberAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportAmberAlertDays,dueDt)<GETDate())OR(SR.HabProgressReportRedAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportRedAlertDays,dueDt)<GETDate()))
	JOIN THPLcl AS C ON C.clsvId=CHPR.clsvId
	JOIN THPLipr AS S ON S.prId=CHPR.prId
	WHERE SR.roleNominal=@userLevel AND (SR.HabProgressReportAmberAlertOn<>0 OR SR.HabProgressReportRedAlertOn<> 0) 
	UNION
	/* Hab Progress reports not verified by supervisor */
	SELECT S.fn AS sfn,S.ln AS sln, S.prId,
	S.fn + ' ' + S.ln + ' ' + CHPR.svc + ' progress report for ' + C.fn + ' ' + C.ln + ' not verified, due ' + CONVERT(varchar(11),CHPR.dueDt,101)AS msg,
	CASE 
		WHEN SR.HabProgressReportRedAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportRedAlertDays,dueDt)<GETDate() THEN 1
		WHEN SR.HabProgressReportAmberAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportAmberAlertDays,dueDt)<GETDate() THEN 1
	END AS priority
	FROM StaffRoles AS SR
	JOIN ClientHABProgressReport AS CHPR ON CHPR.completed<>0 AND CHPR.verified=0 AND CHPR.deleted=0 AND
	((SR.HabProgressReportAmberAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportAmberAlertDays,dueDt)<GETDate())OR(SR.HabProgressReportRedAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportRedAlertDays,dueDt)<GETDate()))
	JOIN THPLcl AS C ON C.clsvId=CHPR.clsvId
	JOIN THPLipr AS I ON I.prId=CHPR.prId
	JOIN THPLipr AS S ON S.prid=I.supPrId OR S.prId=I.tempsupPrid /* Gets supervisors */
	WHERE SR.roleNominal=@userLevel AND (SR.HabProgressReportAmberAlertOn<>0 OR SR.HabProgressReportRedAlertOn<> 0)
	UNION
	/* ATC Monitoring Reports */
	SELECT S.fn AS sfn,S.ln AS sln, S.prId,
	S.fn + ' ' + S.ln + ' ' + CAM.svc + ' monitoring for ' + C.fn + ' ' + C.ln + '  due ' + CONVERT(varchar(11),CAM.dueDt,101),
	CASE 
		WHEN SR.ATCMonitoringRedAlertOn<>0 AND DATEADD(DAY,SR.ATCMonitoringRedAlertDays,dueDt)<GETDate() THEN 1
		WHEN SR.ATCMonitoringAmberAlertOn<>0 AND DATEADD(DAY,SR.ATCMonitoringAmberAlertDays,dueDt)<GETDate() THEN 1
	END AS priority
	FROM StaffRoles AS SR
	JOIN ClientATCMonitoring AS CAM ON CAM.completed=0 AND CAM.deleted=0 AND
	((SR.ATCMonitoringAmberAlertOn<>0 AND DATEADD(DAY,SR.ATCMonitoringAmberAlertDays,dueDt)<GETDate())OR(SR.ATCMonitoringRedAlertOn<>0 AND DATEADD(DAY,SR.ATCMonitoringRedAlertDays,dueDt)<GETDate()))
	JOIN THPLcl AS C ON C.clsvId=CAM.clsvId
	JOIN ClientStaffRelationships AS CSR ON CSR.clsvId=CAM.clsvId
	JOIN THPLipr AS I ON I.prId=CSR.prId
	JOIN THPLipr AS S ON S.prid=I.supPrId OR S.prId=I.tempsupPrid
	WHERE SR.roleNominal='SuperAdmin' AND (SR.ATCMonitoringAmberAlertOn<>0 OR SR.ATCMonitoringRedAlertOn<> 0) 
	UNION
	/* Credentials */
	SELECT TOP 50 X.fn AS sfn,X.ln AS sln,X.prId,
	CASE
		WHEN validTo IS NULL THEN X.fn + ' ' + X.ln + ' Missing ' + X.credName
		WHEN verified=0 THEN X.fn + ' ' + X.ln + ' Requires Verification for ' + X.credName
		WHEN (SR.CredRedAlertOn<>0 AND DATEADD(DAY,SR.credRedAlertDays,validTo)<GETDate())OR(SR.CredAmberAlertOn<>0 AND DATEADD(DAY,SR.credAmberAlertDays,validTo)<GETDate()) THEN X.fn + ' ' + X.ln +  'Expiring ' + X.credName + ' '  +CONVERT(VARCHAR(11), validTo, 101)
	END AS msg,
	CASE
		WHEN validTo IS NULL THEN 1
		WHEN verified=0 THEN 1
		WHEN SR.CredRedAlertOn<>0 AND DATEADD(DAY,SR.credRedAlertDays,validTo)<GETDate() THEN 1
		WHEN SR.CredAmberAlertOn<>0 AND DATEADD(DAY,SR.credAmberAlertDays,validTo)<GETDate() THEN 2
	END AS priority
	FROM
		(
		SELECT DISTINCT S2.prId,S2.fn,S2.ln,ICI.credName,ICI.credTypeId,IC.credId,IC.verified,IC.validFrom,IC.validTo,IC.docId,IC.verificationDate,S.fn+' '+S.ln AS verifier,
		ROW_NUMBER() OVER(PARTITION BY S2.prId,ICI.credTypeId ORDER BY IC.verified ASC,IC.validTo DESC)AS R
		FROM THPLipr AS S2
		JOIN CredentialIds AS ICI ON(S2.isSuperAdmin<>0 AND ICI.superadmin<>0)OR(S2.isHumanResources<>0 AND ICI.humanResources<>0)OR(S2.isDirector<>0 AND ICI.director<>0)OR(S2.isAssistantDirector<>0 AND ICI.assistantdirector<>0)OR(S2.isSupervisor<>0 AND ICI.supervisor<>0)OR(S2.isProvider<>0 AND ICI.provider<>0)OR(S2.profLicReq<>0 AND ICI.credTypeId=7)OR(S2.profLiabilityReq<>0 AND ICI.credTypeId=8)OR(S2.providesTransport='Y' AND ICI.credTypeId=9)OR(S2.ownVehicle='Y' AND(ICI.credTypeId=10 OR ICI.credTypeId=11))
		LEFT JOIN StaffCredentials AS IC ON IC.prId=S2.prId AND IC.credTypeId=ICI.credTypeId
		LEFT JOIN THPLipr AS S ON S.prid=IC.verifier
		WHERE S2.deleted=0
		)AS X
	JOIN StaffRoles AS SR ON SR.rolenominal=@userLevel
	WHERE R=1 AND((SR.CredAmberAlertOn<>0 AND (validTo IS NULL OR verified=0 OR DATEADD(DAY,SR.CredAmberAlertDays,validTo)<GETDate()))OR(SR.CredRedAlertOn<>0 AND (validTo IS NULL OR verified=0 OR DATEADD(DAY,SR.CredRedAlertDays,validTo)<GETDate())))
	
END;

ELSE
BEGIN
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
/* late note alerts */
SELECT S.fn AS sfn, S.ln AS sln, N.prId,
CASE 
  WHEN N.lostSession<> 0 THEN S.fn + ' ' + S.ln + ' ' + noteType + ' Client Note for ' + C.fn + ' ' + C.ln + ' Incomplete - Session Lost'
  ELSE S.fn + ' ' + S.ln + ' ' + noteType + ' Client Note for ' + C.fn + ' ' + C.ln + ' Incomplete'
END AS msg,
1 AS priority
FROM 
	(
	SELECT CN.prId,CN.clsvId,CN.svc,CN.clRspNoteId AS NtId,CN.Completed,CN.deleted,CN.lostSession,'RSP' AS noteType FROM staffList AS I
	JOIN ClientNotesRSP AS CN ON CN.prId=I.prId
	WHERE (completed=0 OR lostSession<>0) AND deleted=0
	UNION
	SELECT CN.prId,CN.clsvId,CN.svc,CN.clAtcNoteId AS NtId,CN.Completed,CN.deleted,CN.lostSession,'ATC' AS noteType  FROM staffList AS I
	JOIN ClientNotesATC AS CN ON CN.prId=I.prId
	WHERE (completed=0 OR lostSession<>0) AND deleted=0
	UNION
	SELECT CN.prId,CN.clsvId,CN.svc,CN.clHahNoteId AS NtId,CN.Completed,CN.deleted,CN.lostSession,'HAH' AS noteType  FROM staffList AS I
	JOIN ClientNotesHAH AS CN ON CN.prId=I.prId
	WHERE (completed=0 OR lostSession<>0) AND deleted=0
	) AS N
JOIN THPLcl AS C ON C.clsvId=N.clsvId
JOIN THPLipr AS S ON S.prId=N.prId
WHERE ((@userlevel = 'Provider' OR @userlevel = 'Supervisor') AND N.completed = 0 AND N.deleted=0) OR
(N.lostSession<>0 AND N.deleted=0)
UNION
	/* ATC/HSK monitoring reports */
	SELECT S.fn AS sfn,S.ln AS sln, S.prId,
	S.fn + ' ' + S.ln + ' ' + CAM.svc + ' monitoring for ' + C.fn + ' ' + C.ln + '  due ' + CONVERT(varchar(11),CAM.dueDt,101),
	CASE 
		WHEN SR.ATCMonitoringRedAlertOn<>0 AND DATEADD(DAY,SR.ATCMonitoringRedAlertDays,dueDt)<GETDate() THEN 1
		WHEN SR.ATCMonitoringAmberAlertOn<>0 AND DATEADD(DAY,SR.ATCMonitoringAmberAlertDays,dueDt)<GETDate() THEN 1
	END AS priority
	FROM StaffList AS SL
	JOIN StaffRoles AS SR ON SR.roleNominal=@userLevel AND (SR.ATCMonitoringAmberAlertOn<>0 OR SR.ATCMonitoringRedAlertOn<> 0) 
	JOIN ClientStaffRelationships AS CSR ON CSR.prId=SL.prId
	JOIN ClientATCMonitoring AS CAM ON CAM.clsvId=CSR.clsvId AND CAM.completed=0 AND CAM.deleted=0 AND
	((SR.ATCMonitoringAmberAlertOn<>0 AND DATEADD(DAY,SR.ATCMonitoringAmberAlertDays,dueDt)<GETDate())OR(SR.ATCMonitoringRedAlertOn<>0 AND DATEADD(DAY,SR.ATCMonitoringRedAlertDays,dueDt)<GETDate()))
	JOIN THPLcl AS C ON C.clsvId=CAM.clsvId
	JOIN THPLipr AS I ON I.prId=CSR.prId
	JOIN THPLipr AS S ON S.prid=I.supPrId OR S.prId=I.tempsupPrid
	UNION
	/* Hab progress report incomplete */
	SELECT S.fn AS sfn,S.ln AS sln, S.prId,	
	S.fn + ' ' + S.ln + ' ' + CHPR.svc + ' progress report for ' + C.fn + ' ' + C.ln + ' not completed, due ' + CONVERT(varchar(11),CHPR.dueDt,101),
	CASE 
		WHEN SR.HabProgressReportRedAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportRedAlertDays,dueDt)<GETDate() THEN 1
		WHEN SR.HabProgressReportAmberAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportAmberAlertDays,dueDt)<GETDate() THEN 1
	END AS priority
	FROM StaffList AS SL
	JOIN StaffRoles AS SR ON SR.roleNominal=@userLevel AND (SR.HabProgressReportAmberAlertOn<>0 OR SR.HabProgressReportRedAlertOn<> 0) 
	JOIN ClientStaffRelationships AS CSR ON CSR.prId=SL.prId
	JOIN ClientHABProgressReport AS CHPR ON CHPR.clsvId=CSR.clsvID AND CHPR.completed=0 AND CHPR.deleted=0 AND
	((SR.HabProgressReportAmberAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportAmberAlertDays,dueDt)<GETDate())OR(SR.HabProgressReportRedAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportRedAlertDays,dueDt)<GETDate()))
	JOIN THPLcl AS C ON C.clsvId=CHPR.clsvId
	JOIN THPLipr AS S ON S.prId=CHPR.prId
	
	UNION
	/* Hab Progress reports not verified by supervisor */
	SELECT S.fn AS sfn,S.ln AS sln, S.prId,
	S.fn + ' ' + S.ln + ' ' + CHPR.svc + ' progress report for ' + C.fn + ' ' + C.ln + ' not verified, due ' + CONVERT(varchar(11),CHPR.dueDt,101),
	CASE 
		WHEN SR.HabProgressReportRedAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportRedAlertDays,dueDt)<GETDate() THEN 1
		WHEN SR.HabProgressReportAmberAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportAmberAlertDays,dueDt)<GETDate() THEN 1
	END AS priority
	FROM StaffList AS SL
	JOIN StaffRoles AS SR ON SR.roleNominal=@userLevel AND (SR.HabProgressReportAmberAlertOn<>0 OR SR.HabProgressReportRedAlertOn<> 0) 
	JOIN ClientStaffRelationships AS CSR ON CSR.prId=SL.prId
	JOIN ClientHABProgressReport AS CHPR ON CHPR.clsvId=CSR.clsvID AND CHPR.completed<>0 AND CHPR.verified=0 AND CHPR.deleted=0 AND
	((SR.HabProgressReportAmberAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportAmberAlertDays,dueDt)<GETDate())OR(SR.HabProgressReportRedAlertOn<>0 AND DATEADD(DAY,SR.HabProgressReportRedAlertDays,dueDt)<GETDate()))
	JOIN THPLcl AS C ON C.clsvId=CHPR.clsvId
	JOIN THPLipr AS I ON I.prId=CHPR.prId
	JOIN THPLipr AS S ON S.prid=I.supPrId OR S.prId=I.tempsupPrid
UNION 
/* Credential alerts */
SELECT X.fn AS sfn,X.ln AS sln,X.prId,
CASE
WHEN validTo IS NULL THEN X.fn + ' ' + X.ln + ' Missing ' + X.credName
WHEN verified=0 THEN X.fn + ' ' + X.ln + ' Requires Verification for ' + X.credName
WHEN (SR.CredRedAlertOn<>0 AND DATEADD(DAY,SR.credRedAlertDays,validTo)<GETDate())OR(SR.CredAmberAlertOn<>0 AND DATEADD(DAY,SR.credAmberAlertDays,validTo)<GETDate()) THEN X.fn + ' ' + X.ln +  'Expiring ' + X.credName + ' '  +CONVERT(VARCHAR(11), validTo, 101)
END AS msg,
CASE
	WHEN validTo IS NULL THEN 1
	WHEN verified=0 THEN 1
	WHEN SR.CredRedAlertOn<>0 AND DATEADD(DAY,SR.credRedAlertDays,validTo)<GETDate() THEN 1
	WHEN SR.CredAmberAlertOn<>0 AND DATEADD(DAY,SR.credAmberAlertDays,validTo)<GETDate() THEN 2
END AS priority
FROM
	(
	SELECT DISTINCT I.prId,S2.fn,S2.ln,ICI.credName,ICI.credTypeId,IC.credId,IC.verified,IC.validFrom,IC.validTo,IC.docId,IC.verificationDate,S.fn+' '+S.ln AS verifier,
	ROW_NUMBER() OVER(PARTITION BY I.prId,ICI.credTypeId ORDER BY IC.verified ASC,IC.validTo DESC)AS R
	FROM StaffList AS I
	JOIN THPLipr AS S2 ON S2.prId=I.prId
	JOIN CredentialIds AS ICI ON(S2.isSuperAdmin<>0 AND ICI.superadmin<>0)OR(S2.isHumanResources<>0 AND ICI.humanResources<>0)OR(S2.isDirector<>0 AND ICI.director<>0)OR(S2.isAssistantDirector<>0 AND ICI.assistantdirector<>0)OR(S2.isSupervisor<>0 AND ICI.supervisor<>0)OR(S2.isProvider<>0 AND ICI.provider<>0)OR(S2.profLicReq<>0 AND ICI.credTypeId=7)OR(S2.profLiabilityReq<>0 AND ICI.credTypeId=8)OR(S2.providesTransport='Y' AND ICI.credTypeId=9)OR(S2.ownVehicle='Y' AND(ICI.credTypeId=10 OR ICI.credTypeId=11))
	LEFT JOIN StaffCredentials AS IC ON IC.prId=I.prId AND IC.credTypeId=ICI.credTypeId
	LEFT JOIN THPLipr AS S ON S.prid=IC.verifier
	)AS X
JOIN StaffRoles AS SR ON SR.rolenominal=@userLevel
WHERE R=1 AND((SR.CredAmberAlertOn<>0 AND (validTo IS NULL OR verified=0 OR DATEADD(DAY,SR.CredAmberAlertDays,validTo)<GETDate()))OR(SR.CredRedAlertOn<>0 AND (validTo IS NULL OR verified=0 OR DATEADD(DAY,SR.CredRedAlertDays,validTo)<GETDate())))
ORDER BY priority ASC;
END;

	END
	

END