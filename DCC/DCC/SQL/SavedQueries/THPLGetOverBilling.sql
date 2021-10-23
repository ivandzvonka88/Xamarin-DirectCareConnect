
/* The date should be 1 week earlier */
SELECT P.s,P.e,H.* 
FROM payPeriods AS P
JOIN HCBSHrsClient AS H ON H.prId=830 AND H.dt>=P.s AND H.dt<=P.e
WHERE s<='4/19/2019' AND e>='4/19/2019'


