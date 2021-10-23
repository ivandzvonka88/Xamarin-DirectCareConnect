/* Get notes waiting approval */
SELECT C.fn AS cfn,C.ln AS cln,S.fn AS sfn,S.ln AS sln,CN.clsvId,CN.clsvidId,CN.prId,CN.due,CN.svc 
FROM ClientNotes AS CN
JOIN Clients AS C ON C.clsvid=CN.clsvId
JOIN Staff AS S ON S.prId=CN.prId
JOIN StaffRoles AS SR ON SR.roleNominal='director'
WHERE CN.noteWritten<>0 AND CN.approved=0;


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
SELECT C.fn AS cfn,C.ln AS cln,S.fn AS sfn,S.ln AS sln,CN.clsvId,CN.clsvidId,CN.prId,CN.due,CN.svc 
FROM staffList
JOIN ClientNotes AS CN ON CN.noteWritten=0 AND CN.prId=StaffList.prID
JOIN Clients AS C ON C.clsvid=CN.clsvId
JOIN Staff AS S ON S.prId=CN.prId
JOIN StaffRoles AS SR ON SR.roleNominal='director'
WHERE CN.noteWritten<>0 AND CN.approved=0


