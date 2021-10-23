using DCC.Models.Providers;
using DCC.SQLHelpers.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;

namespace DCC.Models
{
    internal class ClaimReconciler : IDisposable
    {
        private readonly SQLHelper sqlHelper;
        UserClaims _UserClaim = null;
        
        public ClaimReconciler(UserClaims UserClaim)
        {
            _UserClaim = UserClaim;
            sqlHelper = new SQLHelper();
        }

        public PaymentInfo ProcessAZMedicaid(byte[] filedata, ref ReconcileResponse rsp, Dictionary<string, ClaimInfo> claims)
        {
            List<int> statuses = new List<int>();
            List<int> fields = new List<int>();
            fields.Add(1);
            fields.Add(5);
            fields.Add(9);
            fields.Add(13);
            PaymentInfo result = new PaymentInfo()
            {
                Type = PaymentTypeEnum.Govt,
                MadeOn = DateTime.Today,
                Description = "ACCHS",
                Claims = new List<ClaimPaymentRC>(),
                Amount = 0
            };
            ClaimInfo claim = null;

            int lineNbr = 0;
            bool hasFull = false;
            using (var stream = new MemoryStream(filedata))
            {
                using (var sr = new StreamReader(stream))
                {
                    while (!sr.EndOfStream)
                    {
                        lineNbr++;
                        hasFull = false;
                        var line = sr.ReadLine();
                        var columns = CsvParser(line);
                        claim = null;
                        //Not a data line
                        if (columns.Count == 0 || (columns[0] != "PAID" && columns[0] != "DENIED" && columns[0] != "PENDED")) continue;
                        rsp.RecordCount++;

                        //Find claim
                        string claimIdStr = columns[17];
                        long claimId = 0;
                        if (!string.IsNullOrWhiteSpace(claimIdStr) && long.TryParse(claimIdStr, out claimId))
                        {
                            claim = GetClaim(claimId);
                        }


                        if (claim == null)
                        {
                            rsp.EntityList.Add(new GenericEntity()
                            {
                                UniqueId = lineNbr.ToString(),
                                Context = "Reconcile Issue",
                                Name = "Could not find claim from file!",
                                AlternateId = claimId.ToString()
                            });
                            continue;
                        }


                        string claimName = string.Format("{1} for {2} with {0}", claim.Provider.Name, claim.ClaimDate.ToShortDateString(), claim.Client.Name);

                        //Check to make sure claim is in state pending government payment
                        if (claim.Status != ClaimStatusEnum.PendGovtPay && claim.Status != ClaimStatusEnum.PendGovtSubmission && claim.Status != ClaimStatusEnum.PendGovtSubAppv && claim.Status != ClaimStatusEnum.PendingGovtIssue)
                        {
                            rsp.EntityList.Add(new GenericEntity()
                            {
                                UniqueId = lineNbr.ToString(),
                                Context = "Reconcile Issue",
                                Name = string.Format("Claim {0} is not in PendingGovt status", claimName),
                                AlternateId = claim.ClaimId.ToString()
                            });
                            if (columns[0] != "PAID")
                            {
                                claim.Status = ClaimStatusEnum.PendingGovtIssue;
                                claims.Add(claim.ClaimId.ToString(), claim);
                                if (claim.Comments == null) claim.Comments = new List<Comment>();
                                claim.Comments.Add(new Comment() { CommentDate = DateTime.UtcNow, CommentId = "NEW", CommentText = string.Format("{0}: {1}", columns[0], columns[18]) });
                            }
                            continue;

                        }

                        if (columns[0] != "PAID")
                        {
                            rsp.EntityList.Add(new GenericEntity()
                            {
                                UniqueId = lineNbr.ToString(),
                                Context = "Reconcile Issue",
                                Name = string.Format("{0}: {1}", columns[0], claimName),
                                AlternateId = claim.ClaimId.ToString()
                            });
                            claim.Status = ClaimStatusEnum.PendingGovtIssue;
                            claims.Add(claim.ClaimId.ToString(), claim);
                            if (claim.Comments == null) claim.Comments = new List<Comment>();
                            claim.Comments.Add(new Comment() { CommentDate = DateTime.UtcNow, CommentId = "NEW", CommentText = string.Format("{0}: {1}", columns[0], columns[18]) });
                            continue;
                        }



                        //got money - create payment and determine next steps
                        decimal paid = 0;
                        decimal TPLAmt = 0;
                        if (string.IsNullOrWhiteSpace(result.Notes)) result.Notes = columns[15];
                        if (!string.IsNullOrWhiteSpace(columns[14])) paid = decimal.Parse(columns[14]);
                        if (!string.IsNullOrWhiteSpace(columns[13])) TPLAmt = decimal.Parse(columns[13]);
                        ClaimPaymentRC cp = new ClaimPaymentRC()
                        {
                            Amount = paid,
                            Claim = new GenericEntity() { UniqueId = claim.ClaimId.ToString(), Context = "Claim", Name = null },
                            ClaimDate = claim.ClaimDate,
                            Client = claim.Client,
                            PayDate = DateTime.Today,
                            Source = claim.PendingWith
                        };
                        result.Amount += paid;
                        claim.Payments.Add(cp);
                        result.Claims.Add(cp);
                        claims.Add(claim.ClaimId.ToString(), claim);
                        claim.Status = ClaimStatusEnum.Paid;
                        if (claim.Comments == null) claim.Comments = new List<Comment>();
                        claim.Comments.Add(new Comment() { CommentDate = DateTime.UtcNow, CommentId = "NEW", CommentText = string.Format("{0}: {1}", columns[0], columns[18]) });
                    }
                }
            }
            return result;
        }


        private static List<string> CsvParser(string csvText)
        {
            List<string> tokens = new List<string>();

            int last = -1;
            int current = 0;
            bool inText = false;

            while (current < csvText.Length)
            {
                switch (csvText[current])
                {
                    case '"':
                        inText = !inText; break;
                    case ',':
                        if (!inText)
                        {
                            tokens.Add(csvText.Substring(last + 1, (current - last)).Trim(' ', ',').Replace("\"", ""));
                            last = current;
                        }
                        break;
                    default:
                        break;
                }
                current++;
            }

            if (last != csvText.Length - 1)
            {
                tokens.Add(csvText.Substring(last + 1).Trim().Replace("\"", ""));
            }

            return tokens;
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ClaimReconciler() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        public ClaimInfo GetClaim(long id)
        {
            ClaimInfo result = null;
            using (SqlConnection cn = new SqlConnection(_UserClaim.conStr))
            {
                cn.Open();
                SqlCommand myCmd = new SqlCommand("Claim_l_sp", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                //SqlCommand myCmd = this.CreateSPCommand("Claim_l_sp");
                myCmd.Parameters.AddWithValue("@ID", id);


                try
                {
                    DataSet ds = new DataSet();
                    sqlHelper.ExecuteSqlDataAdapter(myCmd, ds);
         
                    result = PopulateClaimInfo(ds.Tables[0]);
                    if (result != null)
                    {
                        result.Payments = new List<ClaimPaymentRC>();

                        ClaimPaymentRC p = PopulateClaimPayment(ds.Tables[1]);
                        if (p != null) result.Payments.Add(p);
         
                        result.Comments = new List<Comment>();
         
                        if (ds.Tables[2] != null)
                        {
                            foreach (DataRow dr in ds.Tables[2].Rows)
                            {
                                var comment = new Comment()
                                {
                                    CommentId = dr["ClaimCommentId"].ToString(),
                                    CommentDate = dr["MadeAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["MadeAt"]),
                                    CommentText = dr["Comments"].ToString()
                                };
                                if (dr["StaffId"] == DBNull.Value)
                                {
                                    comment.Commentor = new GenericEntity()
                                    {
                                        UniqueId = "System",
                                        Context = "System",
                                        Name = "System"
                                    };
                                }
                                else
                                {
                                    comment.Commentor = new GenericEntity()
                                    {
                                        UniqueId = dr["StaffId"].ToString(),
                                        Context = "Staff",
                                        Name = null,
                                        AlternateId = dr["UserId"].ToString()
                                    };
                                }
                                result.Comments.Add(comment);
                            }
                        }
                        
                        result.Appointments = new List<ClaimAppointmentInfo>();
                        
                        if (ds.Tables[3] != null)
                        {
                            foreach (DataRow dr in ds.Tables[3].Rows)
                            {
                                result.Appointments.Add(PopulateClaimAppointment(dr));
                            }
                        }
                        
                        if (ds.Tables[4] != null)
                        {
                            foreach (DataRow dr in ds.Tables[4].Rows)
                            {
                                FillInClaimCPT(result, dr);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    myCmd.Dispose();
                }
            }
            return result;
        }
        private ClaimAppointmentInfo PopulateClaimAppointment(DataRow dr)
        {
            ClaimAppointmentInfo result = new ClaimAppointmentInfo()
            {
                AppointmentId = Convert.ToInt64(dr["AppointmentId"] == DBNull.Value ? 0 : dr["AppointmentId"]),
                Units = Convert.ToDecimal(dr["Units"] == DBNull.Value ? 0 : dr["Units"]),
                Amount = Convert.ToDecimal(dr["Amount"] == DBNull.Value ? 0 : dr["Amount"]),
                GovtUnits = Convert.ToDecimal(dr["GovtUnits"] == DBNull.Value ? 0 : dr["GovtUnits"]),
                Service = new GenericEntity()
                {
                    UniqueId = dr["ClientServiceId"].ToString(),
                    Context = "ClientService",
                    Name = dr["ServiceName"].ToString(),
                    AlternateId = dr["ServiceId"].ToString()
                },
                Start = dr["StartAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(dr["StartAt"]),
                Status = (AppointmentStatusEnum)Convert.ToInt32(dr["AppointmentStatusId"].ToString())
            };

            //if (dr["ServiceDisciplineId"] != DBNull.Value)
            //{
            //    result.ServiceDisciplineId = Convert.ToInt32(dr["ServiceDisciplineId"].ToString());
            //}
            //if (dr["DisciplineCode"] != DBNull.Value)
            //{
            //    result.DisciplineCode = dr["DisciplineCode"].ToString();
            //}
            return result;

        }

        private ClaimPaymentRC PopulateClaimPayment(DataTable dt)
        {
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                var p = new ClaimPaymentRC()
                {
                    Amount = Convert.ToDecimal(dr["Amount"] == DBNull.Value ? 0 : dr["Amount"]),
                    Denial = Convert.ToBoolean(dr["IsDenial"] == DBNull.Value ? 0 : dr["IsDenial"]),
                    PayDate = Convert.ToDateTime(dr["ReceivedAt"] == DBNull.Value ? DateTime.MinValue : dr["ReceivedAt"]),
                    PaymentId = Convert.ToInt64(dr["PaymentId"] == DBNull.Value ? 0 : dr["PaymentId"]),
                    VoidedAt = Convert.ToDateTime(dr["VoidedAt"] == DBNull.Value ? DateTime.MinValue : dr["VoidedAt"]),
                    ClaimDate = Convert.ToDateTime(dr["ClaimDate"] == DBNull.Value ? DateTime.MinValue : dr["ClaimDate"]),
                    CPTs = new List<CPTRate>()
                };
                var tid = dr["PaymentTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PaymentTypeId"].ToString());
                if (tid != 0)
                {
                    PaymentTypeEnum tpe = (PaymentTypeEnum)tid;
                    switch (tpe)
                    {
                        case PaymentTypeEnum.Insurance:
                            p.Source = new GenericEntity()
                            {
                                UniqueId = dr["InsurancePolicyId"].ToString(),
                                Context = "InsurancePolicy",
                                Name = string.Format("{0} ({1})", dr["Payer"].ToString(), dr["InsuredIdNo"].ToString()),
                                AlternateId = dr["InsuranceCompanyId"].ToString()
                            };
                            break;
                        case PaymentTypeEnum.Govt:
                            p.Source = new GenericEntity()
                            {
                                UniqueId = dr["GovernmentProgramId"].ToString(),
                                Context = "GovernmentProgram",
                                Name = "Payer",
                                AlternateId = null
                            };
                            break;
                        default:
                            p.Source = new GenericEntity() { UniqueId = "0", Context = "PrivatePayer", Name = dr["Payer"].ToString() };
                            break;
                    }
                }
                string dreason = dr["DenialReasonId"] == DBNull.Value ? "" : dr["DenialReasonId"].ToString();
                if (!string.IsNullOrWhiteSpace(dreason))
                {
                    p.DenialReason = new GenericEntity() { UniqueId = dreason.TrimEnd(), Context = "Denial Reason", Name = dr["ReasonText"].ToString() };
                }
                if (dr["ClaimId"] != DBNull.Value)
                {
                    p.Claim = new GenericEntity() { UniqueId = dr["ClaimId"].ToString(), Context = "Claim", Name = null };
                }
                if (dr["ClientId"] != DBNull.Value)
                {
                    var c = PopulateClientPerson(dr);
                    p.Client = new GenericEntity() { UniqueId = c.UniqueId, Context = "Client", Name = string.Format("{0} {1} {2}", c.LastName, c.FirstName, c.MiddleName) };
                }
                return p;
            }
            else
            {
                return null;
            }
        }

        private PersonField PopulateClientPerson(DataRow dr)
        {
            var gid = dr["ClientId"] == DBNull.Value ? "0" : dr["ClientId"].ToString();
            if (gid != "0")
            {
                return new PersonField()
                {
                    UniqueId = gid,
                    FirstName = dr["ClientFirst"].ToString(),
                    MiddleName = dr["ClientMiddle"].ToString(),
                    LastName = dr["ClientLast"].ToString(),
                    Context = "Client"
                };
            }
            return null;
        }
        private void FillInClaimCPT(ClaimInfo claim, DataRow dr)
        {
            var serviceId = Convert.ToInt64(dr["AppointmentId"]);

            var rt = new CPTRate()
            {
                Amount = Convert.ToDecimal(dr["Amount"]),
                CPT = new GenericEntity()
                {
                    UniqueId = dr["CPTCode"].ToString(),
                    Context = "CPT",
                    Name = null,
                    AlternateId = null
                },
                Mod1 = dr["Mod1"].ToString(),
                Mod2 = dr["Mod2"].ToString(),
                Mod3 = dr["Mod3"].ToString()
            };

            if (dr["ServiceCPTRateId"] != DBNull.Value)
            {
                rt.ServiceCPTRateId = Convert.ToInt32(dr["ServiceCPTRateId"].ToString());
            }

            var a = claim.Appointments.Find(appt => appt.AppointmentId == serviceId);
            if (a != null)
            {
                if (a.CPTs == null) a.CPTs = new List<CPTRate>();
                a.CPTs.Add(rt);
            }
        }

        private ClaimInfo PopulateClaimInfo(DataTable dt)
        {
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                var result = new ClaimInfo()
                {
                    ClaimId = Convert.ToInt64(dr["ClaimId"] == DBNull.Value ? 0 : dr["ClaimId"]),
                    Status = (ClaimStatusEnum)Convert.ToInt32(dr["ClaimStatusId"] == DBNull.Value ? 0 : dr["ClaimStatusId"]),
                    UpdatedAt = Convert.ToDateTime(dr["StatusUpdatedAt"] == DBNull.Value ? DateTime.MinValue : dr["StatusUpdatedAt"]),
                    ClaimDate = Convert.ToDateTime(dr["ClaimDate"] == DBNull.Value ? DateTime.MinValue : dr["ClaimDate"]),
                    Appointments = new List<ClaimAppointmentInfo>(),
                    Payments = new List<ClaimPaymentRC>(),
                    Location = (LocationEnum)Convert.ToInt32(dr["LocationTypeId"] == DBNull.Value ? 0 : dr["LocationTypeId"]),
                    SubStatus = Convert.ToString(dr["SubStatus"] == DBNull.Value ? "" : dr["SubStatus"]),
                    ActivityId = Convert.ToString(dr["ActivityId"] == DBNull.Value ? "" : dr["ActivityId"]),
                    TransactionId = Convert.ToString(dr["TransactionId"] == DBNull.Value ? "" : dr["TransactionId"])
                };

                //if (dr["IsNonTelehealth"] != DBNull.Value)
                //{
                //    result.IsNonTelehealth = dr["IsNonTelehealth"].ToString() == "0" ? false : true;
                //}

                //if (dr["IsTelehealthClient"] == DBNull.Value)
                //{
                //    result.IsTelehealthClient = dr["IsTelehealthClient"].ToString() == "0" ? false : true;
                //}

                //if (dr["TelehealthUpdatedAt"] == DBNull.Value)
                //{
                //    result.TelehealthUpdatedAt = Convert.ToDateTime(dr["TelehealthUpdatedAt"]);
                //}

                var c = PopulateClientPerson(dr);
                result.Client = new GenericEntity() { UniqueId = c.UniqueId, Context = "Client", Name = string.Format("{0} {1} {2}", c.LastName, c.FirstName, c.MiddleName) };

                if (result.Status == ClaimStatusEnum.PendInsPay || result.Status == ClaimStatusEnum.PendInsSubmission || result.Status == ClaimStatusEnum.PendingWaiver)
                {
                    result.PendingWith = new GenericEntity()
                    {
                        UniqueId = dr["InsurancePolicyId"].ToString(),
                        Context = "InsurancePolicy",
                        Name = dr["InsuredIdNo"].ToString(),
                        AlternateId = dr["InsuranceCompanyId"].ToString()
                    };
                }
                else if (result.Status == ClaimStatusEnum.PendGovtSubmission || result.Status == ClaimStatusEnum.PendGovtPay || result.Status == ClaimStatusEnum.PendGovtSubAppv || result.Status == ClaimStatusEnum.PendingGovtIssue)
                {
                    result.PendingWith = new GenericEntity()
                    {
                        UniqueId = dr["InsurancePolicyId"].ToString(),
                        Context = "GovtProgram",
                        Name = dr["InsuredIdNo"].ToString(),
                        AlternateId = dr["InsuranceCompanyId"].ToString()
                    };
                }

                int bid = dr["ApproverStaffId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ApproverStaffId"].ToString());
                if (bid != 0)
                    result.Approver = new GenericEntity()
                    {
                        UniqueId = dr["ApproverStaffId"].ToString(),
                        Context = "Staff",
                        Name = null,
                        AlternateId = dr["ApproverUserId"].ToString()
                    };
                bid = dr["StaffId"] == DBNull.Value ? 0 : Convert.ToInt32(dr["StaffId"].ToString());
                if (bid != 0)
                    result.Provider = new GenericEntity()
                    {
                        UniqueId = dr["StaffId"].ToString(),
                        Context = "Staff",
                        Name = null,
                        AlternateId = dr["UserId"].ToString()
                    };
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}