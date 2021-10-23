 
SELECT DISTINCT 0 AS ID,ipr.prid,C.dt,C.un AS hrs,'' AS nt,
CASE
WHEN C.svc='RSP' THEN 'RE'+CAST(C.rat AS nvarchar(1))
WHEN C.svc='HAH' THEN 'HA'+CAST(C.rat AS nvarchar(1))
WHEN C.svc='ATC' THEN 'AT'+CAST(C.rat AS nvarchar(1))
END AS cd,



0 AS locID,0 AS prhomedeptID, u1.uid,E.geoLat1 AS lat1,E.geoLat2 AS lat2,E.geoLon1 AS lon1,E.geoLon2 AS lon2, u2.uid AS uid2,C.in1 AS inx,C.out1 AS outx,0 AS tid,'HCBS' AS dept, ipr.ln+','+ipr.fn AS nm,u1.ln+', '+u1.fn AS unm1,u2.ln+', '+u2.fn AS unm2,''AS tcnm1,''AS tcnm2,CAST(1 AS BIT)AS isHCBS FROM THPLipr AS ipr JOIN THPLpayperiod AS pp ON pp.ppid=32 JOIN THPLHCBSHrsClient AS C ON C.prid=ipr.prid AND C.dt>=pp.s AND C.dt<=pp.e JOIN THPLHCBSHrsEmp AS E ON E.HCBSHrsEmpID=C.HCBSHrsEmpID AND E.pd<>4 LEFT JOIN u AS u1 ON u1.uid=E.uid1 LEFT JOIN u AS u2 ON u2.uid=E.uid2 WHERE ipr.prid=185 ORDER BY dt ASC,inx ASC;