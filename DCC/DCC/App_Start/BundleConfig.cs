using System.Web;
using System.Web.Optimization;

namespace DCC
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
      
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));
            bundles.Add(new ScriptBundle("~/bundles/DCC").Include(
                        "~/Scripts/DCC/DCC.js"));
            bundles.Add(new ScriptBundle("~/bundles/Home").Include(
                       "~/Scripts/DCC/Home.js"));
            bundles.Add(new ScriptBundle("~/bundles/Staff").Include(
                "~/Scripts/DCC/Staff.js"));

        }
    }
}
