USE [DDDEZ]

/* Set to Az general */
UPDATE THPLcl SET billRegId=1;
/* sets resets date to null that were previously set to nulldate or N/A date 
ALTER TABLE THPLipr ALTER COLUMN termdt date NULL;
update THPLipr Set termdt=null where termdt='1/1/2110' OR termdt='1/1/2111';
ALTER TABLE THPLipr ALTER COLUMN hiredtf date NULL;
update THPLipr Set hiredtf=null where hiredtf='1/1/2110' OR hiredtf='1/1/2111';
ALTER TABLE THPLipr ALTER COLUMN dobf date NULL;
update THPLipr Set dobf=null where dobf='1/1/2110' OR dobf='1/1/2111';
ALTER TABLE THPLipr ALTER COLUMN crverf date NULL;
update THPLipr Set crverf=null where crverf='1/1/2110' OR crverf='1/1/2111';
ALTER TABLE THPLipr ALTER COLUMN faexpf date NULL;
update THPLipr Set faexpf=null where faexpf='1/1/2110' OR faexpf='1/1/2111';
*/
/* clean up for staff table */
UPDATE THPLipr SET providerhome='N' WHERE providerhome='0';
UPDATE THPLipr SET ownvehicle='N' WHERE ownvehicle='';
UPDATE THPLipr SET providesTransport='N' WHERE providesTransport='';
UPDATE THPLipr SET profLicReq=0,profLiabilityReq=0;

UPDATE THPLipr SET prdeptId=1; /* THPL only */
UPDATE ClientStaffRelationships SET pridr=1 WHERE pridr=0;
UPDATE ClientStaffRelationships SET pridr=2 WHERE pridr=3;
UPDATE ClientStaffRelationships SET pridr=3 WHERE pridr=4;
UPDATE ClientStaffRelationships SET pridr=4 WHERE pridr=5;
/* Employment need to get this right xxxxxxxxx */
UPDATE THPLemphrs SET deptId=1;

/* initialize user */
UPDATE u SET isSuperAdmin=0, isHumanResources=0,isDirector=0,isAssistantDirector=0,isSupervisor=0,isProvider=0;
UPDate u SET isSuperAdmin=1 WHERE email='thpl@azweb-tek.com' OR email='office@arizonaautism.com' /* me and ryan */
UPDATE u SET isProvider=1 WHERE sTSipr<>0 AND coid=32
/* makes sure everyone is registered */
UPDATE u SET username=email
UPDATE u SET registered= 1 where coid=32;
/* Lets start clean
Do we really want to do this???????????xxxxxxxx
DELETE FROM THPLipr where deleted<>0
DELETE FROM u where deleted<>0
 */
/* If user does not have an employee account - give him one */
Declare @fn varchar(100);
declare @ln varchar(100);
declare @uid integer;
declare @deleted bit;
Declare @deletedDate Date;
Declare @em varchar(100);
Declare @pridNew integer;

UPDATE u SEt processed=0 WHERE coid=32;
While (Select Count(*) From u WHERE processed=0 AND coid=32 AND prid=0) > 0
Begin
    Select Top 1 @fn=fn,@ln=ln,@uid=uID,@deleted=deleted,@em=email From u WHERE processed=0 AND prid=0 AND coid=32;
	if @deleted<>0 
		SET @deletedDate=GetDate();
	Else
		SET @deletedDate = DATEADD(year, 100, GetDate())
	         INSERT INTO THPLipr(
            ln,
            fn,
            ad1,
            ad2,
            cty,
            st,
            z,
            Sex,
            ph,
            cl,
            em,
            ssnf,
            dobf,
            ahcccsID,
            npi,
            deleted,
            deldt,
            hiredtf,
            termdt,
            CRverf,
            faexpf,
            ttlf,
            rlYf,
            apYf,
            EmployeeType,
            PTFP,
            isSalary,
            profLicReq,
            profLiabilityReq,
            prdeptId,
            eID,
            Classification,
            ProviderHome,
            ProvidesTransport,
            ownVehicle,
            refOfficeId,
            linkedSSN,
            irExemption,
            supPrid,
			tempsupPrid
			)VALUES(
			@ln,
			@fn,
			'',
			'',
			'',
			'',
			'',
			'',
			'',
			'',
            @em,
			'',
            null,
			'',
			'',
            @deleted,
			@deletedDate,
			null,
			null,
			null,
			null,
			'',
			0,
			0,
			'',
			0,
			1,
            0,
            0,
			1, /*pr dept*/
            '', /* eid */
			'', /* classification*/           
            'N',
            'N',
            'N',
            '',
			'',
            'N',
            NULL,
			NULL);SELECT @pridNew =@@Identity;
	Update u Set Processed=1,prid=@pridNew Where uid=@uid; 
End


/* Update employee tables if necessary */
Declare @pridx integer;
Declare @uidx integer;
Declare @isSuperAdmin smallint;
Declare @isHumanResources smallint;
Declare @isDirector smallint;
Declare @isAssistantDirector smallint;
declare @isSupervisor smallint;
declare @isProvider smallint;
UPDATE THPLipr SEt processed=0;
While (Select Count(*) From THPLipr JOIN u ON U.prid=THPLipr.prid AND u.coid=32 WHERE THPLipr.processed=0 AND THPLipr.isSuperAdmin IS NULL) > 0
	Begin
	SELECT @pridx=THPLipr.prid,@uidx=U.uid,@isSuperAdmin=U.isSuperAdmin,@isHumanResources=U.isHumanResources,@isDirector=U.isDirector,@isAssistantDirector=U.isAssistantDirector,@isSupervisor=U.isSupervisor,@isprovider=U.isprovider     
	FROM THPLipr
	JOIN u ON U.prid=THPLipr.prid AND U.coid=32
	WHERE THPLipr.processed=0
	UPDATE THPLipr SET userID=@uidx,isSuperAdmin=@isSuperAdmin,isHumanResources=@isHumanResources,isDirector=@isDirector,isAssistantDirector=@isAssistantDirector,isSupervisor=@isSupervisor,isProvider=@isprovider,registered=1,processed=1 WHERE prid=@pridx
	End
/* xxxxxxx might get staff without user accounts */
SELECT THPLipr.prid,thplipr.userid AS iprUid,u.prid,u.uid,u.deleted AS uiddeleted,u.coid FROM THPLipr 
LEFT JOIN u ON u.prid=THPLipr.prid AND u.coid=32
WHERE thplipr.userid IS NULL

/* Set client-provider relationships */

DELETE FROM ClientStaffrelationships
Declare @prId1 integer
Declare @prId1r integer
Declare @prId2 integer
Declare @prId2r integer
Declare @prId3 integer
Declare @prId3r integer
Declare @prId4 integer
Declare @prId4r integer
Declare @prId5 integer
Declare @prId5r integer
Declare @prId6 integer
Declare @prId6r integer
Declare @prId7 integer
Declare @prId7r integer
Declare @prId8 integer
Declare @prId8r integer
Declare @prId9 integer
Declare @prId9r integer
Declare @prId10 integer
Declare @prId10r integer
Declare @prId11 integer
Declare @prId11r integer
Declare @prId12 integer
Declare @prId12r integer
Declare @prId13 integer
Declare @prId13r integer
Declare @prId14 integer
Declare @prId14r integer
Declare @prId15 integer
Declare @prId15r integer
Declare @clsvid integer
Declare @clsvidid integer
UPDATE THPLclsv SEt processed=0;
While (Select Count(*) From THPLclsv WHERE deleted=0 AND processed=0) > 0
Begin
	SELECT TOP 1 @clsvId=clsvid,@clsvidId=id
	,@prid1=prid1,@prid1r=prid1r  
	,@prid2=prid2,@prid2r=prid2r 
	,@prid3=prid3,@prid3r=prid3r 
	,@prid4=prid4,@prid4r=prid4r  
	,@prid5=prid5,@prid5r=prid5r
	,@prid6=prid6,@prid6r=prid6r 
	,@prid7=prid7,@prid7r=prid7r 
	,@prid8=prid8,@prid8r=prid8r  
	,@prid9=prid9,@prid9r=prid9r   
	,@prid10=prid10,@prid10r=prid10r 
	,@prid11=prid11,@prid11r=prid11r 
	,@prid12=prid12,@prid12r=prid12r 
	,@prid13=prid13,@prid13r=prid13r  
	,@prid14=prid14,@prid14r=prid14r  
	,@prid15=prid15,@prid15r=prid15r
	FROM THPLclsv WHERE deleted=0 AND processed=0
	If @prid1 IS NOT NULL AND @prid1<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid1,@prid1r);
	END
	If @prid2 IS NOT NULL AND @prid2<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid2,@prid2r);
	END
	If @prid3 IS NOT NULL AND @prid3<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid3,@prid3r);
	END
	If @prid4 IS NOT NULL AND @prid4<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid4,@prid4r);
	END
	If @prid5 IS NOT NULL AND @prid5<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid5,@prid5r);
	END
	If @prid6 IS NOT NULL AND @prid6<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid6,@prid6r);
	END
	If @prid7 IS NOT NULL AND @prid7<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid7,@prid7r);
	END
	If @prid8 IS NOT NULL AND @prid8<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid8,@prid8r);
	END
	If @prid9 IS NOT NULL AND @prid9<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid9,@prid9r);
	END
	If @prid10 IS NOT NULL AND @prid10<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid10,@prid10r);
	END
	If @prid11 IS NOT NULL AND @prid11<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid11,@prid11r);
	END
	If @prid12 IS NOT NULL AND @prid12<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid12,@prid12r);
	END
	If @prid13 IS NOT NULL AND @prid13<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid13,@prid13r);
	END
	If @prid14 IS NOT NULL AND @prid14<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid14,@prid14r);
	END
	If @prid15 IS NOT NULL AND @prid15<>0
	BEGIN
		INSERT INTO ClientStaffrelationships(clsvid,prid,pridr)VALUES(@clsvid,@prid15,@prid15r);
	END
	Update THPLclsv Set Processed=1 Where id=@clsvidid; 
END
Declare @relId integer
WHILE(
(SELECT COUNT(*) FROM(
SELECT relId,ROW_NUMBER() OVER(PARTITION BY prid,clsvId ORDER BY pridr DESC) AS ROw 
FROM ClientStaffRelationships) AS A WHERE ROW<>1)<>0)
BEGIN 
	SELECT TOP 1 @relId=relId FROM(
		SELECT relId,ROW_NUMBER() OVER(PARTITION BY prid,clsvId ORDER BY pridr DESC) AS ROw 
		FROM ClientStaffRelationships) AS A 
	WHERE ROW<>1 ORDER BY relID ASC
	DELETE FROM clientstaffRelationships WHERE relId=@relId

END

/* Set Client Worker Stuff */
UPDATE THPLcl
SET THPLcl.clwEm = B.em,THPLcl.clwPh = B.ph,THPLcl.clwNm = B.nm
FROM THPLcl
INNER JOIN
(SELECT clsvid,em,nm,ph FROM
	(SELECT C.clsvid,CO.em,CO.nm,CO.ph, ROW_NUMBER() OVER(Partition by clsvid ORDER BY eddt DESC) AS R
	FROM THPLcl AS C
	JOIN  THPLau AS A ON A.clid=C.clid
	JOIN THPLcord AS CO ON CO.cid=A.cid
	) AS A WHERE R=1
) AS B
ON B.clsvid = THPLcl.clsvid;

