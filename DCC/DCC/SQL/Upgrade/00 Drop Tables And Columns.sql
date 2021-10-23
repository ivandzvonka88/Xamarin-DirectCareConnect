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

DROP TABLE IF EXISTS dddofficestbl;
DROP TABLE IF EXISTS em;
DROP TABLE IF EXISTS g;
DROP TABLE IF EXISTS cllocations;
DROP TABLE IF EXISTS marchrshB;

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
