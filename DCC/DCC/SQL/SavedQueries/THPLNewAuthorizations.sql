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
SELECT CSR.clsvId,CSR.clsvidId,C.ln+' '+C.fn AS nm,CS.svc,CA.stdt,CA.eddt,CA.au
FROM staffList AS I
JOIN ClientStaffRelationships AS CSR ON CSR.prId=I.prId
JOIN Clients AS C ON C.clsvId=CSR.clsvId AND C.deleted=0
JOIN ClientServices AS CS ON CS.id=CSR.clsvidId AND CS.deleted=0
JOIN ClientAuths AS CA ON CA.clid=C.clId AND CA.seCode=CS.svc AND CA.dtRcvd>DATEADD(DAY,-4,GETDATE())
ORDER BY nm ASC,svc ASC
