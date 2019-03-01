using System;

namespace PDAWS
{
    public partial class Index : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            AjaxCB.Register();
            //AjaxPro.Utility.RegisterTypeForAjax(typeof(AjaxCB));
        }
    }
}