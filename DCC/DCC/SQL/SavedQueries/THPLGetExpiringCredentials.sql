SELECT X.*,
CASE
WHEN validTo IS NULL THEN 'Missing'
WHEN verified=0 THEN 'Not Verified'
WHEN (SR.CredRedAlertOn<>0 AND DATEADD(DAY,SR.credRedAlertDays,validTo)<GETDate())OR(SR.CredAmberAlertOn<>0 AND DATEADD(DAY,SR.credAmberAlertDays,validTo)<GETDate()) THEN  'Expiration' 
END AS AlertType,
CASE
WHEN validTo IS NULL THEN 1
WHEN verified=0 THEN 1
WHEN SR.CredRedAlertOn<>0 AND DATEADD(DAY,SR.credRedAlertDays,validTo)<GETDate() THEN  1 
WHEN SR.CredAmberAlertOn<>0 AND DATEADD(DAY,SR.credAmberAlertDays,validTo)<GETDate() THEN  2
END AS priority
FROM(
SELECT S.prId,S.fn,S.ln,ICI.credName,ICI.credTypeId,IC.verified,IC.validTo,IC.credId,
ROW_NUMBER() OVER(PARTITION BY S.prId,ICI.credTypeId ORDER BY IC.verified ASC,IC.validTo DESC)AS Row
FROM Staff AS S
JOIN CredentialIds AS ICI ON(S.isSuperAdmin<>0 AND ICI.superadmin<>0)OR(S.isHumanResources<>0 AND ICI.humanResources<>0)OR(S.isDirector<>0 AND ICI.director<>0)OR(S.isAssistantDirector<>0 AND ICI.assistantdirector<>0)OR(S.isSupervisor<>0 AND ICI.supervisor<>0)OR(S.isProvider<>0 AND ICI.provider<>0)OR(S.profLicReq<>0 AND ICI.credTypeId=7)OR(S.profLiabilityReq<>0 AND ICI.credTypeId=8)OR(S.providesTransport='Y' AND ICI.credTypeId=9)OR(S.ownVehicle='Y' AND (ICI.credTypeId=10 OR ICI.credTypeId=11)) 
LEFT JOIN StaffCredentials AS IC ON IC.prId=S.prId AND IC.credTypeId=ICI.credTypeId 
WHERE S.deleted=0
) AS X
JOIN StaffRoles AS SR ON SR.rolenominal='provider' 
WHERE Row=1 AND 
((SR.CredAmberAlertOn<>0 AND (validTo IS NULL OR verified=0 OR DATEADD(DAY, SR.CredAmberAlertDays, validTo)<GETDate()))OR
(SR.CredRedAlertOn<>0 AND (validTo IS NULL OR verified=0 OR DATEADD(DAY, SR.CredRedAlertDays, validTo)<GETDate()))) 
ORDER BY ln ASC,fn ASC,verified asc,Row asc,credname ASC;






WITH StaffList AS
(SELECT S1.prid,S1.fn,S1.ln,S1.profLicReq,S1.profLiabilityReq,S1.providesTransport,S1.ownVehicle,1 AS empLevel
FROM Staff AS S1
WHERE prid= 827
UNION ALL
SELECT S2.prid,S2.fn,S2.ln,S2.profLicReq,S2.profLiabilityReq,S2.providesTransport,S2.ownVehicle,SL.empLevel+1 AS empLevel
FROM Staff AS S2
INNER JOIN StaffList AS SL
ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId
WHERE S2.prid<>0 AND S2.prID IS NOT NULL
)
SELECT X.*,
CASE
WHEN validTo IS NULL THEN 'Missing'
WHEN verified=0 THEN 'Not Verified'
WHEN (SR.CredRedAlertOn<>0 AND DATEADD(DAY,SR.credRedAlertDays,validTo)<GETDate())OR(SR.CredAmberAlertOn<>0 AND DATEADD(DAY,SR.credAmberAlertDays,validTo)<GETDate()) THEN  'Expiring'
WHEN verified=1 THen 'Verified' 
END AS AlertType,
CASE
WHEN validTo IS NULL THEN 1
WHEN verified=0 THEN 1
WHEN SR.CredRedAlertOn<>0 AND DATEADD(DAY,SR.credRedAlertDays,validTo)<GETDate() THEN  1 
WHEN SR.CredAmberAlertOn<>0 AND DATEADD(DAY,SR.credAmberAlertDays,validTo)<GETDate() THEN  2
END AS priority
FROM
(
SELECT DISTINCT I.prId,I.fn,I.ln,ICI.credName,ICI.credTypeId,IC.credId,IC.verified,IC.validTo,IC.docId,IC.verificationDate,S.fn+' '+S.ln AS verifier,
ROW_NUMBER() OVER(PARTITION BY I.prId,ICI.credTypeId ORDER BY IC.verified ASC,IC.validTo DESC)AS R
FROM StaffList AS I
JOIN U ON U.coId=32 AND U.prId=I.prId 
JOIN CredentialIds AS ICI ON(U.isSuperAdmin<>0 AND ICI.superadmin<>0)OR(U.isHumanResources<>0 AND ICI.humanResources<>0)OR(U.isDirector<>0 AND ICI.director<>0)OR(U.isAssistantDirector<>0 AND ICI.assistantdirector<>0)OR(U.isSupervisor<>0 AND ICI.supervisor<>0)OR(U.isProvider<>0 AND ICI.provider<>0)OR(I.profLicReq<>0 AND ICI.credTypeId=7)OR(I.profLiabilityReq<>0 AND ICI.credTypeId=8)OR(I.providesTransport='Y' AND ICI.credTypeId=9)OR(I.ownVehicle='Y' AND (ICI.credTypeId=10 OR ICI.credTypeId=11)) 
LEFT JOIN StaffCredentials AS IC ON IC.prId=I.prId AND IC.credTypeId=ICI.credTypeId
LEFT JOIN Staff AS S ON S.prid=IC.verifier
) AS X 
JOIN StaffRoles AS SR ON SR.rolenominal='provider' 
WHERE R=1 AND 
((SR.CredAmberAlertOn<>0 AND (validTo IS NULL OR verified=0 OR DATEADD(DAY, SR.CredAmberAlertDays, validTo)<GETDate()))OR
(SR.CredRedAlertOn<>0 AND (validTo IS NULL OR verified=0 OR DATEADD(DAY, SR.CredRedAlertDays, validTo)<GETDate()))) 
ORDER BY ln ASC,fn ASC,verified asc,R asc,credname ASC;



