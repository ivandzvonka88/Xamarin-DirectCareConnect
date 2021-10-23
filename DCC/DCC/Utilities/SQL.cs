using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC
{
    public static class SQL
    {
        public  static string getStaffPermissions(string userLevel, int prId)
        {
            return
                "SELECT S.fn,S.ln,SR.* FROM Staff AS S JOIN StaffRoles AS SR ON SR.roleNominal='" + userLevel + "' WHERE prId=" + prId + ";";
        }
        public static string getStaffProfile(int prId)
        {
            return
            // Table 0 general staff member stuff
            "SELECT S.prID,S.fn,S.ln,S.ln+' '+S.fn As nm,S.ad1,S.ad2,S.cty,S.st,S.z,S.ph,S.cl,S.em,S.eid,S.deleted,ISNULL(S.mi,'')AS mi,ISNULL(S.classification,'')AS classification," +
            "ISNULL(S.employeetype,'')AS employeetype,S.providestransport,ISNULL(S.refOfficeId,'')AS refOfficeId,ISNULL(S.linkedSSN,'')AS linkedSSN,ISNULL(S.ownVehicle,'')AS ownVehicle,ISNULL(S.irExemption,'')AS irExemption,S.profLicReq,S.profLiabilityReq,S.ahcccsId,S.npi,S.hiredtf,S.dobf,S.ttlf,S.ssnf,S.CRverf,S.termdt,ISNULL(S.Sex,'')AS Sex,ISNULL(S.npi,'')AS npi,S.providerHome,ISNULL(S.refOfficeId,'')AS refOfficeId,S.isSalary,S.PTFP,ISNULL(S.registered,0) AS registered,ISNULL(S.prt,0)AS prt,ISNULL(P.prDeptId,0) AS prDeptId,ISNULL(P.prDeptCode,'')AS prdeptCode," +
            "ISNULL(I2.prid,0) AS supId,I2.ln+' '+I2.fn AS supName,R2.roleId AS supRoleId,R2.roleName AS supRoleName," +
            "ISNULL(I3.prid,0) AS tempsupId,I3.ln+' '+I3.fn AS tempsupName,R3.roleId AS tempsupRoleId,R3.roleName AS tempsupRoleName" +
            " FROM Staff AS S" +
            " LEFT JOIN PayrollDepts AS P ON P.prDeptId=S.prDeptId" +
            " LEFT JOIN Staff AS I2 ON I2.prid=S.supPrId" +
            " LEFT JOIN StaffRoles AS R2 ON R2.roleId=I2.isDirector OR R2.roleId=I2.isAssistantDirector OR  R2.roleId=I2.isSupervisor" +
            " LEFT JOIN Staff AS I3 ON I3.prid=S.tempsupPrid" +
            " LEFT JOIN StaffRoles AS R3 ON R3.roleId=I3.isDirector OR R2.roleId=I3.isAssistantDirector OR  R2.roleId=I3.isSupervisor" +
            " WHERE S.prid=" + prId + ";";
        }


        public static string getHierarchicalExpiringAuths(string userLevel, int prId)
        {
            return
            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT X.*," +
            "CASE" +
            " WHEN SR.AuthRedAlertOn<>0 AND DATEADD(DAY,SR.AuthRedAlertDays,eddt)<GETDate() THEN 1" +
            " WHEN SR.AuthAmberAlertOn<>0 AND DATEADD(DAY,SR.AuthAmberAlertDays,eddt)<GETDate() THEN 2" +
            " ELSE 0" +
            " END AS Priority" +
            " FROM(" +
            "SELECT C.clsvId,C.fn AS cfn,C.ln AS cln,CS.svc,A.eddt," +
            "ROW_NUMBER() OVER(PARTITION BY CS.id ORDER BY eddt DESC) AS Row" +
            " FROM StaffList" +
            " JOIN ClientStaffRelationships AS RS ON RS.prId=StaffList.prId" +
            " JOIN Clients AS C ON C.clsvid=RS.clsvId AND C.deleted=0" +
            " JOIN ClientServices AS CS ON CS.id=RS.clsvidId AND CS.deleted=0" +
            " LEFT JOIN ClientAuths AS A ON A.clid=C.clId AND A.secode=CS.svc)AS X" +
            " JOIN StaffRoles AS SR ON SR.rolenominal='" + userLevel + "'" +
            " WHERE Row=1 AND" +
            "((SR.AuthAmberAlertOn<>0 AND DATEADD(DAY,SR.AuthAmberAlertDays,eddt)<GETDate())OR" +
            "(SR.AuthRedAlertOn<>0 AND DATEADD(DAY,SR.AuthRedAlertDays,eddt)<GETDate()))" +
            "ORDER BY eddt ASC,cln ASC, cfn ASC, svc ASC;";
        }

        public static string getHierarchicalLateOrUapprovedNotes(string userLevel, int prId)
        {
            return
            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT C.fn AS cfn,C.ln AS cln,S.fn AS sfn,S.ln AS sln,A.fn AS afn,A.ln AS aln,CN.*" +
            " FROM staffList" +
            " JOIN ClientNotes AS CN ON CN.noteWritten=0 AND CN.prId=StaffList.prId" +
            " JOIN Clients AS C ON C.clsvid=CN.clsvId" +
            " JOIN Staff AS S ON S.prId=CN.prId" +
            " LEFT JOIN Staff AS A ON A.prid=approverId" +
            " JOIN StaffRoles AS SR ON SR.roleNominal='" + userLevel + "'" +
            " WHERE CN.noteWritten=0 AND" +
            "((SR.LateNoteRedAlertOn<>0 AND DATEADD(DAY, SR.LateNoteRedAlertHours, CN.dueDt)<GETDate())OR CN.approved<>1)" +
            "ORDER BY dueDt ASC;";
        }

        public static string getHierarchicalOverBilling(string userLevel, int prId)
        {
            // dummy
            return
                "SELECT * FROM Clients WHERE clsvID=0;";
        }

        public static string getHierarchicalClientList(int prId)
        {
            return
                "WITH StaffList AS" +
                "(SELECT S1.prid,1 AS empLevel" +
                " FROM Staff AS S1" +
                " WHERE prid=" + prId +
                " UNION ALL" +
                " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
                " FROM Staff AS S2" +
                " INNER JOIN StaffList AS SL" +
                " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
                " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
                "SELECT DISTINCT C.clsvId,C.ln + ' ' + C.fn AS cNm" +
                " FROM StaffList" +
                " JOIN ClientStaffRelationships AS CSR ON CSR.prId=StaffList.prId" +
                " JOIN Clients AS C ON C.clsvid=CSR.clsvId AND C.deleted=0" +
                " JOIN Staff AS S ON S.prId=CSR.prId" +
                " WHERE CSR.prId=StaffList.prId" +
                " ORDER BY cNm ASC;";
        }

        public static string getProviderBillingData(int prId)
        {
            // dummy
            return
                "SELECT * FROM Clients WHERE clsvID=0;";
        }

        public static string getHierarchicalExpiringCredentials(string userLevel, int prId)
        {
            return
            "WITH StaffList AS" +
            "(SELECT S1.prid,S1.fn,S1.ln,S1.isSuperAdmin,S1.isHumanResources,S1.isDirector,S1.isAssistantDirector,S1.isSupervisor,S1.isProvider,S1.profLicReq,S1.profLiabilityReq,S1.providesTransport,S1.ownVehicle,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,S2.fn,S2.ln,S2.isSuperAdmin,S2.isHumanResources,S2.isDirector,S2.isAssistantDirector,S2.isSupervisor,S2.isProvider,S2.profLicReq,S2.profLiabilityReq,S2.providesTransport,S2.ownVehicle,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT X.*," +
            "CASE" +
            " WHEN validTo IS NULL THEN 'Missing'" +
            " WHEN verified=0 THEN 'Not Verified'" +
            " WHEN (SR.CredRedAlertOn<>0 AND DATEADD(DAY,SR.credRedAlertDays,validTo)<GETDate())OR(SR.CredAmberAlertOn<>0 AND DATEADD(DAY,SR.credAmberAlertDays,validTo)<GETDate()) THEN 'Expiring '+CONVERT(VARCHAR(10), validTo, 101)" +
            " END AS AlertType," +
            "CASE" +
            " WHEN validTo IS NULL THEN 1" +
            " WHEN verified=0 THEN 1" +
            " WHEN SR.CredRedAlertOn<>0 AND DATEADD(DAY,SR.credRedAlertDays,validTo)<GETDate() THEN 1" +
            " WHEN SR.CredAmberAlertOn<>0 AND DATEADD(DAY,SR.credAmberAlertDays,validTo)<GETDate() THEN 2" +
            " END AS priority" +
            " FROM(" +
            "SELECT DISTINCT I.prId,I.fn,I.ln,ICI.credName,ICI.credTypeId,IC.credId,IC.verified,IC.validFrom,IC.validTo,IC.docId,IC.verificationDate,S.fn+' '+S.ln AS verifier," +
            "ROW_NUMBER() OVER(PARTITION BY I.prId,ICI.credTypeId ORDER BY IC.verified ASC,IC.validTo DESC)AS R" +
            " FROM StaffList AS I" +
            " JOIN CredentialIds AS ICI ON(I.isSuperAdmin<>0 AND ICI.superadmin<>0)OR(I.isHumanResources<>0 AND ICI.humanResources<>0)OR(I.isDirector<>0 AND ICI.director<>0)OR(I.isAssistantDirector<>0 AND ICI.assistantdirector<>0)OR(I.isSupervisor<>0 AND ICI.supervisor<>0)OR(I.isProvider<>0 AND ICI.provider<>0)OR(I.profLicReq<>0 AND ICI.credTypeId=7)OR(I.profLiabilityReq<>0 AND ICI.credTypeId=8)OR(I.providesTransport='Y' AND ICI.credTypeId=9)OR(I.ownVehicle='Y' AND(ICI.credTypeId=10 OR ICI.credTypeId=11))" +
            " LEFT JOIN StaffCredentials AS IC ON IC.prId=I.prId AND IC.credTypeId=ICI.credTypeId" +
            " LEFT JOIN Staff AS S ON S.prid=IC.verifier)AS X" +
            " JOIN StaffRoles AS SR ON SR.rolenominal='" + userLevel + "'" +
            " WHERE R=1 AND" +
            "((SR.CredAmberAlertOn<>0 AND (validTo IS NULL OR verified=0 OR DATEADD(DAY,SR.CredAmberAlertDays,validTo)<GETDate()))OR" +
            "(SR.CredRedAlertOn<>0 AND (validTo IS NULL OR verified=0 OR DATEADD(DAY,SR.CredRedAlertDays,validTo)<GETDate())))" +
            "ORDER BY ln ASC,fn ASC,verified asc,R asc,credname ASC;";
        }

        public static string getHierarchicalStaffComments(int prId)
        {
            return
                "WITH StaffList AS" +
                "(SELECT S1.prid,1 AS empLevel" +
                " FROM Staff AS S1" +
                " WHERE prid=" + prId +
                " UNION ALL" +
                " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
                " FROM Staff AS S2" +
                " INNER JOIN StaffList AS SL" +
                " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
                " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
                " SELECT SC.*,S.fn + ' ' + S.ln AS subject,S2.fn + ' ' + S2.ln AS commentator FROM staffList AS I" +
                " JOIN StaffComments AS SC ON SC.prId=I.prId" +
                " JOIN Staff AS S ON S.prid=SC.prId" +
                " JOIN Staff AS S2 ON S2.prId = SC.commentatorId" +
                " WHERE I.prId<>" + prId + " ORDER BY cmtDt DESC;";
        }
        public static string getHierarchicalStaffList(int prId)
        {
            return
                "WITH StaffList AS" +
                "(SELECT S1.prid,1 AS empLevel" +
                " FROM Staff AS S1" +
                " WHERE prid=" + prId +
                " UNION ALL" +
                " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
                " FROM Staff AS S2" +
                " INNER JOIN StaffList AS SL" +
                " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
                " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
                "SELECT S.prId,S.ln+' '+S.fn AS sNm,S.isDirector,S.isAssistantDirector,S.isSupervisor,S.isProvider" +
                " FROM staffList AS I" +
                " JOIN Staff AS S ON S.prId=I.prId AND S.prId<>" + prId + " ORDER BY sNm ASC;";

        }

        public static string getHierarchicalNewAuths(int prId)
        {
            return
            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT DISTINCT CSR.clsvId,CSR.clsvidId,C.ln+' '+C.fn AS nm,CS.svc,CA.stdt,CA.eddt,CA.au" +
            " FROM staffList AS I" +
            " JOIN ClientStaffRelationships AS CSR ON CSR.prId=I.prId" +
            " JOIN Clients AS C ON C.clsvId=CSR.clsvId AND C.deleted=0" +
            " JOIN ClientServices AS CS ON CS.id=CSR.clsvidId AND CS.deleted=0" +
            " JOIN ClientAuths AS CA ON CA.clid=C.clId AND CA.seCode=CS.svc AND CA.dtRcvd>DATEADD(DAY,-4,GETDATE())" +
            " ORDER BY nm ASC,svc ASC;";
        }

        public static string getHierarchicalClientProfile(int prId, int clsvId)
        {
            return
            /* make sure staff member has access to this client */
            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT DISTINCT C.*,BR.* FROM staffList AS I" +
            " JOIN ClientStaffRelationships AS CSR ON CSR.prId=I.prId" +
            " JOIN Clients AS C ON C.clsvId=CSR.clsvId" +
            " JOIN [DCCMain].[dbo].[billingRegions] AS BR ON BR.billRegId=C.billRegId" +
        //    " LEFT JOIN Guardians AS G ON G.gId=C.gId1 OR G.gId=C.gId2 OR G.gId=C.gId3" +
            " WHERE CSR.prid=I.prId AND CSR.clsvId=" + clsvId + ";";
        }

        public static string getHierarchicalClientServices(int prId, int clsvId)
        {
            return
            /* make sure staff member has access to this client */
            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT DISTINCT CS.*,D.deptId,D.deptCode,SV.isHourly,SV.hasMaxHours,SV.allowSpecialRates,SV.selectrate,SV.hasWeeklySchedule,SVRA.svce,SVRADT.rate" + 
            " FROM staffList AS I" +
            " JOIN ClientStaffRelationships AS CSR ON CSR.prId=I.prId AND CSR.clsvId=" + clsvId +
            " JOIN Clients AS C ON C.clsvId=CSR.clsvId" +
            " JOIN ClientServices AS CS ON CS.id=CSR.clsvidId" +
            " JOIN ServiceLocations AS CL ON CL.locId=CS.locId" +
            " JOIN Departments AS D ON D.deptId=CL.deptId" +
            " JOIN [DCCMain].[dbo].[svco] AS SV ON SV.svc=CS.svc" +
            " LEFT JOIN [DCCMain].[dbo].[svra] AS SVRA ON SVRA.svId=SV.svId AND SVRA.rid=CS.rid" +
            " LEFT JOIN [DCCMain].[dbo].[svradt] AS SVRADT ON SVRADT.rId=SVRA.rId AND GETDATE()>=SVRADT.efdt AND GETDATE()<=SVRADT.fndt AND SVRADT.billRegId=C.billRegId" +

            " ORDER BY svc ASC;";
        }

        public static string getHierarchicalClientAuths(int prId, int clsvId)
        {
            return
            /* make sure staff member has access to this client */
             "WITH StaffList AS" +
             "(SELECT S1.prid,1 AS empLevel" +
             " FROM Staff AS S1" +
             " WHERE prid=" + prId +
             " UNION ALL" +
             " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
             " FROM Staff AS S2" +
             " INNER JOIN StaffList AS SL" +
             " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
             " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT DISTINCT CS.id AS clsvidId,CA.auid,CA.stdt,CA.eddt,CA.au,CA.uu,CA.ru,ISNULL(X.o,0)AS o,CA.au/DATEDIFF(week,stdt,eddt)AS wk FROM StaffList AS I" +
            " JOIN clientStaffRelationships AS CSR ON CSR.prId=I.prId AND CSR.clsvId=" + clsvId +
            " JOIN Clients AS C ON C.clsvId=CSR.clsvId" +
            " JOIN ClientServices AS CS ON CS.clsvid=C.clsvid" +
            " JOIN ClientAuths AS CA ON CA.clid=C.clid AND CA.secode=CS.svc AND CA.eddt>DATEADD(MONTH, -6,GETDATE())" +
            " LEFT JOIN" +
            "(SELECT au.auid,SUM(un)AS o FROM(" +
            " SELECT svc,dt,(un+ajun)AS un FROM ClientHours WHERE clsvid=" + clsvId + " AND(un+ajun>0)AND(pd<>1 AND pd<>3)" +
            " UNION ALL" +
            " SELECT svc,dt,(un+ajun)AS un FROM ClientDays WHERE clsvid=" + clsvId + " AND(un+ajun>0)AND(pd<>1 AND pd<>3)" +
            " UNION ALL" +
            " SELECT svc,dt,(un+ajun)AS un FROM HCBSHrsBill WHERE clsvid=" + clsvId + " AND(un+ajun>0)AND(pd<>1 AND pd<>3)AND billASGroup IS NULL" +
            " UNION ALL" +
            " SELECT DISTINCT svc,dt,CAST(12 AS DECIMAL)AS un FROM HCBSHrsBill WHERE clsvid=" + clsvId + " AND(pd<>1 AND pd<>3)AND billASGroup IS NOT NULL" +
            ")AS A" +
            " JOIN Clients AS cl ON cl.clsvid=1607" +
            " LEFT JOIN ClientAuths AS au ON au.clid=cl.clid AND au.secode=A.svc AND A.dt>=au.stdt AND A.dt<=au.eddt" +
            " GROUP BY auid)AS X ON x.auid=CA.auid;";
        }

        public static string getHierarchicalClientSpecialRates(int prId, int clsvId)
        {
            return
            /* make sure staff member has access to this client */
            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT DISTINCT SPR.* FROM staffList AS I" +
            " JOIN ClientStaffRelationships AS CSR ON CSR.prId=I.prId AND CSR.clsvId=" + clsvId +
            " JOIN ClientSpecialRates AS SPR ON SPR.clsvidId=SPR.clsvId;";
        }

        public static string getHierarchicalClientGeoLocations(int prId, int clsvId)
        {
            return
            /* make sure staff member has access to this client */
            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT DISTINCT CL.* FROM staffList AS I" +
            " JOIN ClientStaffRelationships AS CSR ON CSR.prId=I.prId AND CSR.clsvId=" + clsvId +
            " JOIN ClientLocations AS CL ON CL.clsvId=CSR.clsvId;";
        }

        public static string getHierarchicalClientCharts(int prId, int clsvId)
        {
            return
            /* make sure staff member has access to this client */
            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT DISTINCT CC.* FROM staffList AS I" +
            " JOIN ClientStaffRelationships AS CSR ON CSR.prId=I.prId AND CSR.clsvId=" + clsvId +
            " JOIN ClientCharts AS CC ON CC.clsvId=CSR.clsvId ORDER BY chartId DESC;";
        }

        public static string getHierarchicalClientNotes(int prId, int clsvId)
        {
            return
            /* make sure staff member has access to this client */
            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT DISTINCT CN.* FROM staffList AS I" +
            " JOIN ClientStaffRelationships AS CSR ON CSR.prId=I.prId AND CSR.clsvId=" + clsvId +
            " JOIN ClientNotes AS CN ON CN.clsvId=CSR.clsvId ORDER BY clNoteId DESC;";
        }
        public static string getHierarchicalClientBillingData(int prId, int clsvId)
        {
            return
/* make sure staff member has access to this client */

            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT x.rat,x.svc,SUM(x.un)AS un" +
            " FROM StaffList" +
            " JOIN ClientStaffRelationships AS CSR ON CSR.prid=StaffList.prid AND clsvid=" + clsvId +
            " JOIN ClientServices AS CS ON CS.id=CSR.clsvidId" +
            " JOIN PayPeriods AS PP ON PP.s<='"+ DateTime.Now +"' AND pp.e>='" + DateTime.Now + "'" +
            " LEFT JOIN(" +
            "SELECT dt,svc,3 AS rat,(un+ajun)AS un FROM ClientHours WHERE clsvid=" + clsvId + " AND(un+ajun>0)" +
            " UNION ALL" +
            " SELECT dt,svc,rat,(un+ajun)AS un FROM HCBSHrsBill WHERE clsvid=" + clsvId + " AND(un+ajun>0)AND billASGroup IS NULL" +
            " UNION ALL" +
            " SELECT DISTINCT dt,svc,rat,CAST(12 AS DECIMAL)AS un FROM HCBSHrsBill WHERE clsvid=" + clsvId + " AND billASGroup IS NOT NULL) AS X ON X.dt>=PP.s AND X.dt<=PP.e AND X.svc=CS.svc" +
            " GROUP BY x.rat,x.svc;";


        }


        public static string getHierarchicalClientComments(int prId, int clsvId)
        {
            return
            /* make sure staff member has access to this client */
            "WITH StaffList AS" +
            "(SELECT S1.prid,1 AS empLevel" +
            " FROM Staff AS S1" +
            " WHERE prid=" + prId +
            " UNION ALL" +
            " SELECT S2.prid,SL.empLevel+1 AS empLevel" +
            " FROM Staff AS S2" +
            " INNER JOIN StaffList AS SL" +
            " ON S2.supPrid=SL.prId OR S2.tempsupPrid=SL.prId" +
            " WHERE S2.prid<>0 AND S2.prID IS NOT NULL)" +
            "SELECT DISTINCT CC.* FROM staffList AS I" +
            " JOIN ClientStaffRelationships AS CSR ON CSR.prId=I.prId AND CSR.clsvId=" + clsvId +
            " JOIN ClientComments AS CC ON CC.clsvId=CSR.clsvId ORDER BY commentId DESC;";
        }






    }
}