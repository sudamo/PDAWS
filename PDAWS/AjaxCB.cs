using System.Web;
using System.Web.SessionState;
using AjaxPro;

namespace PDAWS
{
    [AjaxNamespace("AjaxCB")]
    public class AjaxCB
    {
        public AjaxCB() { }

        public static void Register()
        {
            Utility.RegisterTypeForAjax(typeof(AjaxCB));
        }

        public HttpContext Context
        {
            get { return HttpContext.Current; }
        }
        public HttpSessionState Session
        {
            get { return HttpContext.Current.Session; }
        }

        //[AjaxMethod(HttpSessionStateRequirement.Read)]
        [AjaxMethod]
        public string SSM(string pFBillNo, bool pSingle)
        {
            object obj;
            string strSQL;

            if (!pFBillNo.Contains("XSDD") && !pFBillNo.Contains("SEORD") && !pFBillNo.Contains("W"))
                return "[" + pFBillNo + "]并非销售订单";

            strSQL = "SELECT F_PAEZ_SINGLESHIPMENT FROM T_SAL_ORDER WHERE FBILLNO = '" + pFBillNo + "'";
            obj = FactorySQL.Common.SqlOperation(1, strSQL);

            if (obj == null)
                return "[" + pFBillNo + "]单号不存在，可能被删除。";
            else if (obj.ToString() == "1" && pSingle)
                return "[" + pFBillNo + "]已经是整单发货。";
            else if (obj.ToString() == "0" && !pSingle)
                return "[" + pFBillNo + "]已经是非整单发货。";

            if (pSingle)
                strSQL = "UPDATE T_SAL_ORDER SET F_PAEZ_SINGLESHIPMENT = '1' WHERE FBILLNO ='" + pFBillNo + "'";
            else
                strSQL = "UPDATE T_SAL_ORDER SET F_PAEZ_SINGLESHIPMENT = '0' WHERE FBILLNO ='" + pFBillNo + "'";
            obj = FactorySQL.Common.SqlOperation(0, strSQL);

            return "[" + pFBillNo + "]修改成功。";
        }
    }
}