using DCC.Models.Providers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Data;
using System.Linq;
using System.Reflection;
using DCC.SQLHelpers.Helpers;
using DCC.Controllers;
using System.Configuration;

namespace DCC.Helpers
{
    public class CompanyData
    {
        private string _connString;
        private SQLHelper sqlHelper;

        public CompanyData(string connString)
        {
            _connString = connString;
        }

        public List<ClaimDTO> ListClaimsWithFullInfo(string ids)
        {

            var claims = new List<ClaimDTO>();
            var dataSet = new DataSet();
            SqlConnection sqlConnection = null;
            sqlHelper = new SQLHelper();
            try
            {
                using (sqlConnection = new SqlConnection(this._connString))
                {
                    SqlCommand sqlCommand = new SqlCommand("sp_ClaimGetClaimsFullInfoByIds", sqlConnection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    sqlCommand.Parameters.AddWithValue("@Ids", ids);
                    sqlConnection.Open();
                    sqlHelper.ExecuteSqlDataAdapter(sqlCommand, dataSet);

                    if (dataSet.HasTables())
                    {
                        if (dataSet.Tables[0].HasRows())
                        {
                            //Claim info
                            var claimInfo = dataSet.Tables[0];
                            claims = claimInfo.Rows.Cast<DataRow>().Select(x => new ClaimDTO()
                            {
                                ClaimId = int.Parse(x["ClaimId"].ToString()),// x.GetValueOrDefault<int>("ClaimId"),
                                StatusId = int.Parse(x["ClaimStatusId"].ToString()),
                                StatusUpdatedAt = x.GetValueOrDefault<DateTime>("StatusUpdatedAt"),
                                InsurancePolicyId = int.Parse(x["InsurancePolicyId"].ToString()),
                                ClaimDate = x.GetValueOrDefault<DateTime>("ClaimDate"),
                                ClientId = int.Parse(x["ClientId"].ToString()),
                                ClientGovtProgramId = x["ClientGovtProgramId"] == DBNull.Value ? "" : x["ClientGovtProgramId"].ToString().Trim(),
                                StaffId = int.Parse(x["StaffId"].ToString()),
                                StaffUserID = int.Parse(x["UserId"].ToString()),
                                ApproverStaffId = int.Parse(x["ApproverStaffId"].ToString()),
                                ApproverUserId = int.Parse(x["ApproverUserId"].ToString()),
                                LocationTypeId = int.Parse(x["LocationTypeId"] == DBNull.Value ? "1" : x["LocationTypeId"].ToString()),
                                InsuranceCompanyId = int.Parse(x["InsuranceCompanyId"].ToString()),
                                GovernmentProgramId = int.Parse(x["GovernmentProgramId"].ToString()),
                                InsuredIdNo = x.GetValueOrDefault<string>("InsuredIdNo"),
                                ProviderNPI = x["ProviderNPI"] == DBNull.Value ? "" : x["ProviderNPI"].ToString().Trim(),
                                ProviderStateMedicaid = x["ProviderStateMedicaid"] == DBNull.Value ? "" : x["ProviderStateMedicaid"].ToString().Trim(),
                                ProviderFirstName = x.GetValueOrDefault<string>("StaffFirstName"),
                                ProviderLastName = x.GetValueOrDefault<string>("StaffLastName"),
                                ProviderAddress = new AddressDTO
                                {
                                    Line1 = x.GetValueOrDefault<string>("StaffAddressLine1"),
                                    City = x.GetValueOrDefault<string>("StaffCity"),
                                    State = x.GetValueOrDefault<string>("StaffState"),
                                    PostalCode = x.GetValueOrDefault<string>("StaffPostalCode")
                                },
                                OrderingPhysicianFirstName = x.GetValueOrDefault<string>("OrderingPhysicianFirstName"),
                                OrderingPhysicianLastName = x.GetValueOrDefault<string>("OrderingPhysicianLastName"),
                                OrderingPhysicianNPI = x["OrderingPhysicianNPI"] == DBNull.Value ? "" : x["OrderingPhysicianNPI"].ToString().Trim(),
                                AppointmentId = x.GetValueOrDefault<long>("AppointmentId"),
                                DeductibleInd = int.Parse(x["DeductibleInd"].ToString()),
                                //DeductibleAmt = x.GetValueOrDefault<decimal?>("DeductibleAmt"),
                                //DeductibleReasonCode = x.GetValueOrDefault<int?>("DeductibleReasonCode")
                            }).ToList();

                            //Claim Payment
                            if (dataSet.Tables[1].HasRows())
                            {
                                var paymentResultSet = dataSet.Tables[1];
                                var payments = paymentResultSet.Rows.Cast<DataRow>().Select(x => new ClaimPaymentDTO()
                                {
                                    Amount = x.GetValueOrDefault<decimal?>("Amount"),
                                    ClaimId = x.GetValueOrDefault<long>("ClaimId"),
                                    GovernmentProgramId = int.Parse(x["GovernmentProgramId"] == DBNull.Value ? "0" : x["GovernmentProgramId"].ToString()),
                                    InsurancePolicyId = int.Parse(x["InsurancePolicyId"] == DBNull.Value ? "0" : x["InsurancePolicyId"].ToString()),
                                    InsurancePolicyNumber = x.GetValueOrDefault<string>("PolicyNumber"),
                                    InsuredIdNo = x.GetValueOrDefault<string>("InsuredIdNo"),
                                    InsuranceCompanyId = int.Parse(x["InsuranceCompanyId"] == DBNull.Value ? "0" : x["InsuranceCompanyId"].ToString()),
                                    SubscriberFirstName = x.GetValueOrDefault<string>("SubscriberFirstName"),
                                    SubscriberLastName = x.GetValueOrDefault<string>("SubscriberLastName"),
                                    SubscriberAddress = new AddressDTO
                                    {
                                        Line1 = x.GetValueOrDefault<string>("SubscriberAddressLine1"),
                                        City = x.GetValueOrDefault<string>("SubscriberCity"),
                                        State = x.GetValueOrDefault<string>("SubscriberState"),
                                        PostalCode = x.GetValueOrDefault<string>("SubscriberPostalCode")
                                    },
                                    InsurancePolicyPriorityId = int.Parse(x["InsurancePriorityId"] == DBNull.Value ? "1" : x["InsurancePriorityId"].ToString()),
                                    InsurancePolicyRelationshipId = int.Parse(x["InsuranceRelationshipId"] == DBNull.Value ? "0" : x["InsuranceRelationshipId"].ToString()),
                                    IsDenial = x.GetValueOrDefault<bool>("IsDenial"),
                                    Payer = x.GetValueOrDefault<string>("Payer"),
                                    Notes = x.GetValueOrDefault<string>("Notes"),
                                    PayDate = x.GetValueOrDefault<DateTime>("ReceivedAt"),
                                    PaymentTypeId = int.Parse(x["PaymentTypeId"] == DBNull.Value ? "0" : x["PaymentTypeId"].ToString()),
                                    VoidedAt = x.GetValueOrDefault<DateTime?>("VoidedAt"),
                                    DenialReasonId = x.GetValueOrDefault<string>("DenialReasonId"),
                                    ReasonText = x.GetValueOrDefault<string>("ReasonText"),
                                    AllowedAmount = x.GetValueOrDefault<decimal>("AllowedAmount"),
                                    AppointmentId = x.GetValueOrDefault<long>("AppointmentId"),
                                    CoInsuranceAmount = x.GetValueOrDefault<decimal>("CoInsuranceAmount"),
                                    MCID = x.GetValueOrDefault<string>("MCID")
                                }).ToList();

                                // Add corresponding Payments to each claim
                                if (payments.Any() && claims.Any())
                                {
                                    claims.ForEach(x =>
                                    {
                                        x.Payments.AddRange(payments.Where(a => a.AppointmentId == x.AppointmentId));
                                    });
                                }
                            }

                            // Claim Insurance Policy
                            if (dataSet.Tables[2].HasRows())
                            {
                                var insPolicyResultSet = dataSet.Tables[2];
                                var insPolicies = insPolicyResultSet.Rows.Cast<DataRow>().Select(x => new InsurancePolicyDTO()
                                {
                                    AuditActionId = x.GetValueOrDefault<long?>("AuditActionId"),
                                    ClientId = int.Parse(x["ClientId"] == DBNull.Value ? "0" : x["ClientId"].ToString()),
                                    EndDate = x.GetValueOrDefault<DateTime>("EndDate"),
                                    FirstName = x.GetValueOrDefault<string>("FirstName"),
                                    InsuranceCompanyId = int.Parse(x["InsuranceCompanyId"] == DBNull.Value ? "0" : x["InsuranceCompanyId"].ToString()),
                                    InsurancePolicyId = int.Parse(x["InsurancePolicyId"] == DBNull.Value ? "0" : x["InsurancePolicyId"].ToString()),
                                    InsuranceRelationshipId = int.Parse(x["InsuranceRelationshipId"] == DBNull.Value ? "0" : x["InsuranceRelationshipId"].ToString()),
                                    InsuredDoB = x.GetValueOrDefault<DateTime>("InsuredDoB"),
                                    InsuredIdNo = x.GetValueOrDefault<string>("InsuredIdNo"),
                                    InsuranceTierId = x.GetValueOrDefault<byte>("InsurancePriorityId"),
                                    MCID = x.GetValueOrDefault<string>("MCID"),
                                    LastName = x.GetValueOrDefault<string>("LastName"),
                                    StartDate = x.GetValueOrDefault<DateTime>("StartDate"),
                                    PolicyNumber = x.GetValueOrDefault<string>("PolicyNumber"),
                                    ClientFirst = x.GetValueOrDefault<string>("ClientFirst"),
                                    ClientMiddle = x.GetValueOrDefault<string>("ClientMiddle"),
                                    ClientLast = x.GetValueOrDefault<string>("ClientLast"),
                                    ClientSuffix = x.GetValueOrDefault<string>("ClientSuffix"),
                                    Phone = x.GetValueOrDefault<string>("Phone"),
                                    ClaimId = x.GetValueOrDefault<long>("ClaimId"),
                                    GenderId = int.Parse(x["GenderId"] == DBNull.Value ? "0" : x["GenderId"].ToString()),
                                    PatientIdNo = x.GetValueOrDefault<string>("PatientIdNo"),
                                    Address = new AddressDTO
                                    {
                                        Line1 = x.GetValueOrDefault<string>("AddressLine"),
                                        Line2 = x.GetValueOrDefault<string>("AddressLine2"),
                                        City = x.GetValueOrDefault<string>("City"),
                                        State = x.GetValueOrDefault<string>("State"),
                                        PostalCode = x.GetValueOrDefault<string>("PostalCode"),
                                    },
                                }).ToList();


                                if (insPolicies.Any() && claims.Any())
                                {
                                    var mcid = insPolicies.FirstOrDefault(x => !string.IsNullOrEmpty(x.MCID))?.MCID;
                                    claims.ForEach(x =>
                                    {
                                        x.InsurancePolicy = insPolicies.FirstOrDefault(a => a.ClaimId == x.ClaimId);

                                        if (x.InsurancePolicy != null && string.IsNullOrEmpty(x.InsurancePolicy.MCID) && !string.IsNullOrEmpty(mcid))
                                        {
                                            x.InsurancePolicy.MCID = mcid;
                                        }
                                    });
                                }
                            }

                            // Claim Appointments
                            if (dataSet.Tables[3].HasRows())
                            {
                                var appintmentResultSet = dataSet.Tables[3];
                                var appointments = appintmentResultSet.Rows.Cast<DataRow>().Select(x => new AppointmentDTO()
                                {
                                    ID = x.GetValueOrDefault<long>("AppointmentId"),
                                    ClaimId = x.GetValueOrDefault<long>("ClaimId"),
                                    StatusId = int.Parse(x["AppointmentStatusId"] == DBNull.Value ? "0" : x["AppointmentStatusId"].ToString()),
                                    ClientServiceID = int.Parse(x["ClientServiceId"] == DBNull.Value ? "0" : x["ClientServiceId"].ToString()),
                                    StartAt = x.GetValueOrDefault<DateTime>("StartAt"),
                                    ServiceId = int.Parse(x["ServiceId"] == DBNull.Value ? "0" : x["ServiceId"].ToString()),
                                    ServiceName = x.GetValueOrDefault<string>("ServiceName"),
                                    Units = Convert.ToDecimal(x.GetValueOrDefault<float?>("Units")),
                                    GovtUnits = Convert.ToDecimal(x.GetValueOrDefault<float?>("GovtUnits")),
                                    Amount = x.GetValueOrDefault<decimal?>("Amount"),
                                    DisciplineCode = x.GetValueOrDefault<string>("DisciplineCode"),
                                    IsTeletherapy = x.GetValueOrDefault<bool>("IsTeletherapy"),
                                    CPTServiceRates = new List<ClientServiceCPTDTO>()
                                }).ToList();

                                // Add corresponding Appointments to each claim
                                if (appointments.Any() && claims.Any())
                                {
                                    claims.ForEach(x =>
                                    {
                                        x.Appointments.AddRange(appointments.Where(a => a.ClaimId == x.ClaimId));
                                    });
                                }
                            }

                            // Claim Comments
                            if (dataSet.Tables[4].HasRows())
                            {
                                var commentsResultSet = dataSet.Tables[4];
                                var comments = commentsResultSet.Rows.Cast<DataRow>().Select(x => new ClaimCommentDTO()
                                {
                                    ID = x.GetValueOrDefault<long>("AppointmentId"),
                                    ClaimId = x.GetValueOrDefault<long>("ClaimId"),
                                    Comments = x.GetValueOrDefault<string>("Comments"),
                                    MadeAt = x.GetValueOrDefault<DateTime>("MadeAt"),
                                    StaffId = int.Parse(x["StaffId"] == DBNull.Value ? "0" : x["StaffId"].ToString()),
                                    StaffUserId = int.Parse(x["UserId"] == DBNull.Value ? "0" : x["UserId"].ToString()),
                                }).ToList();

                                // Add corresponding Appointments to each claim
                                if (comments.Any() && claims.Any())
                                {
                                    claims.ForEach(x =>
                                    {
                                        x.ClaimComments.AddRange(comments.Where(a => a.ClaimId == x.ClaimId));
                                    });
                                }
                            }

                            //CPT Rates
                            if (dataSet.Tables[5].HasRows())
                            {
                                var cptResultSet = dataSet.Tables[5];
                                var cpts = cptResultSet.Rows.Cast<DataRow>().Select(x => new ClientServiceCPTDTO()
                                {
                                    AppointmentId = x.GetValueOrDefault<long>("AppointmentId"),
                                    ClaimId = x.GetValueOrDefault<long>("ClaimId"),
                                    Amount = x.GetValueOrDefault<decimal?>("Amount"),
                                    ClientServiceId = int.Parse(x["ClientServiceId"] == DBNull.Value ? "0" : x["ClientServiceId"].ToString()),
                                    CPTCode = x.GetValueOrDefault<string>("CPTCode"),
                                    Modifier3 = x.GetValueOrDefault<string>("Mod3"),
                                    Modifier1 = x.GetValueOrDefault<string>("Mod1"),
                                    Modifier2 = x.GetValueOrDefault<string>("Mod2"),
                                    Units = x.GetValueOrDefault<string>("Units"),
                                }).ToList();



                                // Add corresponding Appointments to each claim
                                if (cpts.Any() && claims.Any())
                                {

                                    cpts.ForEach(x =>
                                    {
                                        var claim = claims.FirstOrDefault(c => c.ClaimId == x.ClaimId);
                                        if (claim != null)
                                        {
                                            var appointment = claim.Appointments.Find(a => a.ID == x.AppointmentId);
                                            if (appointment != null)
                                            {
                                                appointment.CPTServiceRates.Add(x);
                                            }
                                        }
                                    });
                                }
                            }

                            //Diagnosis Codes
                            if (dataSet.Tables[6].HasRows())
                            {
                                var diagCodesResultSet = dataSet.Tables[6];

                                var diagCodes = diagCodesResultSet.Rows.Cast<DataRow>().Select(x => new ClientDiagnosisCodeDTO()
                                {
                                    Code = x.GetValueOrDefault<string>("Code"),
                                    ID = int.Parse(x["ID"] == DBNull.Value ? "0" : x["ID"].ToString()),
                                    InsurancePolicyId = int.Parse(x["InsurancePolicyId"] == DBNull.Value ? "0" : x["InsurancePolicyId"].ToString()),
                                    ClientServiceId = int.Parse(x["ClientServiceId"] == DBNull.Value ? "0" : x["ClientServiceId"].ToString())
                                }).ToList();
                                if (diagCodes.Any())
                                {
                                    claims.ForEach(x =>
                                    {
                                        var diagCodesFiltered = diagCodes.Where(y => y.ClientServiceId.HasValue && y.ClientServiceId.Value == x.Appointments?.First()?.ClientServiceID).Distinct().ToList();
                                        if(diagCodesFiltered != null)
                                        {
                                            x.DiagnosisCodes.AddRange(diagCodesFiltered);
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
                //var error = new ErrorLogDTO();
                //error.CompanyId = this._account.CompanyID;
                //error.MethodInScope = MethodBase.GetCurrentMethod().Name;
                //error.ClassInScope = this.GetType().Name;
                //error.ExceptionFull = ex;
                //ErrorLogHelper.StoreException(error);
            }
            finally
            {
                sqlConnection.Close();
            }
            return claims;
        }
    }
}
