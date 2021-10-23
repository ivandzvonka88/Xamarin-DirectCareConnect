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
SELECT CSR.relId,C.fn AS cfn,C.ln AS cln,CS.svc,S.fn AS sfn,s.ln AS sln,atcrelationship,CSR.prid,CSR.clsvid,CSR.clsvidid
FROM StaffList
JOIN ClientStaffRelationships AS CSR ON CSR.prId=StaffList.prId
JOIN Clients AS C ON C.clsvid=CSR.clsvId AND C.deleted=0
JOIN ClientServices AS CS ON CS.id=CSR.clsvidId AND CS.deleted=0
JOIN Staff AS S ON S.prId=CSR.prId
LEFT JOIN ATCRelationships AS A ON A.atcRelId=CSR.pridr
WHERE CSR.prId=StaffList.prId
ORDER BY cln ASC,cfn ASC,svc ASC


