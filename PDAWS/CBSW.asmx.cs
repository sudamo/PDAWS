using System.Data;
using System.Web.Services;

namespace PDAWS
{
    /// <summary>
    /// CBSW 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    // [System.Web.Script.Services.ScriptService]
    public class CBSW : WebService
    {
        /// <summary>
        /// 登录验证
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [WebMethod]
        public bool CheckLogin(string userName, string password)
        {
            return FactorySQL.cnCB.CheckLogin(userName, password);
        }

        /// <summary>
        /// 操作订单
        /// </summary>
        /// <param name="pOperation"></param>
        /// <param name="pJson"></param>
        /// <returns></returns>
        [WebMethod]
        public string OperateBill(string pOperation, string pJson)
        {
            return FactorySQL.cnCB.OperateBill(pOperation, pJson);
        }

        /// <summary>
        /// 根据类型获取信息
        /// </summary>
        /// <param name="pString"></param>
        /// <param name="pType"></param>
        /// <returns></returns>
        [WebMethod]
        public DataTable GetTable(string pString, string pType)
        {
            return FactorySQL.cnCB.GetTable(pString, pType);
        }

        /// <summary>
        /// 标量查询
        /// </summary>
        /// <param name="pString"></param>
        /// <param name="pType"></param>
        /// <param name="pIndex"></param>
        /// <returns></returns>
        [WebMethod]
        public object ExecuteScalar(string pString, string pType, int pIndex)
        {
            return FactorySQL.cnCB.ExecuteScalar(pString, pType, pIndex);
        }

        /// <summary>
        /// 生产入库
        /// </summary>
        /// <param name="pBarcode"></param>
        /// <param name="pInventory"></param>
        /// <returns></returns>
        [WebMethod]
        public string Instock(string pBarcode, string pInventory)
        {
            return FactorySQL.cnCB.InStock(pBarcode, pInventory);
        }

        /// <summary>
        /// 销售出库
        /// </summary>
        /// <param name="pBarcodeList"></param>
        /// <returns></returns>
        [WebMethod]
        public string SalOutStock(string pBarcodeList)
        {
            return FactorySQL.cnCB.SalOutStock(pBarcodeList);
        }

        /// <summary>
        /// 寄售订单扫描生成调拨单
        /// </summary>
        /// <param name="strBarcodes">条码集</param>
        /// <returns></returns>
        [WebMethod]
        public string Trans(string strBarcodes)
        {
            return FactorySQL.cnCB.Trans(strBarcodes);
        }

        /// <summary>
        /// 装箱
        /// </summary>
        /// <param name="pPackageId"></param>
        /// <param name="pCustId"></param>
        /// <param name="pBarcodes"></param>
        /// <param name="pVolume"></param>
        /// <param name="pType"></param>
        /// <param name="pMaxBoxNumber"></param>
        /// <returns></returns>
        [WebMethod]
        public string Package(int pPackageId, int pCustId, string pBarcodes, double pVolume, int pType, int pMaxBoxNumber)
        {
            return FactorySQL.cnCB.Package(pPackageId, pCustId, pBarcodes, pVolume, pType, pMaxBoxNumber);
        }

        /// <summary>
        /// PDA日志
        /// </summary>
        /// <param name="pFNumbers"></param>
        /// <param name="pOperator"></param>
        /// <param name="pType"></param>
        /// <param name="pFLAG"></param>
        /// <param name="pDescription"></param>
        /// <param name="pIP"></param>
        /// <param name="pBARCODES"></param>
        /// <param name="pERMESSAGE"></param>
        /// <param name="pMOBILLS"></param>
        /// <param name="pMOENTRYID"></param>
        /// <param name="pORDERBILLS"></param>
        /// <param name="pORDERENTRYID"></param>
        /// <param name="pMATERIALID"></param>
        [WebMethod]
        public void SaveLog(string pFNumbers, string pOperator, string pType, int pFLAG, string pDescription, string pIP, string pBARCODES, string pERMESSAGE, string pMOBILLS, string pMOENTRYID, string pORDERBILLS, string pORDERENTRYID, string pMATERIALID)
        {
            FactorySQL.cnCB.SaveLog(pFNumbers, pOperator, pType, pFLAG, pDescription, pIP, pBARCODES, pERMESSAGE, pMOBILLS, pMOENTRYID, pORDERBILLS, pORDERENTRYID, pMATERIALID);
        }
    }
}
