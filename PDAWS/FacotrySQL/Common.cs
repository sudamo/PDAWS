using System.Data;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace PDAWS.FactorySQL
{
    static class Common
    {
        #region STATIC
        private static string C_CONNECTIONSTRINGORL;
        static Common()
        {
            C_CONNECTIONSTRINGORL = ConfigurationManager.AppSettings["C_CONNECTIONSTRINGORL"];
        }
        #endregion

        /// <summary>
        /// 修改销售订单的整单发货标识
        /// </summary>
        /// <param name="pFBillNo">销售订单编号</param>
        /// <param name="pSingle">是否整单</param>
        /// <returns></returns>
        public static string Update_SingleShipment(string pFBillNo, bool pSingle)
        {
            object obj;
            string strSQL;

            if (!pFBillNo.Contains("XSDD") && !pFBillNo.Contains("SEORD") && !pFBillNo.Contains("W"))
                return "[" + pFBillNo + "]并非销售订单";

            strSQL = "SELECT F_PAEZ_SINGLESHIPMENT FROM T_SAL_ORDER WHERE FBILLNO = '" + pFBillNo + "'";
            obj = SqlOperation(1, strSQL);

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
            obj = SqlOperation(0, strSQL);

            return "[" + pFBillNo + "]修改成功。";
        }

        /// <summary>
        /// 修改销售订单可出数量
        /// </summary>
        /// <param name="pFBillNo">销售订单编号</param>
        /// <param name="pMTL">物料编码</param>
        /// <param name="pFCanOutQty">可出数量</param>
        /// <returns></returns>
        public static string UpdateCanOutQty(string pFBillNo, string pMTL, decimal pFCanOutQty)
        {
            object obj;
            string strSQL;

            if (!pFBillNo.Contains("XSDD") && !pFBillNo.Contains("SEORD") && !pFBillNo.Contains("W"))
                return "[" + pFBillNo + "]并非销售订单";

            strSQL = @"SELECT AE.FENTRYID,AE.FQTY
                    FROM T_SAL_ORDER A
                    INNER JOIN T_SAL_ORDERENTRY AE ON A.FID = AE.FID
                    INNER JOIN T_BD_MATERIAL MTL ON AE.FMATERIALID = MTL.FMATERIALID
                    WHERE A.FBILLNO = '" + pFBillNo + "' AND MTL.FNUMBER = '" + pMTL + "'";

            obj = SqlOperation(3, strSQL);

            if (obj == null || ((DataTable)obj).Rows.Count == 0)
                return "[" + pFBillNo + "]单号或物料[" + pMTL + "]不存在。";

            int iFEntryID = int.Parse(((DataTable)obj).Rows[0]["FENTRYID"].ToString());
            decimal dFQty = decimal.Parse(((DataTable)obj).Rows[0]["FQTY"].ToString());

            if (dFQty < pFCanOutQty)
                return "物料[" + pMTL + "]的订单数量[" + dFQty.ToString() + "]小于可出数量设置值[" + pFCanOutQty.ToString() + "]";

            strSQL = "UPDATE T_SAL_ORDERENTRY_R SET FCANOUTQTY = " + pFCanOutQty + ",FBASECANOUTQTY = " + pFCanOutQty + ",FSTOCKBASECANOUTQTY = " + pFCanOutQty + " WHERE FENTRYID = " + iFEntryID.ToString();

            obj = SqlOperation(0, strSQL);

            return "[" + pFBillNo + "]修改成功。";
        }

        /// <summary>
        /// 修改销售订单的销售组织
        /// </summary>
        /// <param name="pFBillNo">销售订单编号</param>
        /// <returns></returns>
        public static string Update_SaleOrganization(string pFBillNo)
        {
            object obj;
            string strSQL;

            if (!pFBillNo.Contains("XSDD") && !pFBillNo.Contains("SEORD") && !pFBillNo.Contains("W"))
                return "[" + pFBillNo + "]并非销售订单";
            //根据销售订单编号获取销售组织内码
            strSQL = "SELECT FSALEORGID FROM T_SAL_ORDER WHERE FBILLNO = '" + pFBillNo + "'";
            obj = SqlOperation(1, strSQL);
            //判断
            if (obj == null)
                return "[" + pFBillNo + "]单号不存在，可能被删除。";
            else if (obj.ToString() == "479482")
                return "[" + pFBillNo + "]销售组织已经是电商二部。";
            else if (obj.ToString() != "477965")
                return "[" + pFBillNo + "]销售组织不是电商一部，不能修改。";
            //更新销售组织和销售部门
            strSQL = "UPDATE T_SAL_ORDER SET FSALEORGID = 479482, FSALEDEPTID = 623823 WHERE FBILLNO = '" + pFBillNo + "'";
            obj = SqlOperation(0, strSQL);
            //更新客户内码
            strSQL = "UPDATE T_SAL_ORDER SET FCUSTID = 451165337 WHERE FSALEORGID = 479482 AND FCUSTID = 450744677 AND FBILLNO = '" + pFBillNo + "'";
            obj = SqlOperation(0, strSQL);
            //更新结算组织
            strSQL = "UPDATE T_SAL_ORDERENTRY_F SET FSETTLEORGID = 479482 WHERE FID = (SELECT FID FROM T_SAL_ORDER WHERE FBILLNO = '" + pFBillNo + "')";
            obj = SqlOperation(0, strSQL);
            return "[" + pFBillNo + "]修改成功。";
        }

        /// <summary>
        /// 获取组织
        /// </summary>
        /// <returns></returns>
        public static DataTable GetOrganization()
        {
            string strSQL;

            strSQL = @"SELECT ORG.FORGID FValue,ORGL.FName
            FROM T_ORG_ORGANIZATIONS ORG
            INNER JOIN T_ORG_ORGANIZATIONS_L ORGL ON ORG.FORGID = ORGL.FORGID AND ORGL.FLOCALEID = 2052
            WHERE ORG.FDOCUMENTSTATUS = 'C' AND ORG.FFORBIDSTATUS = 'A' AND ORG.FACCTORGTYPE = 2
            ORDER BY ORGL.FNAME DESC";

            return (DataTable)SqlOperation(3, strSQL);
        }

        #region 数据库操作
        /// <summary>
        /// 数据库操作
        /// </summary>
        /// <param name="pType">0、NonQuery;1、Scalar;2、Reader;3、DataTable;4、DataSet</param>
        /// <param name="pStrSQL">SQL Sentence</param>
        /// <returns></returns>
        public static object SqlOperation(int pType, string pStrSQL)
        {
            object obj;
            OracleDataAdapter adp;
            DataTable dt;
            DataSet ds;

            OracleConnection conn = new OracleConnection(C_CONNECTIONSTRINGORL);

            try
            {
                conn.Open();
                OracleCommand cmd = conn.CreateCommand();
                cmd.CommandText = pStrSQL;

                switch (pType)
                {
                    case 0:
                        obj = cmd.ExecuteNonQuery();
                        break;
                    case 1:
                        obj = cmd.ExecuteScalar();
                        break;
                    case 2:
                        obj = cmd.ExecuteReader();
                        break;
                    case 3:
                        dt = new DataTable();
                        adp = new OracleDataAdapter(pStrSQL, conn);
                        adp.Fill(dt);
                        obj = dt;
                        break;
                    case 4:
                        ds = new DataSet();
                        adp = new OracleDataAdapter(pStrSQL, conn);
                        adp.Fill(ds);
                        obj = ds;
                        break;
                    default:
                        obj = null;
                        break;
                }
            }
            catch { return null; }
            finally
            {
                conn.Close();
            }

            return obj;
        }
        #endregion
    }
}