
SELECT X.rat,x.svc,SUM(x.un)AS un
FROM  PayPeriods AS PP
LEFT JOIN(
SELECT dt,svc,3 AS rat,(un+ajun)AS un FROM ClientHours WHERE clsvid=35 AND(un+ajun>0)
UNION ALL
SELECT dt,svc,rat,(un+ajun)AS un FROM HCBSHrsBill WHERE clsvid=35 AND(un+ajun>0)AND billASGroup IS NULL
UNION ALL
SELECT DISTINCT dt,svc,rat,CAST(12 AS DECIMAL)AS un FROM HCBSHrsBill WHERE clsvid=35 AND billASGroup IS NOT NULL) AS X ON X.dt>=PP.s AND X.dt<=PP.e
WHERE PP.s<='8/11/2018' AND pp.e>='8/11/2018' GROUP BY x.svc,x.rat;




WITH StaffList AS
(SELECT S1.prid,1 AS empLevel
FROM Staff AS S1
WHERE prid= 828
UNION ALL
SELECT S2.prid,SL.empLevel+1 AS empLevel
FROM Staff AS S2
INNER JOIN StaffList AS SL
ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId
WHERE S2.prid<>0 AND S2.prID IS NOT NULL)
SELECT x.rat,x.svc,SUM(x.un)AS un
FROM StaffList
JOIN ClientStaffRelationships AS CSR ON CSR.prid=StaffList.prid AND clsvid=35
JOIN ClientServices AS CS ON CS.id=CSR.clsvidId
JOIN PayPeriods AS PP ON PP.s<='8/11/2018' AND pp.e>='8/11/2018'
LEFT JOIN(
SELECT dt,svc,3 AS rat,(un+ajun)AS un FROM ClientHours WHERE clsvid=35 AND(un+ajun>0)
UNION ALL
SELECT dt,svc,rat,(un+ajun)AS un FROM HCBSHrsBill WHERE clsvid=35 AND(un+ajun>0)AND billASGroup IS NULL
UNION ALL
SELECT DISTINCT dt,svc,rat,CAST(12 AS DECIMAL)AS un FROM HCBSHrsBill WHERE clsvid=35 AND billASGroup IS NOT NULL) AS X ON X.dt>=PP.s AND X.dt<=PP.e AND X.svc=CS.svc 
GROUP BY x.rat,x.svc;


  

  
