
using DCC.Models.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCC.Models
{
    public class ClaimWrapper
    {

        ClaimDTO _base;
        public ClaimWrapper(ClaimDTO source)
        {
            _base = source;
            if (_base.Payments == null) _base.Payments = new List<ClaimPaymentDTO>();
        }

        public ClaimDTO Claim { get { return _base; } }
        public int ClaimId
        {
            get
            {
                return _base.ClaimId;
            }
        }

        public bool? IsNonTelehealth
        {
            get
            {
                return _base.IsNonTelehealth;
            }
        }

        public decimal AbsentUnits
        {
            get
            {
                return _base.Appointments.Sum(a => a.StatusId == (int)AppointmentStatusEnum.NoShow ? a.GovtUnits.GetValueOrDefault(0) : 0);
            }
        }
        public decimal DeliveredUnits
        {
            get
            {
                return _base.Appointments.Sum(a => a.StatusId != (int)AppointmentStatusEnum.NoShow ? a.GovtUnits.GetValueOrDefault(0) : 0);
            }
        }
        public decimal AmountDue
        {
            get
            {
                var init = (
                    (this.DeliveredUnits == 0 ? 
                        (this.AbsentUnits == 0.5M ? Math.Round(this.AbsentUnits * this.Rate, 2, MidpointRounding.AwayFromZero) : 0)
                        : Math.Round(
                            (
                                (this.DeliveredUnits * this.Rate) < (this.TPLAmount1.GetValueOrDefault() + this.TPLAmount2.GetValueOrDefault() + this.TPLAmount3.GetValueOrDefault()) ? 
                                    0.0M
                                    : (this.DeliveredUnits * this.Rate) - this.TPLAmount1.GetValueOrDefault() - this.TPLAmount2.GetValueOrDefault() - this.TPLAmount3.GetValueOrDefault()
                             ), 2, MidpointRounding.AwayFromZero)
                     ));

                // var init = Math.Ceiling((_base.AmountDue - _base.Payments.Sum(p => p.Amount.GetValueOrDefault(0))) * 100) / 100;
                return init < 0 ? 0 : init;
            }
        }
        internal void CapPayments()
        {
            if (_base.Payments == null) return;
            //Translate denial for deductables to $0 payments
            foreach (var p in _base.Payments)
            {
                if (!string.IsNullOrWhiteSpace(p.DenialReasonId) || p.DenialReasonId != "1") continue;
                p.IsDenial = false;
                p.Amount = 0;
            }
            //Remove all other denials
            _base.Payments.RemoveAll(p => p.IsDenial || p.VoidedAt.HasValue || p.PaymentTypeId == (int)PaymentTypeEnum.Private);
            //No work to do if the amount of payments <= amount due
            if (!_base.Payments.Any() || _base.Payments.Sum(p => p.Amount.GetValueOrDefault(0)) <= _base.AmountDue) return;
            decimal total = 0;
            int cnt = _base.Payments.Count;
            List<ClaimPaymentDTO> toRemove = new List<ClaimPaymentDTO>();
            for (int x = 0; x < cnt; x++)
            {
                var pay = _base.Payments[x];
                if (total >= _base.AmountDue && pay.Amount.GetValueOrDefault(0) > 0)
                {
                    toRemove.Add(pay);
                }
                if (pay.Amount.GetValueOrDefault(0) > 0)
                {
                    if (total + pay.Amount.GetValueOrDefault(0) > _base.AmountDue)
                    {
                        pay.Amount = _base.AmountDue - total;
                        total = _base.AmountDue;
                    }
                    else
                    {
                        total += pay.Amount.GetValueOrDefault(0);
                    }
                }
            }

            foreach (var i in toRemove)
            {
                _base.Payments.Remove(i);
            }
        }
        public string ClientGovtProgId
        {
            get
            {
                return _base.ClientGovtProgramId;
            }
        }
        public DateTime ServiceDate
        {
            get { return _base.ClaimDate; }
        }
        public string GovtSvcCode
        {
            get
            {
                //if (_base.StatusId != (int)ClaimStatusEnum.PendGovtPay && _base.StatusId != (int)ClaimStatusEnum.PendGovtSubmission)
                //{
                //    return null;
                //}
                return _base.Appointments[0].DisciplineCode;
            }
        }
        public string StaffNPI
        {
            get
            {
                return _base.ProviderNPI;
            }
        }
        public string StaffStateId
        {
            get
            {
                return _base.ProviderStateMedicaid;

            }
        }
        public string OrderingPhysicianNPI
        {
            get
            {
                return _base.OrderingPhysicianNPI;

            }
        }
        public decimal Rate
        {
            get
            {
                return Math.Round(_base.AmountDue / _base.Appointments.Sum(a => a.GovtUnits.GetValueOrDefault(0)), 2);
            }
        }

        public List<ClientDiagnosisCodeDTO> DiagnosisCodes
        {
            get
            {
                return _base.DiagnosisCodes;
            }
        }

        public string TPLCode1
        {
            get
            {
                int i = 0;
                if (_base.DeductibleInd != 1 && _base.Payments.Count < 1 + i) return "";
                if (_base.DeductibleInd != 1 && _base.Payments[i].IsDenial) return "";
                return (_base.Payments.Count > 0 && !string.IsNullOrEmpty(_base.Payments[i].MCID)) ? _base.Payments[i].MCID : (_base.InsurancePolicy != null && _base.InsurancePolicy.MCID != null) ? _base.InsurancePolicy.MCID : "00000";
            }
        }
        public decimal? TPLAmount1
        {
            get
            {
                int i = 0;

                if (_base.DeductibleInd != 1 && _base.Payments.Count < 1 + i) return null;
                if (_base.DeductibleInd != 1 && _base.Payments[i].IsDenial) return null;
                return (_base.DeductibleInd != 1 || _base.Payments.Count > 0) ? _base.Payments[i].Amount : 0;
            }
        }
        public string TPLReCode1
        {
            get
            {
                return _base.DeductibleInd == 1 ? "01" : "";
            }
        }

        public string TPLCode2
        {
            get
            {
                int i = 1;
                if (_base.Payments.Count < 1 + i) return "";
                return (!string.IsNullOrEmpty(_base.Payments[i].MCID)) ? _base.Payments[i].MCID : (_base.InsurancePolicy != null && _base.InsurancePolicy.MCID != null) ? _base.InsurancePolicy.MCID : "00000";
            }
        }
        public decimal? TPLAmount2
        {
            get
            {
                int i = 1;

                if (_base.Payments.Count < 1 + i) return null;
                return _base.Payments[i].Amount;
            }
        }
        public string TPLReCode2
        {
            get
            {
                return "";
            }
        }

        public string TPLCode3
        {
            get
            {
                int i = 2;
                if (_base.Payments.Count < 1 + i) return "";
                return _base.Payments[i].MCID;
            }
        }
        public decimal? TPLAmount3
        {
            get
            {
                int i = 2;

                if (_base.Payments.Count < 1 + i) return null;
                return _base.Payments[i].Amount;
            }
        }
        public string TPLReCode3
        {
            get
            {
                int i = 2;

                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].Amount.GetValueOrDefault(0) == 0 && !_base.Payments[i].IsDenial ? "01" : "";
            }
        }

        public string TPLCode4
        {
            get
            {
                int i = 3;
                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].MCID;
            }
        }
        public decimal? TPLAmount4
        {
            get
            {
                int i = 3;

                if (_base.Payments.Count < 1 + i) return null;

                return _base.Payments[i].Amount;
            }
        }
        public string TPLReCode4
        {
            get
            {
                int i = 3;

                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].Amount.GetValueOrDefault(0) == 0 && !_base.Payments[i].IsDenial ? "01" : "";
            }
        }

        public string TPLCode5
        {
            get
            {
                int i = 4;
                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].MCID;
            }
        }
        public decimal? TPLAmount5
        {
            get
            {
                int i = 4;

                if (_base.Payments.Count < 1 + i) return null;

                return _base.Payments[i].Amount;
            }
        }
        public string TPLReCode5
        {
            get
            {
                int i = 4;

                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].Amount.GetValueOrDefault(0) == 0 && !_base.Payments[i].IsDenial ? "01" : "";
            }
        }

        public string TPLCode6
        {
            get
            {
                int i = 5;
                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].MCID;
            }
        }
        public decimal? TPLAmount6
        {
            get
            {
                int i = 5;

                if (_base.Payments.Count < 1 + i) return null;

                return _base.Payments[i].Amount;
            }
        }
        public string TPLReCode6
        {
            get
            {
                int i = 5;

                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].Amount.GetValueOrDefault(0) == 0 && !_base.Payments[i].IsDenial ? "01" : "";
            }
        }

        public string TPLCode7
        {
            get
            {
                int i = 6;
                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].MCID;
            }
        }
        public decimal? TPLAmount7
        {
            get
            {
                int i = 6;

                if (_base.Payments.Count < 1 + i) return null;

                return _base.Payments[i].Amount;
            }
        }
        public string TPLReCode7
        {
            get
            {
                int i = 6;

                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].Amount.GetValueOrDefault(0) == 0 && !_base.Payments[i].IsDenial ? "01" : "";
            }
        }

        public string TPLCode8
        {
            get
            {
                int i = 7;
                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].MCID;
            }
        }
        public decimal? TPLAmount8
        {
            get
            {
                int i = 7;

                if (_base.Payments.Count < 1 + i) return null;

                return _base.Payments[i].Amount;
            }
        }
        public string TPLReCode8
        {
            get
            {
                int i = 7;

                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].Amount.GetValueOrDefault(0) == 0 && !_base.Payments[i].IsDenial ? "01" : "";
            }
        }

        public string TPLCode9
        {
            get
            {
                int i = 8;
                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].MCID;
            }
        }
        public decimal? TPLAmount9
        {
            get
            {
                int i = 8;

                if (_base.Payments.Count < 1 + i) return null;

                return _base.Payments[i].Amount;
            }
        }
        public string TPLReCode9
        {
            get
            {
                int i = 8;

                if (_base.Payments.Count < 1 + i) return "";

                return _base.Payments[i].Amount.GetValueOrDefault(0) == 0 && !_base.Payments[i].IsDenial ? "01" : "";
            }
        }

        public string PlaceOfService
        {
            get
            {
                return _base.LocationTypeId == (int)LocationEnum.Clinic ? "11" : "12";
            }
        }
        public string ProcMod1
        {
            get
            {
                return _base.Appointments[0].IsTeletherapy ? "GT" : string.Empty;
            }
        }
        public string ProcMod2
        {
            get
            {
                return string.Empty;
            }
        }
        public string ProcMod3
        {
            get
            {
                return string.Empty;
            }
        }

    }

}
