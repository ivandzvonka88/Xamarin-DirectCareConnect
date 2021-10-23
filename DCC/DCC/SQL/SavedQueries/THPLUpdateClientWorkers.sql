UPDATE
  THPLcl
SET
  THPLcl.clwEm = B.em,
   THPLcl.clwPh = B.ph,
    THPLcl.clwNm = B.nm,
FROM
 THPLcl
INNER JOIN
(
 SELECT clsvid,em,nm,ph FROM (
SELECT C.clsvid,CO.em,CO.nm,CO.ph, ROW_NUMBER() OVER(Partition by clsvid ORDER BY eddt DESC) AS R
FROM THPLcl AS C
JOIN  THPLau AS A ON A.clid=C.clid
JOIN THPLcord AS CO ON CO.cid=A.cid
) AS A WHERE R=1
) AS B
ON
  B.clsvid = THPLcl.clsvid






SELECT clsvid,em,nm,ph FROM (
SELECT C.clsvid,CO.em,CO.nm,CO.ph, ROW_NUMBER() OVER(Partition by clsvid ORDER BY eddt DESC) AS R
FROM THPLcl AS C
JOIN  THPLau AS A ON A.clid=C.clid
JOIN THPLcord AS CO ON CO.cid=A.cid
) AS A WHERE R=1
ORDER BY clsvid ASC
