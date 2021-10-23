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
SELECT DISTINCT C.* FROM staffList AS I
JOIN ClientStaffRelationships AS CSR ON CSR.prId= I.prId
JOIN Clients AS C ON C.clsvId=CSR.clsvId
LEFT JOIN Guardians AS G ON G.gId=C.gId1 OR G.gId=C.gId2 OR G.gId=C.gId3
WHERE CSR.prid=I.prId AND CSR.clsvId=1607









WITH StaffList AS
(SELECT S1.prid,S1.fn,S1.ln,S1.profLicReq,S1.profLiabilityReq,S1.providesTransport,S1.ownVehicle,1 AS empLevel
FROM Staff AS S1
WHERE prid=830
UNION ALL
SELECT S2.prid,S2.fn,S2.ln,S2.profLicReq,S2.profLiabilityReq,S2.providesTransport,S2.ownVehicle,SL.empLevel+1 AS empLevel
FROM Staff AS S2
INNER JOIN StaffList AS SL
ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId
WHERE S2.prid<>0 AND S2.prID IS NOT NULL
)
SELECT DISTINCT CS.*,D.deptId,D.name AS deptName FROM ClientStaffRelationships AS CSR 
JOIN ClientServices AS CS ON CS.id=CSR.clsvIdId
JOIN ServiceLocations AS CL ON CL.locId=CS.locId
LEFT JOIN Departments AS D ON D.deptId=CL.deptId
WHERE CSR.prid=830 AND CSR.clsvId=1607 ORDER BY svc ASC;



WITH StaffList AS
(SELECT S1.prid,S1.fn,S1.ln,S1.profLicReq,S1.profLiabilityReq,S1.providesTransport,S1.ownVehicle,1 AS empLevel
FROM Staff AS S1
WHERE prid=830
UNION ALL
SELECT S2.prid,S2.fn,S2.ln,S2.profLicReq,S2.profLiabilityReq,S2.providesTransport,S2.ownVehicle,SL.empLevel+1 AS empLevel
FROM Staff AS S2
INNER JOIN StaffList AS SL
ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId
WHERE S2.prid<>0 AND S2.prID IS NOT NULL
)
SELECT DISTINCT CS.id AS clsvidId,CA.auid,CA.stdt,CA.eddt,CA.au,CA.uu,CA.ru,ISNULL(X.o,0)AS o FROM StaffList AS I
JOIN clientStaffRelationships AS CSR ON CSR.prId=I.prId AND CSR.clsvId=1607
JOIN Clients AS C ON C.clsvId=CSR.clsvId
JOIN ClientServices AS CS ON CS.clsvid=C.clsvid
JOIN ClientAuths AS CA ON CA.clid=C.clid AND CA.secode=CS.svc AND CA.eddt>DATEADD(MONTH, -6,GETDATE())
LEFT JOIN
(SELECT au.auid,SUM(un)AS o FROM(
SELECT svc,dt,(un+ajun)AS un FROM ClientHours WHERE clsvid=1607 AND(un+ajun>0)AND(pd<>1 AND pd<>3)
UNION ALL
SELECT svc,dt,(un+ajun)AS un FROM ClientDays WHERE clsvid=1607 AND(un+ajun>0)AND(pd<>1 AND pd<>3)
UNION ALL
SELECT svc,dt,(un+ajun)AS un FROM HCBSHrsBill WHERE clsvid=1607 AND(un+ajun>0)AND(pd<>1 AND pd<>3)AND billASGroup IS NULL
UNION ALL
SELECT DISTINCT svc,dt,CAST(12 AS DECIMAL)AS un FROM HCBSHrsBill WHERE clsvid=1607 AND(pd<>1 AND pd<>3)AND billASGroup IS NOT NULL
)AS A
JOIN Clients AS cl ON cl.clsvid=1607
LEFT JOIN ClientAuths AS au ON au.clid=cl.clid AND au.secode=A.svc AND A.dt>=au.stdt AND A.dt<=au.eddt
GROUP BY auid)AS X ON x.auid=CA.auid;

WITH StaffList AS
(SELECT S1.prid,S1.fn,S1.ln,S1.profLicReq,S1.profLiabilityReq,S1.providesTransport,S1.ownVehicle,1 AS empLevel
FROM Staff AS S1
WHERE prid=830
UNION ALL
SELECT S2.prid,S2.fn,S2.ln,S2.profLicReq,S2.profLiabilityReq,S2.providesTransport,S2.ownVehicle,SL.empLevel+1 AS empLevel
FROM Staff AS S2
INNER JOIN StaffList AS SL
ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId
WHERE S2.prid<>0 AND S2.prID IS NOT NULL
)
SELECT DISTINCT SPR.* FROM ClientStaffRelationships AS CSR 
JOIN ClientSpecialRates AS SPR ON SPR.clsvIdId=CSR.clsvidId
WHERE CSR.prid=830 AND CSR.clsvId=1607;

