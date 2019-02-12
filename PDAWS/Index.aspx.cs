using System;

namespace PDAWS
{
    public partial class Index : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 解除销售订单的整单发货标识
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnSingleShipment_Click(object sender, EventArgs e)
        {
            string strBillNo = txtBillNo.Text.Trim();
            string strResult;

            if (strBillNo.Equals(string.Empty))
                return;

            strResult = FactorySQL.Common.Update_SingleShipment(strBillNo, rbtSingle.Checked);
            Response.Write("<script>alert('" + strResult + "')</script>");
            txtBillNo.Text = "";
        }

        /// <summary>
        /// 修改销售订单可出数量
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnUpdateCanOutQty_Click(object sender, EventArgs e)
        {
            string strBillNo, strMTL, strResult;
            decimal FCanOutQty = 0;

            strBillNo = txtBillNo2.Text.Trim();
            strMTL = txtMTL.Text.Trim();

            if (strBillNo.Equals(string.Empty) || strMTL.Equals(string.Empty))
                return;

            try
            {
                FCanOutQty = decimal.Parse(txtFCanOutQty.Text.Trim());
            }
            catch { return; }

            strResult = FactorySQL.Common.UpdateCanOutQty(strBillNo, strMTL, FCanOutQty);
            Response.Write("<script>alert('" + strResult + "')</script>");

            txtBillNo2.Text = "";
            txtMTL.Text = "";
            txtFCanOutQty.Text = "";
        }
    }
}