/* Get auth Alerts for super admin/human resources */
SELECT clsvid,cfn,cln,svc,eddt, 
Case 
WHEN SR.AuthRedAlertOn<>0 AND DATEADD(DAY,SR.AuthRedAlertDays,eddt)< GETDate()THEN 1 
WHEN SR.AuthAmberAlertOn<>0 AND DATEADD(DAY,SR.AuthAmberAlertDays,eddt)< GETDate()THEN 2 
ELSE 0 
END AS Priority
FROM(
SELECT C.clsvid,C.fn AS cfn,C.ln AS cln,CS.svc,A.eddt,
ROW_NUMBER()OVER(PARTITION BY CS.id ORDER BY eddt DESC)AS Row
FROM ClientServices AS CS
JOIN Clients AS C ON C.deleted=0 AND C.clsvId=CS.clsvId
LEFT JOIN ClientAuths AS A ON A.clid=C.clId AND  A.secode=CS.svc
WHERE CS.deleted=0
)AS X
JOIN StaffRoles AS SR ON SR.rolenominal='director' 
WHERE Row=1 AND
((SR.AuthAmberAlertOn<>0 AND DATEADD(DAY, SR.AuthAmberAlertDays, eddt)<GETDate())OR
(SR.AuthRedAlertOn<>0 AND DATEADD(DAY, SR.AuthRedAlertDays, eddt)<GETDate())) 
ORDER BY cln ASC, cfn ASC, svc ASC;



/* In hierarchy */
WITH StaffList AS
(SELECT S1.prid,1 AS empLevel
FROM Staff AS S1
WHERE prid= 828
UNION ALL
SELECT S2.prid,SL.empLevel+1 AS empLevel
FROM Staff AS S2
INNER JOIN StaffList AS SL
ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId
WHERE S2.prid<>0 AND S2.prID IS NOT NULL
)
SELECT X.*,
Case 
WHEN SR.AuthRedAlertOn<>0 AND DATEADD(DAY,SR.AuthRedAlertDays,eddt)<GETDate() THEN 1 
WHEN SR.AuthAmberAlertOn<>0 AND DATEADD(DAY,SR.AuthAmberAlertDays,eddt)<GETDate() THEN 2
ELSE 0  
END AS Priority
FROM(
SELECT C.clsvId,C.fn AS cln,C.ln AS cfn,CS.svc,A.eddt,
ROW_NUMBER() OVER(PARTITION BY CS.id ORDER BY eddt DESC) AS Row
FROM StaffList
JOIN ClientStaffRelationships AS RS ON RS.prId=StaffList.prId
JOIN Clients AS C ON C.clsvid=RS.clsvId AND C.deleted=0
JOIN ClientServices AS CS ON CS.id=RS.clsvidId AND CS.deleted=0
LEFT JOIN ClientAuths AS A ON A.clid=C.clId AND A.secode=CS.svc				
)AS X  
JOIN StaffRoles AS SR ON SR.rolenominal='provider' 
WHERE Row=1 AND
((SR.AuthAmberAlertOn<>0 AND DATEADD(DAY,SR.AuthAmberAlertDays,eddt)<GETDate())OR
(SR.AuthRedAlertOn<>0 AND DATEADD(DAY,SR.AuthRedAlertDays,eddt)<GETDate())) 
ORDER BY cln ASC, cfn ASC, svc ASC;     

