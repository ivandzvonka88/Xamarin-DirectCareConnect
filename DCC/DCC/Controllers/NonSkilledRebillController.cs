using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using DCC.Models;
using System.Threading.Tasks;
using DCC.SQLHelpers.Helpers;

namespace DCC.Controllers
{
    public class NonSkilledRebillController : DCCBaseController
    {
        private readonly SQLHelper sqlHelper;

        public NonSkilledRebillController()
        {
            sqlHelper = new SQLHelper();
        }
        
        int ITEMS_PER_PAGE = 20;


        [Authorize]
        public ActionResult Index()
        {
            NonSkilledRebillPage r = GetRebillPage(1);
            setViewModelBase((ViewModelBase)r);
            return View("Index", r);
        }

        [Authorize]
        public ActionResult GetRebills(int page)
        {
            NonSkilledRebillPage r = GetRebillPage(page);
            setViewModelBase((ViewModelBase)r);
            return View("RebillPage", r);
        }


        [HttpPost]
        [ValidateJsonAntiForgeryToken]
        [Authorize]
        public ActionResult SetRebills(NonSkilledRebillPage r)
        {

            DataTable DDDHCBSReBills = new DataTable();
            DDDHCBSReBills.Columns.Add("tbl");
            DDDHCBSReBills.Columns.Add("id");
            DDDHCBSReBills.Columns.Add("un");
            DDDHCBSReBills.Columns.Add("ajun");
            DDDHCBSReBills.Columns.Add("pd");

            foreach(var item in r.b)
            {
                if (item.chg != 0)
                {
                    DataRow nRow = DDDHCBSReBills.NewRow();
                    int pd = 1;
                    if (item.un + item.ajun == 0)
                        pd = 3;
                    nRow["tbl"] = item.tbl;
                    nRow["id"] = item.id;
                    nRow["un"] = item.un;
                    nRow["ajun"] = item.ajun;
                    nRow["pd"] = pd;
                    DDDHCBSReBills.Rows.Add(nRow);
                }               
            }
          
            DataSet ds = new DataSet();
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_DDDReBillsSetUnskilled", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@DDDHCBSReBills", DDDHCBSReBills);
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            ds.Dispose();
            r = GetRebillPage(r.pg);
            setViewModelBase((ViewModelBase)r);
            return View("RebillPage", r);
        }

        private NonSkilledRebillPage GetRebillPage(int pg)
        {

            NonSkilledRebillPage r = new NonSkilledRebillPage();
            DataSet ds = new DataSet();
            using (SqlConnection cn = new SqlConnection(UserClaim.conStr))
            {
                SqlCommand cmd = new SqlCommand("sp_DDDReBillsGetUnskilled", cn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                sqlHelper.ExecuteSqlDataAdapter(cmd, ds);
            }
            r.pg = pg;
            r.pgCnt = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(ds.Tables[0].Rows.Count) / ITEMS_PER_PAGE));
            if (r.pg > r.pgCnt && r.pg > 1) r.pg -= 1;

            int pageItemStart = (r.pg - 1) * ITEMS_PER_PAGE;
            int pageItemEnd = pageItemStart + ITEMS_PER_PAGE;
            if (pageItemEnd > ds.Tables[0].Rows.Count) pageItemEnd = ds.Tables[0].Rows.Count;

            r.pageInfo = new string[r.pgCnt];
            for (int i = 0; i < r.pgCnt; i++)
            {
                if (i != r.pgCnt - 1) r.pageInfo[i] = ds.Tables[0].Rows[i * ITEMS_PER_PAGE].ItemArray[1] + " - " + ds.Tables[0].Rows[((i + 1) * ITEMS_PER_PAGE) - 1].ItemArray[1];
                else r.pageInfo[i] = ds.Tables[0].Rows[i * ITEMS_PER_PAGE].ItemArray[1] + " - " + ds.Tables[0].Rows[ds.Tables[0].Rows.Count - 1].ItemArray[1];
            }

            r.b = new billItem[pageItemEnd - pageItemStart];
            int RowCount = 0;
            for (int i = pageItemStart; i < pageItemEnd; i++)
            {
                DataRow dr = ds.Tables[0].Rows[i];
                r.b[RowCount] = new billItem();
                r.b[RowCount].tbl = (string)dr["tbl"];
                r.b[RowCount].id = (int)dr["id"];
                r.b[RowCount].loc = (string)dr["loc"];
                r.b[RowCount].svc = (string)dr["svc"];

                DateTime dt = (DateTime)dr["dt"];
                r.b[RowCount].month = dt.ToString("MMM");
                r.b[RowCount].day = dt.Day;
                r.b[RowCount].year = dt.Year;
                r.b[RowCount].fn =(string)dr["fn"];
                r.b[RowCount].ln = (string)dr["ln"];

                r.b[RowCount].un = (decimal)dr["units"];
                r.b[RowCount].ajun = (decimal)dr["ajun"];
                r.b[RowCount].er = (string)dr["errorMsg"];
                r.b[RowCount].rat = (byte)dr["ratio"];
                if (dr["ru"] != DBNull.Value)
                    r.b[RowCount].au = "Auth: " + ((DateTime)dr["stdt"]).ToShortDateString() + " - " + ((DateTime)dr["eddt"]).ToShortDateString() + "   Rem: " + dr["ru"];
                RowCount++;
            }





            return r;
        }


    }
}