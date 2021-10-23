/* step 1 Run this on DDDEZ */


USE [DDDEZ]
GO
Declare @pCode varchar(50)
declare @coId integer;
UPDATE co SEt processed=0;
While (Select Count(*) From co WHERE processed=0 AND pCode<>'THPL') > 0
Begin
    Select Top 1 @pCode=pCode,@coId=coID From co  WHERE processed=0 AND pCode<>'THPL';
		DECLARE @SQLString NVARCHAR(MAX)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'loc';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'au';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'auev';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'autemp';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'bi';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'cl';
		EXEC (@SQLString)
			SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'cldetail';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'cldetailmod';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'clhrs';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'clprcat';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'clprjobcodes';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'clprnts';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'clsv';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'cord';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'ct';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'dys';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'dyshab';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'dysrrb';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'emphrs';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'ghnotes';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'ghsched';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'ghws';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'hcbshrsbill';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'hcbshrsclient';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'hcbshrsemp';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'hrs';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'hrsH';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'ipr';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'oa';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'payperiod';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'prdept';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'prdept';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'prdistricts';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'rc';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'rcdt';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'rcev';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'rctemp';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'ridopts';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'spRates';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'wks';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'cllocations';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'iprcredentials';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'iprcomments';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'clcomments';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'clccomments';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'deptpayroll';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'dept';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'roles';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'clcharts';
		EXEC (@SQLString)
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'clipr';
		EXEC (@SQLString)
		
		SET @SQLString = 'DROP TABLE IF EXISTS ' + @pCode + 'clsvclocations';
		EXEC (@SQLString)
		DELETE FROM co WHERE pCode=@pcode;
		DELETE FROM u WHERE coId=@coId;
		Update co Set Processed=1 Where pcode=@pcode; 
End
/* Drop other tables */
DROP TABLE IF EXISTS coordinatorstbl;
DROP TABLE IF EXISTS FPReaders;
DROP TABLE IF EXISTS ghtype;
DROP TABLE IF EXISTS pos;
DROP TABLE IF EXISTS poscodes;
DROP TABLE IF EXISTS sf;
DROP TABLE IF EXISTS poscodes;
DROP TABLE IF EXISTS sprates;
DROP TABLE IF EXISTS ulog;
DROP TABLE IF EXISTS timeclocks;
DROP TABLE IF EXISTS timeclocklocs;
DROP TABLE IF EXISTS THPLoa;
DROP TABLE IF EXISTS calctherapy;
DROP TABLE IF EXISTS MARCHCBSEmpHrs;
DROP TABLE IF EXISTS MARCHrsHCBS;
DROP TABLE IF EXISTS my;
DROP TABLE IF EXISTS stat;
DROP TABLE IF EXISTS test;
DROP TABLE IF EXISTS credentialIds;
DROP TABLE IF EXISTS dddofficestbl;
DROP TABLE IF EXISTS em;
DROP TABLE IF EXISTS g;
DROP TABLE IF EXISTS cllocations;
DROP TABLE IF EXISTS marchrshB;

/* initialize user */
UPDATE u SET isSuperAdmin=0, isHumanResources=0,isDirector=0,isAssistantDirector=0,isSupervisor=0,isProvider=0;
UPDate u SET isSuperAdmin=1 WHERE email='thpl@azweb-tek.com' OR email='office@arizonaautism.com' /* me and ryan */
UPDATE u SET isdirector=4 WHERE ua<>0 AND isSuperadmin=0;
UPDATE u SET isProvider=1 WHERE sTSipr<>0 AND isSuperadmin=0 AND isDirector=0;
/* makes sure everyone is registered */
UPDATE u SET username=email
UPDATE u SET registered= 1;
/* Lets start clean
Do we really want to do this???????????ZZZz
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

UPDATE u SEt processed=0;
While (Select Count(*) From u WHERE processed=0 AND prid=0) > 0
Begin
    Select Top 1 @fn=fn,@ln=ln,@uid=uID,@deleted=deleted,@em=email From u  WHERE processed=0 AND prid=0;
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


UPDATE THPLipr SEt processed=0;
Declare @pridx integer;
Declare @uidx integer;
Declare @isSuperAdmin smallint;
Declare @isHumanResources smallint;
Declare @isDirector smallint;
Declare @isAssistantDirector smallint;
declare @isSupervisor smallint;
declare @isProvider smallint;
While (Select Count(*) From THPLipr JOIN u ON U.prid=THPLipr.prid WHERE THPLipr.processed=0) > 0
	Begin
	SELECT @pridx=THPLipr.prid,@uidx=U.uid,@isSuperAdmin=U.isSuperAdmin,@isHumanResources=U.isHumanResources,@isDirector=U.isDirector,@isAssistantDirector=U.isAssistantDirector,@isSupervisor=U.isSupervisor,@isprovider=U.isprovider     
	FROM THPLipr
	JOIN u ON U.prid=THPLipr.prid 
	WHERE THPLipr.processed=0
	UPDATE THPLipr SET uID=@uidx,isSuperAdmin=@isSuperAdmin,isHumanResources=@isHumanResources,isDirector=@isDirector,isAssistantDirector=@isAssistantDirector,isSupervisor=@isSupervisor,isProvider=@isprovider,registered=1,processed=1 WHERE prid=@pridx
	End
/* xxxxxxx might get staff without user accounts */
SELECT THPLipr.prid,thplipr.uid AS iprUid,u.prid,u.uid,u.deleted AS uiddeleted,u.coid FROM THPLipr 
LEFT JOIN u ON u.prid=THPLipr.prid AND u.coid=32
WHERE thplipr.uid IS NULL

/* Set client-provider relationships */
DELETE FROM ClientStaffRelationships
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
Declare @clsvidID integer
UPDATE THPLclsv SEt processed=0;
While (Select Count(*) From THPLclsv WHERE deleted=0 AND processed=0) > 0
Begin
	SELECT TOP 1 @clsvidid=id,@clsvId=clsvid
	,@prid1=prid1,@prid1r=CASE WHEN svc<>'ATC' AND prid1r=0 THEN 1 ELSE prid1r END  
	,@prid2=prid2,@prid2r=CASE WHEN svc<>'ATC' AND prid2r=0 THEN 1 ELSE prid2r END  
	,@prid3=prid3,@prid3r=CASE WHEN svc<>'ATC' AND prid3r=0 THEN 1 ELSE prid3r END  
	,@prid4=prid4,@prid4r=CASE WHEN svc<>'ATC' AND prid4r=0 THEN 1 ELSE prid4r END  
	,@prid5=prid5,@prid5r=CASE WHEN svc<>'ATC' AND prid5r=0 THEN 1 ELSE prid5r END  
	,@prid6=prid6,@prid6r=CASE WHEN svc<>'ATC' AND prid6r=0 THEN 1 ELSE prid6r END  
	,@prid7=prid7,@prid7r=CASE WHEN svc<>'ATC' AND prid7r=0 THEN 1 ELSE prid7r END  
	,@prid8=prid8,@prid8r=CASE WHEN svc<>'ATC' AND prid8r=0 THEN 1 ELSE prid8r END  
	,@prid9=prid9,@prid9r=CASE WHEN svc<>'ATC' AND prid9r=0 THEN 1 ELSE prid9r END   
	,@prid10=prid10,@prid10r=CASE WHEN svc<>'ATC' AND prid10r=0 THEN 1 ELSE prid10r END  
	,@prid11=prid11,@prid11r=CASE WHEN svc<>'ATC' AND prid11r=0 THEN 1 ELSE prid11r END  
	,@prid12=prid12,@prid12r=CASE WHEN svc<>'ATC' AND prid12r=0 THEN 1 ELSE prid12r END  
	,@prid13=prid13,@prid13r=CASE WHEN svc<>'ATC' AND prid13r=0 THEN 1 ELSE prid13r END  
	,@prid14=prid14,@prid14r=CASE WHEN svc<>'ATC' AND prid14r=0 THEN 1 ELSE prid14r END  
	,@prid15=prid15,@prid15r=CASE WHEN svc<>'ATC' AND prid15r=0 THEN 1 ELSE prid15r END  
	FROM THPLclsv WHERE deleted=0 AND processed=0
	If @prid1 IS NOT NULL AND @prid1<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid1,@prid1r);
	END
	If @prid2 IS NOT NULL AND @prid2<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid2,@prid2r);
	END
	If @prid3 IS NOT NULL AND @prid3<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid3,@prid3r);
	END
	If @prid4 IS NOT NULL AND @prid4<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid4,@prid4r);
	END
	If @prid5 IS NOT NULL AND @prid5<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid5,@prid5r);
	END
	If @prid6 IS NOT NULL AND @prid6<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid6,@prid6r);
	END
	If @prid7 IS NOT NULL AND @prid7<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid7,@prid7r);
	END
	If @prid8 IS NOT NULL AND @prid8<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid8,@prid8r);
	END
	If @prid9 IS NOT NULL AND @prid9<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid9,@prid9r);
	END
	If @prid10 IS NOT NULL AND @prid10<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid10,@prid10r);
	END
	If @prid11 IS NOT NULL AND @prid11<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid11,@prid11r);
	END
	If @prid12 IS NOT NULL AND @prid12<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid12,@prid12r);
	END
	If @prid13 IS NOT NULL AND @prid13<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid13,@prid13r);
	END
	If @prid14 IS NOT NULL AND @prid14<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid14,@prid14r);
	END
	If @prid15 IS NOT NULL AND @prid15<>0
	BEGIN
		INSERT INTO ClientStaffRelationships(clsvid,clsvidid,prid,pridr)VALUES(@clsvid,@clsvidId,@prid15,@prid15r);
	END
	Update THPLclsv Set Processed=1 Where id=@clsvidid; 
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


/* Tested to here */



/* Set to Az general */
UPDATE THPLcl SET billRegId=1;
/* sets resets date to null that were previously set to nulldate or N/A date */
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
/* clean up for staff table */
UPDATE THPLipr SET providerhome='N' WHERE providerhome='0';
UPDATE THPLipr SET ownvehicle='N' WHERE ownvehicle='';
UPDATE THPLipr SET providesTransport='N' WHERE providesTransport='';
UPDATE THPLipr SET profLicReq=0,profLiabilityReq=0;



UPDATE THPLipr SET prdeptId=1; /* THPL only */
UPDATE ClientStaffRelationships SET pridr=2 WHERE pridr=3;
UPDATE ClientStaffRelationships SET pridr=3 WHERE pridr=4;
UPDATE ClientStaffRelationships SET pridr=4 WHERE pridr=5;
/* Employment need to get this right xxxxxxxxx */
UPDATE THPLemphrs SET deptId=1;
UPDATE THPLHCBSHrsClient SET deptId=1;

DELETE FROM svco WHERE svid<3 OR (svid>=24 AND svid<=29)OR (svid>=52 AND svid<=64)
ALTER TABLE svco DROP COLUMN t,solved,w, mum,gthid
ALTER TABLE co DROP COLUMN
rtf,otf,ptf,shtf,hspf,hskf,rspf,acf,parf,relf,compf,
hhcf,hhnf,traf,habf,habHf,habDf,dttf,otherf,othertxtf,upwd,adpwd,useTherapy,audt,rcdt,qvadsAuto,qvadsUN,qvadsPW,ftpBill,Hemail,Hnm,Haddress,Hcity,Hstate,Hzip,Hphone1,Hphone2,Hfax,HInstructions,esSimple,hSimple,secLogin,focusUN,focusPW,focusAuto,hasPayrollEmployeeId,hasPayrollDept,hasPayrollDistrict,prIncHCBS,prShowTimeSheet,prShowReports,lockB,processed;
ALTER TABLE u DROP COLUMN
pswd,gh1,gh2,gh3,gh4,ua,bi,cl,us,dc,hb,dt,es,ghhba,ghhid,ghhab,ghrng,inv,py,pr,pyts,gh5,gh6,gh7,gh8,co,sTS,
th,pyV,prw,clw,cl2,clw2,sendAuUpdt,sendRcUpdt,sendExpAu,sendbiNotification,p0,p1,p2,p3,p4,p5,p6,p7,tc,supadmn,admn,prSup,sTSipr,tabOrder,tabOrderTRE,ghreports,hc,p8,p9,p10,p11,p12,p13,p14,P15;
ALTER TABLE THPLcl DROP COLUMN
ad1,ad2,cty,st,z,tl,cl,wk,lng,fng,FPQuality,diag1,diag2,diag3;
ALTER TABLE THPLloc DROP COLUMN
ld1,ctid,ignore,isPayroll,isdept,isbilling,deptNm,prdept;

ALTER TABLE THPLclsv DROP COLUMN
prid1,prid1r,prid2,prid2r,prid3,prid3r,prid4,prid4r,prid5,prid5r,prid6,prid6r,
prid7,prid7r,prid8,prid8r,prid9,prid9r,prid10,prid10r,prid11,prid11r,prid12,prid12r,prid13,prid13r,prid14,prid14r,prid15,prid15r,processed;

/* Dummy stuff delete XXXXXXX */
UPDATE THPLau SET dtRcvd=getDate()
declare @DirPrid int;
declare @AsstDirPrid int;
declare @SupPrid int;
declare @ProvPrid int;
declare @Diruid int;
declare @AsstDiruid int;
declare @Supuid int;
declare @Provuid int;
INSERT INTO U(deleted,coid,fn,ln,username,email,guid,pw,isSuperAdmin,isHumanResources,isDirector,isAssistantDirector,isSupervisor,isProvider,registered)VALUES
(0,32,'Debbie','McDirector','director@thpl.com','director@thpl.com','xxxx',HASHBYTES('SHA2_512',CAST('Abc123' AS NVARCHAR(30)) + CAST('xxxx' AS NVARCHAR(50))),0,0,4,0,0,0,1);SELECT @diruid=@@Identity;
INSERT INTO U(deleted,coid,fn,ln,username,email,guid,pw,isSuperAdmin,isHumanResources,isDirector,isAssistantDirector,isSupervisor,isProvider,registered)VALUES
(0,32,'Annie','McAsstDirector','asstdirector@thpl.com','asstdirector@thpl.com','xxxx',HASHBYTES('SHA2_512',CAST('Abc123' AS NVARCHAR(30)) + CAST('xxxx' AS NVARCHAR(50))),0,0,0,3,0,0,1);SELECT @asstdiruid=@@Identity;
INSERT INTO U(deleted,coid,fn,ln,username,email,guid,pw,isSuperAdmin,isHumanResources,isDirector,isAssistantDirector,isSupervisor,isProvider,registered)VALUES
(0,32,'Susan','McSupervisor','supervisor@thpl.com','supervisor@thpl.com','xxxx',HASHBYTES('SHA2_512',CAST('Abc123' AS NVARCHAR(30)) + CAST('xxxx' AS NVARCHAR(50))),0,0,0,0,2,0,1);SELECT @supuid=@@Identity;
INSERT INTO U(deleted,coid,fn,ln,username,email,guid,pw,isSuperAdmin,isHumanResources,isDirector,isAssistantDirector,isSupervisor,isProvider,registered)VALUES
(0,32,'Paul','McProvider','provider@thpl.com','provider@thpl.com','xxxx',HASHBYTES('SHA2_512',CAST('Abc123' AS NVARCHAR(30)) + CAST('xxxx' AS NVARCHAR(50))),0,0,0,0,0,1,1);SELECT @provuid=@@Identity;

INSERT INTO THPLipr(
ln,fn,ad1,ad2,cty,st,z,Sex,ph,cl,em,ssnf,dobf,ahcccsID,npi,deleted,deldt,hiredtf,termdt,CRverf,faexpf,ttlf,rlYf,apYf,EmployeeType,PTFP,isSalary,profLicReq,profLiabilityReq,prdeptId,eID,Classification,ProviderHome,ProvidesTransport,ownVehicle,refOfficeId,linkedSSN,irExemption,
supPrid,tempsupPrid,isSuperAdmin,isHumanResources,isDirector,isAssistantDirector,isSupervisor,isProvider,registered,uid)VALUES(
'McDirector','Debbie','','','','','','','','','director@thpl.com','',null,'','',0,'1/1/2100',null,null,null,null,'',0,0,'',0,1,0, 0,1,'','','N','N','N','','','N',
NULL,NULL,0,0,4,0,0,0,1,@diruid);SELECT @dirprid =@@Identity;
INSERT INTO THPLipr(
ln,fn,ad1,ad2,cty,st,z,Sex,ph,cl,em,ssnf,dobf,ahcccsID,npi,deleted,deldt,hiredtf,termdt,CRverf,faexpf,ttlf,rlYf,apYf,EmployeeType,PTFP,isSalary,profLicReq,profLiabilityReq,prdeptId,eID,Classification,ProviderHome,ProvidesTransport,ownVehicle,refOfficeId,linkedSSN,irExemption,
supPrid,tempsupPrid,isSuperAdmin,isHumanResources,isDirector,isAssistantDirector,isSupervisor,isProvider,registered,uid)VALUES(
'McAsstDirector','Annie','','','','','','','','','asstdirector@thpl.com','',null,'','',0,'1/1/2100',null,null,null,null,'',0,0,'',0,1,0, 0,1,'','','N','N','N','','','N',
@dirprid,NULL,0,0,0,3,0,0,1,@asstdiruid);SELECT @asstdirprid =@@Identity;
INSERT INTO THPLipr(
ln,fn,ad1,ad2,cty,st,z,Sex,ph,cl,em,ssnf,dobf,ahcccsID,npi,deleted,deldt,hiredtf,termdt,CRverf,faexpf,ttlf,rlYf,apYf,EmployeeType,PTFP,isSalary,profLicReq,profLiabilityReq,prdeptId,eID,Classification,ProviderHome,ProvidesTransport,ownVehicle,refOfficeId,linkedSSN,irExemption,
supPrid,tempsupPrid,isSuperAdmin,isHumanResources,isDirector,isAssistantDirector,isSupervisor,isProvider,registered,uid)VALUES(
'McSupervisor','Susan','','','','','','','','','supervisor@thpl.com','',null,'','',0,'1/1/2100',null,null,null,null,'',0,0,'',0,1,0, 0,1,'','','N','N','N','','','N',
@asstdirprid,NULL,0,0,0,0,2,0,1,@supuid);SELECT @supprid =@@Identity;
INSERT INTO THPLipr(
ln,fn,ad1,ad2,cty,st,z,Sex,ph,cl,em,ssnf,dobf,ahcccsID,npi,deleted,deldt,hiredtf,termdt,CRverf,faexpf,ttlf,rlYf,apYf,EmployeeType,PTFP,isSalary,profLicReq,profLiabilityReq,prdeptId,eID,Classification,ProviderHome,ProvidesTransport,ownVehicle,refOfficeId,linkedSSN,irExemption,
supPrid,tempsupPrid,isSuperAdmin,isHumanResources,isDirector,isAssistantDirector,isSupervisor,isProvider,registered,uid)VALUES(
'McProvider','Paul','','','','','','','','','provider@thpl.com','',null,'','',0,'1/1/2100',null,null,null,null,'',0,0,'',0,1,0, 0,1,'','','N','N','N','','','N',
@supprid,NULL,0,0,0,0,0,1,1,@provuid);SELECT @provprid =@@Identity;

UPDATE U SET prid=@dirprid WHERE uid=@diruid;
UPDATE U SET prid=@asstdirprid WHERE uid=@asstdiruid;
UPDATE U SET prid=@supprid WHERE uid=@supuid;
UPDATE U SET prid=@provprid WHERE uid=@provuid;

UPDATE THPLcl SET ln='ARNSHAW',fn='ARNOLD' WHERE clsvid=1607
UPDATE THPLcl SET ln='BRADFORD',fn='BARRY' WHERE clsvid=1475
UPDATE THPLcl SET ln='CHAPMAN',fn='CAROL' WHERE clsvid=1500
UPDATE THPLcl SET ln='DANIELS',fn='DAWN' WHERE clsvid=1501


INSERT INTO ClientStaffRelationships(prid,pridr,clsvid,clsvidid)VALUES(@provprid,0,1475,936);
INSERT INTO ClientStaffRelationships(prid,pridr,clsvid,clsvidid)VALUES(@provprid,0,1500,977);
INSERT INTO ClientStaffRelationships(prid,pridr,clsvid,clsvidid)VALUES(@provprid,0,1500,978);


INSERT INTO ClientStaffRelationships(prid,pridr,clsvid,clsvidid)VALUES(@provprid,0,1607,1158);
INSERT INTO ClientStaffRelationships(prid,pridr,clsvid,clsvidid)VALUES(@provprid,0,1607,1281);
INSERT INTO ClientStaffRelationships(prid,pridr,clsvid,clsvidid)VALUES(@provprid,0,1475,920);
INSERT INTO ClientStaffRelationships(prid,pridr,clsvid,clsvidid)VALUES(@provprid,0,1475,921);
INSERT INTO ClientStaffRelationships(prid,pridr,clsvid,clsvidid)VALUES(@provprid,0,1500,979);
INSERT INTO ClientStaffRelationships(prid,pridr,clsvid,clsvidid)VALUES(@provprid,0,1501,981);










/* End dummy stuff */


/* Rename Tables */
/*
EXEC sp_rename 'THPLcl', 'Clients'; 
EXEC sp_rename 'THPLclsv', 'ClientServices'; 
EXEC sp_rename 'THPLloc', 'ServiceLocations'; 
EXEC sp_rename 'THPLipr', 'Staff'; 
EXEC sp_rename 'THPLau', 'ClientAuths'; 
EXEC sp_rename 'THPLhrs', 'ClientHours';
EXEC sp_rename 'THPLdys', 'ClientDays';
EXEC sp_rename 'THPLHCBSHrsBill', 'HCBSHrsBill';
EXEC sp_rename 'THPLHCBSHrsEmp', 'HCBSHrsEmp';
EXEC sp_rename 'THPLHCBSHrsClient', 'HCBSHrsClient';
EXEC sp_rename 'THPLwks', 'WorkWeeks';
EXEC sp_rename 'THPLpayPeriod', 'PayPeriods';
EXEC sp_rename 'THPLEmpHrs', 'PayrollHours';
*/





