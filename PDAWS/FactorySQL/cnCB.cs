﻿using System;
using System.Data;
using System.Collections;
using System.Configuration;
using Newtonsoft.Json.Linq;
using Kingdee.BOS.WebApi.Client;
using Oracle.ManagedDataAccess.Client;

namespace PDAWS.FactorySQL
{
    static class cnCB
    {
        #region STATIC
        /// <summary>
        /// ERP地址
        /// </summary>
        private static string _URL;
        /// <summary>
        /// K3Cloud帐套ID
        /// </summary>
        private static string _ZTID;
        /// <summary>
        /// K3用户名
        /// </summary>
        private static string _UserName;
        /// <summary>
        /// K3用户密码
        /// </summary>
        private static string _PWD;
        /// <summary>
        /// SQL语句
        /// </summary>
        private static string _SQL;
        private static string _ConnectionString;
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                return _ConnectionString;
            }

            set
            {
                _ConnectionString = value;
            }
        }

        static cnCB()
        {
            _URL = ConfigurationManager.AppSettings["URL"];//http://192.168.3.247:9898/K3Cloud/,http://localhost:9898/k3cloud/
            _ZTID = ConfigurationManager.AppSettings["ZTID"];//5c4671d93bacad,5a4b0e5784c0cf
            _UserName = "Administrator";
            _PWD = "tjb316,";
            //_ConnectionString = "Data Source=" + ConfigurationManager.AppSettings["DB_IP"] + "/orcl;User Id=K30118;Password=Tangmi123";
            _ConnectionString = "Data Source=" + ConfigurationManager.AppSettings["DB_IP"] + "/k3data;User Id=K3ERP0903;Password=Tangmi1234";
        }
        #endregion

        #region 验证登录
        /// <summary>
        /// 验证登录
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        public static bool CheckLogin(string userName, string password)
        {
            bool bReValue = false;
            K3CloudApiClient client = new K3CloudApiClient(_URL);
            try
            {
                bReValue = client.Login(_ZTID, userName, password, 2052);
            }
            catch { }
            return bReValue;
        }
        #endregion

        #region 操作订单
        /// <summary>
        /// 操作订单
        /// </summary>
        /// <param name="pOperation">操作标识</param>
        /// <param name="pJson">Json字符串</param>
        /// <returns></returns>
        public static string OperateBill(string pOperation, string pJson)
        {
            string strReturn = string.Empty, strResult = string.Empty, strFormId, strBillNo;

            strFormId = pJson.Substring(pJson.IndexOf("FormId:") + 7, pJson.IndexOf(";", pJson.IndexOf("FormId:")) - 7);
            strBillNo = "{\"CreateOrgId\":\"0\",\"Numbers\":[\"" + pJson.Substring(pJson.IndexOf("Number:") + 7, pJson.Length - pJson.IndexOf("Number:") - 7) + "\"]}";
            K3CloudApiClient client = new K3CloudApiClient(_URL);
            var bLogin = client.Login(_ZTID, _UserName, _PWD, 2052);
            if (bLogin)
            {
                switch (pOperation)
                {
                    case "Delete":
                        strResult = client.Delete(strFormId, strBillNo);
                        break;
                    case "Audit":
                        strResult = client.Audit(strFormId, strBillNo);
                        break;
                    case "Submit":
                        strResult = client.Submit(strFormId, strBillNo);
                        break;
                    case "UnAudit":
                        strResult = client.UnAudit(strFormId, strBillNo);
                        break;
                }
                JObject jo = JObject.Parse(strResult);
                if (jo["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
                    strReturn += "[ID:" + jo["Result"]["ResponseStatus"]["SuccessEntitys"][0]["Id"].Value<int>().ToString() + ";Number:" + jo["Result"]["ResponseStatus"]["SuccessEntitys"][0]["Number"].Value<string>() + "]";
                else
                {
                    strReturn = string.Empty;
                    for (int i = 0; i < ((IList)jo["Result"]["ResponseStatus"]["Errors"]).Count; i++)
                        strReturn += jo["Result"]["ResponseStatus"]["Errors"][i]["Message"].Value<string>() + "\r\n";//不成功返错误信息
                }
            }
            return strReturn;
        }
        #endregion

        #region 表查询
        /// <summary>
        /// 根据类型获取信息
        /// </summary>
        /// <param name="pString">SQL参数</param>
        /// <param name="pType">类型标识</param>
        /// <returns></returns>
        public static DataTable GetTable(string pString, string pType)
        {
            switch(pType)
            {
                case "BC"://Barcode信息
                    _SQL = @"SELECT BC.ID BARCODEID,BC.BARCODE,BC.INSTOCKSTATUS,BC.PACKAGESTATUS,BC.UTSTOCKSTATUS
                        , NVL(BC.KDINSTOCKID, 0) INSTOCKID,BC.MANUALGEN,NVL(PK.SEALEDFLAG, 0) SEALEDFLAG,NVL(PK.UNIQUENO, ' ') UNIQUENO,A.FBILLNO MOBILLNO
                        , NVL(MTLL.FNAME, ' ') MATERIALNAME,NVL(AE.FQTY, 0) QTY,(SELECT COUNT(1) FROM C##BARCODE2.PM_BARCODE O WHERE O.TASKID = BC.TASKID AND O.INSTOCKSTATUS = 1) FINISHQTY,AA.FSTATUS,B.FID ORDERID
                        , BC.KDORDERFENTRYID ORDERENTRYID,NVL(B.FBILLNO, ' ') ORDERNO,NVL(BE.FSEQ, 1) FSEQ,NVL(B.FCLOSESTATUS, 'A') FCLOSESTATUS,NVL(B.FCUSTID, 0) CUSTID
                        , NVL(CUSTL.FNAME, ' ') CUSTOMERNAME,NVL(B.F_PAEZ_HEADLOCADDRESS, ' ') ADDRESS,CASE WHEN NVL(ASSL.FDATAVALUE, ' ') LIKE '%物流%' THEN 1 ELSE 0 END ISLOGISTICS,NVL(ASSL2.FDATAVALUE, ' ') FDELIVERYMETHOD,DEPL.FNAME FWORKSHOP
                        , NVL(B.F_PAEZ_SINGLESHIPMENT, 0) SINGLESHIPMENT,NVL(SP.FBILLNO, ' ') SHARESHIPMENT
                    FROM C##BARCODE2.PM_BARCODE BC
                    LEFT JOIN T_PRD_MOENTRY AE ON BC.KDTASKFENTRYID = AE.FENTRYID
                    LEFT JOIN T_PRD_MOENTRY_A AA ON AE.FENTRYID = AA.FENTRYID
                    LEFT JOIN T_PRD_MO A ON AE.FID = A.FID
                    LEFT JOIN T_SAL_ORDERENTRY BE ON BC.KDORDERFENTRYID = BE.FENTRYID
                    LEFT JOIN T_SAL_ORDER B ON BE.FID = B.FID
                    LEFT JOIN T_BD_DEPARTMENT DEP ON AE.FWORKSHOPID = DEP.FDEPTID
                    LEFT JOIN T_BD_DEPARTMENT_L DEPL ON DEP.FDEPTID = DEPL.FDEPTID AND DEPL.FLOCALEID = 2052
                    LEFT JOIN T_BD_CUSTOMER_L CUSTL ON B.FCUSTID = CUSTL.FCUSTID AND CUSTL.FLOCALEID = 2052
                    LEFT JOIN T_BD_MATERIAL_L MTLL ON BC.KDMTLID = MTLL.FMATERIALID AND MTLL.FLOCALEID = 2052
                    LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASSL ON ASSL.FENTRYID = B.FHEADDELIVERYWAY AND ASSL.FLOCALEID = 2052
                    LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASSL2 ON ASSL2.FENTRYID = B.FDELIVERYMETHOD AND ASSL2.FLOCALEID = 2052
                    LEFT JOIN C##BARCODE2.PM_SENDPLANENTRY SPE ON B.FID = SPE.ORDERINTERID
                    LEFT JOIN C##BARCODE2.PM_SENDPLAN SP ON SPE.FINTERID = SP.FINTERID
                    LEFT JOIN C##BARCODE2.PM_PRODUCTPACKAGE PK ON BC.PACKAGEID = PK.ID
                    WHERE BC.BARCODE = '" + pString + "'";
                    break;
                case "PK"://装箱信息
                    _SQL = @"SELECT PK.ID PACKAGEID,PK.UNIQUENO,NVL(A.FCUSTID, 0) CUSTID,NVL(CUSTL.FNAME, ' ') CUSTOMERNAME,NVL(A.F_PAEZ_HEADLOCADDRESS, ' ') ADDRESS,PK.SEALEDFLAG,NVL(PK.VOLUME, 0) VOLUME,NVL(TK.FBILLNO, ' ') BILLNO
                        , (SELECT COUNT(1) FROM C##BARCODE2.PM_BARCODE WHERE PACKAGEID = BC.PACKAGEID AND PACKAGESTATUS = 1) PACKAGEQTY,NVL(A.F_PAEZ_SINGLESHIPMENT, 0) SINGLESHIPMENT,NVL(I.FBILLNO, ' ') SHARESHIPMENT
                        , NVL(BC.BARCODE, ' ') BARCODE,NVL(A.FBILLNO, ' ') ORDERNO,NVL(CUSTL.FNAME, ' ') MATERIALNAME,NVL(A.FCLOSESTATUS, 'A') FCLOSESTATUS,NVL(ASSL.FDATAVALUE, ' ') FDELIVERYMETHOD
                    FROM C##BARCODE2.PM_PRODUCTPACKAGE PK
                    LEFT JOIN C##BARCODE2.PM_BarCode BC ON BC.PACKAGEID = PK.ID
                    LEFT JOIN T_SAL_ORDERENTRY AE ON BC.KDORDERFENTRYID = AE.FENTRYID
                    LEFT JOIN T_SAL_ORDER A ON AE.FID = A.FID
                    LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASSL ON ASSL.FENTRYID = A.FDELIVERYMETHOD AND ASSL.FLOCALEID = 2052
                    LEFT JOIN T_BD_CUSTOMER_L CUSTL ON A.FCUSTID = CUSTL.FCUSTID AND CUSTL.FLOCALEID = 2052
                    LEFT JOIN C##BARCODE2.PM_PRODUCETASK TK ON BC.TASKID = TK.ID
                    LEFT JOIN C##BARCODE2.PM_SENDPLANENTRY SP ON A.FID = SP.ORDERINTERID
                    LEFT JOIN C##BARCODE2.PM_SENDPLAN I ON SP.FINTERID = I.FINTERID
                    WHERE PK.UNIQUENO = '" + pString + "'";
                    break;
                case "PKFIX"://装箱维护信息
                    _SQL = @"SELECT PK.ID PACKAGEID,PK.UNIQUENO,PK.SEALEDFLAG,NVL(PK.VOLUME, 0) VOLUME,BC.BARCODE,BC.KDINSTOCKID INSTOCKID
                        , NVL(A.FBILLNO, ' ') BILLNO,NVL(CUSTL.FNAME, ' ') CUSTOMERNAME,NVL(MTLL.FNAME, ' ') MATERIALNAME,NVL(A.FCUSTID, 0) FCUSTID
                        , PK.UTSTATUS,NVL(A.FCLOSESTATUS, 'A') FCLOSESTATUS
                    FROM  C##BARCODE2.PM_PRODUCTPACKAGE PK
                    INNER JOIN C##BARCODE2.PM_BarCode BC ON BC.PACKAGEID = PK.ID
                    LEFT JOIN T_SAL_ORDERENTRY AE ON BC.KDORDERFENTRYID = AE.FENTRYID
                    LEFT JOIN T_SAL_ORDER A ON AE.FID = A.FID
                    LEFT JOIN T_BD_CUSTOMER_L CUSTL ON A.FCUSTID = CUSTL.FCUSTID AND CUSTL.FLOCALEID = 2052
                    LEFT JOIN T_BD_MATERIAL_L MTLL ON BC.KDMTLID = MTLL.FMATERIALID AND MTLL.FLOCALEID = 2052
                    WHERE PK.UNIQUENO = '" + pString + "'";
                    break;
                case "INVENTORY"://仓库列表
                    _SQL = @"SELECT 'ID:' || A.FSTOCKID || ';Number:' || A.FNUMBER FNUMBER,B.FNAME
                    FROM T_BD_STOCK A
                    INNER JOIN T_BD_STOCK_L B ON A.FSTOCKID = B.FSTOCKID AND B.FLOCALEID = 2052
                    WHERE A.FUSEORGID = " + pString + @"
                    ORDER BY A.FNUMBER";
                    break;
                case "SALOUTSTOCK"://销售出库信息
                    _SQL = @"SELECT PK.UNIQUENO,BC.BARCODE, NVL(A.FBILLNO, ' ') FBILLNO,NVL(A.FCUSTID, 0) KDCUSTID,NVL(CUST.FNUMBER, ' ') CUSTID
                        , NVL(CUSTL.FNAME, ' ') CUSTNAME,NVL(MTLL.FNAME, ' ') MATERIALNAME,NVL(A.F_PAEZ_HEADLOCADDRESS, ' ') FADDRESS,NVL(PK.VOLUME, 0) VOLUME,PK.SEALEDFLAG
                        , NVL(A.F_PAEZ_SINGLESHIPMENT, 0) SINGLESHIPMENT,NVL(SP.FBILLNO, ' ') SHARESHIPMENT,BC.PACKAGESTATUS, BC.INSTOCKSTATUS,BC.UTSTOCKSTATUS, B.FBILLNO MOBILLNO,NVL(BE.FSEQ, 0) FSEQ
                        , NVL(A.FCLOSESTATUS, 'A') FCLOSESTATUS,NVL(ASSL.FDATAVALUE, ' ') 商品名,NVL(ASSL2.FDATAVALUE,' ') 颜色,NVL(BTL.FNAME, ' ') 销售订单类型
                    FROM C##BARCODE2.PM_PRODUCTPACKAGE PK
                    INNER JOIN C##BARCODE2.PM_BARCODE BC ON PK.ID = BC.PACKAGEID
                    LEFT JOIN T_SAL_ORDERENTRY AE ON BC.KDORDERFENTRYID = AE.FENTRYID
                    LEFT JOIN T_SAL_ORDER A ON AE.FID = A.FID
                    LEFT JOIN T_PRD_MOENTRY BE ON BC.KDTASKFENTRYID = BE.FENTRYID
                    LEFT JOIN T_PRD_MO B ON BE.FID = B.FID
                    LEFT JOIN T_BD_MATERIAL MTL ON AE.FMATERIALID = MTL.FMATERIALID
                    LEFT JOIN T_BD_MATERIAL_L MTLL ON MTL.FMATERIALID = MTLL.FMATERIALID AND MTLL.FLOCALEID = 2052
                    LEFT JOIN T_BD_CUSTOMER CUST ON A.FCUSTID = CUST.FCUSTID
                    LEFT JOIN T_BD_CUSTOMER_L CUSTL ON CUST.FCUSTID = CUSTL.FCUSTID AND CUSTL.FLOCALEID = 2052
                    LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASSL ON MTL.F_PAEZ_TRADE = ASSL.FENTRYID AND ASSL.FLOCALEID = 2052
                    LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASSL2 ON MTL.F_PAEZ_COLOR = ASSL2.FENTRYID AND ASSL2.FLOCALEID = 2052
                    LEFT JOIN T_BAS_BILLTYPE_L BTL ON A.FBILLTYPEID = BTL.FBILLTYPEID AND BTL.FLOCALEID = 2052
                    LEFT JOIN C##BARCODE2.PM_SENDPLANENTRY SPE ON A.FID = SPE.ORDERINTERID
                    LEFT JOIN C##BARCODE2.PM_SENDPLAN SP ON SPE.FINTERID = SP.FINTERID
                    WHERE PK.UNIQUENO = '" + pString + "'";
                    break;
                case "ZD"://同一销售订单的所有已封箱未出库装箱单
                    _SQL = @"SELECT NVL(D.UNIQUENO, ' ') UNIQUENO, NVL(D.SEALEDFLAG, 0) SEALEDFLAG, NVL(C.UTSTOCKSTATUS, 0) UTSTOCKSTATUS
                    FROM T_SAL_ORDER A
                    INNER JOIN T_SAL_ORDERENTRY B ON A.FID = B.FID
                    INNER JOIN C##BARCODE2.PM_BARCODE C ON B.FENTRYID = C.KDORDERFENTRYID
                    INNER JOIN C##BARCODE2.PM_PRODUCTPACKAGE D ON C.PACKAGEID = D.ID
                    WHERE D.SEALEDFLAG = 1 AND C.UTSTOCKSTATUS = 0 AND A.FBILLNO = '" + pString + @"'
                    GROUP BY D.UNIQUENO, D.SEALEDFLAG, C.UTSTOCKSTATUS";
                    break;
                case "ZDBC"://同一销售订单的所有产品条码
                    _SQL = @"SELECT A.FBILLNO 订单号,NVL(C.BARCODE, ' ') BARCODE,CASE WHEN NVL(C.INSTOCKSTATUS, 0) = 0 THEN '否' ELSE '是' END 入库状态,CASE WHEN NVL(C.UTSTOCKSTATUS, 0) = 0 THEN '否' ELSE '是' END 出库状态,CASE WHEN NVL(C.PACKAGESTATUS, 0) = 0 THEN '否' ELSE '是' END 装箱状态
                        , (SELECT SUM(D.FQTY) FROM T_SAL_ORDERENTRY D INNER JOIN T_SAL_ORDER E ON D.FID = E.FID INNER JOIN T_BD_MATERIAL F ON D.FMATERIALID = F.FMATERIALID AND F.FNUMBER LIKE '3%' WHERE E.FBILLNO = A.FBILLNO) ALLQTY
                        , NVL(ASL.FDATAVALUE, ' ') 商品名,NVL(ASL2.FDATAVALUE,' ') 颜色
                    FROM T_SAL_ORDER A
                    INNER JOIN T_SAL_ORDERENTRY B ON A.FID = B.FID
                    INNER JOIN C##BARCODE2.PM_BARCODE C ON B.FENTRYID = C.KDORDERFENTRYID
                    LEFT JOIN T_BD_MATERIAL MTL ON B.FMATERIALID = MTL.FMATERIALID
                    LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASL ON MTL.F_PAEZ_TRADE = ASL.FENTRYID AND ASL.FLOCALEID = 2052
                    LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASL2 ON MTL.F_PAEZ_COLOR = ASL2.FENTRYID AND ASL2.FLOCALEID = 2052
                    WHERE A.FBILLNO = '" + pString + @"'";
                    break;
                case "PD"://同一拼单的所有已封箱未出库装箱单
                    _SQL = @"SELECT A.UNIQUENO,A.SEALEDFLAG,NVL(B.UTSTOCKSTATUS, 0) UTSTOCKSTATUS
                    FROM C##BARCODE2.PM_PRODUCTPACKAGE A
                    INNER JOIN C##BARCODE2.PM_BARCODE B ON B.PACKAGEID = A.ID
                    INNER JOIN T_SAL_ORDERENTRY C ON C.FENTRYID = B.KDORDERFENTRYID
                    INNER JOIN T_SAL_ORDER D ON C.FID = D.FID
                    LEFT JOIN C##BARCODE2.PM_SENDPLANENTRY E ON D.FID = E.ORDERINTERID
                    LEFT JOIN C##BARCODE2.PM_SENDPLAN F ON E.FINTERID = F.FINTERID
                    WHERE A.SEALEDFLAG = 1 AND B.UTSTOCKSTATUS = 0 AND F.FBILLNO = '" + pString + @"'
                    GROUP BY A.UNIQUENO, A.SEALEDFLAG, B.UTSTOCKSTATUS";
                    break;
                case "PDBC"://同一拼单的所有产品条码
                    _SQL = @"SELECT A.订单号,NVL(E.BARCODE, ' ') BARCODE,CASE WHEN NVL(E.INSTOCKSTATUS, 0) = 0 THEN '否' ELSE '是' END 入库状态,CASE WHEN NVL(E.UTSTOCKSTATUS, 0) = 0 THEN '否' ELSE '是' END 出库状态,CASE WHEN NVL(E.PACKAGESTATUS, 0) = 0 THEN '否' ELSE '是' END 装箱状态
                        , NVL(ASL.FDATAVALUE, ' ') 商品名,NVL(ASL2.FDATAVALUE,' ') 颜色
                    FROM C##BARCODE2.PM_SENDPLAN A
                    INNER JOIN C##BARCODE2.PM_SENDPLANENTRY B ON A.FINTERID = B.FINTERID
                    INNER JOIN T_SAL_ORDER C ON B.ORDERINTERID = C.FID
                    INNER JOIN T_SAL_ORDERENTRY D ON C.FID = D.FID
                    INNER JOIN C##BARCODE2.PM_BARCODE E ON D.FENTRYID = E.KDORDERFENTRYID
                    LEFT JOIN T_BD_MATERIAL MTL ON D.FMATERIALID = MTL.FMATERIALID
                    LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASL ON MTL.F_PAEZ_TRADE = ASL.FENTRYID AND ASL.FLOCALEID = 2052
                    LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASL2 ON MTL.F_PAEZ_COLOR = ASL2.FENTRYID AND ASL2.FLOCALEID = 2052
                    WHERE A.FBILLNO = '" + pString + @"'";
                    break;
                case "OT"://非整非拼单同一客户的所有已入库未出库装箱单
                    {
                        string FCustid = string.Empty;
                        string FAddress = string.Empty;
                        FCustid = pString.Substring(0, pString.IndexOf("|"));
                        FAddress = pString.Substring(pString.IndexOf("|") + 1, pString.Length - pString.IndexOf("|") - 1);
                        _SQL = @"SELECT DISTINCT A.UNIQUENO, A.SEALEDFLAG,NVL(B.UTSTOCKSTATUS, 0) UTSTOCKSTATUS
                        FROM C##BARCODE2.PM_PRODUCTPACKAGE A
                        INNER JOIN C##BARCODE2.PM_BARCODE B ON B.PACKAGEID = A.ID
                        INNER JOIN T_SAL_ORDERENTRY CE ON CE.FENTRYID = B.KDORDERFENTRYID
                        INNER JOIN T_SAL_ORDER C ON CE.FID = C.FID
                        LEFT JOIN C##BARCODE2.PM_SENDPLANENTRY DE ON C.FID = DE.ORDERINTERID
                        LEFT JOIN C##BARCODE2.PM_SENDPLAN D ON DE.FINTERID = D.FINTERID
                        WHERE B.INSTOCKSTATUS = 1 AND B.UTSTOCKSTATUS = 0 AND C.F_PAEZ_SINGLESHIPMENT = 0 AND (D.FBILLNO IS NULL OR D.FBILLNO = ' ') AND C.FCUSTID = " + FCustid + " AND C.F_PAEZ_HEADLOCADDRESS = '" + FAddress + "'";
                    }
                    break;
                case "OTBC"://非整非拼单同一客户的所有已入库未出库产品条码
                    {
                        string FCustid = string.Empty;
                        string FAddress = string.Empty;
                        FCustid = pString.Substring(0, pString.IndexOf("|"));
                        FAddress = pString.Substring(pString.IndexOf("|") + 1, pString.Length - pString.IndexOf("|") - 1);
                        _SQL = @"SELECT A.FBILLNO 订单号,NVL(B.BARCODE, ' ') BARCODE,'是' 入库状态,'否' 出库状态,CASE WHEN NVL(B.PACKAGESTATUS, 0) = 0 THEN '否' ELSE '是' END 装箱状态,NVL(ASL.FDATAVALUE, ' ') 商品名,NVL(ASL2.FDATAVALUE,' ') 颜色
                        FROM T_SAL_ORDER A
                        INNER JOIN T_SAL_ORDERENTRY AE ON A.FID = AE.FID
                        INNER JOIN T_SAL_ORDERENTRY_R AR ON AE.FENTRYID = AR.FENTRYID
                        INNER JOIN C##BARCODE2.PM_BARCODE B ON AE.FENTRYID = B.KDORDERFENTRYID
                        LEFT JOIN C##BARCODE2.PM_SENDPLANENTRY CE ON A.FID = CE.ORDERINTERID
                        LEFT JOIN C##BARCODE2.PM_SENDPLAN C ON CE.FINTERID = C.FINTERID
                        LEFT JOIN T_BD_MATERIAL MTL ON AE.FMATERIALID = MTL.FMATERIALID
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASL ON MTL.F_PAEZ_TRADE = ASL.FENTRYID AND ASL.FLOCALEID = 2052
                        LEFT JOIN T_BAS_ASSISTANTDATAENTRY_L ASL2 ON MTL.F_PAEZ_COLOR = ASL2.FENTRYID AND ASL2.FLOCALEID = 2052
                        WHERE B.INSTOCKSTATUS = 1 AND B.UTSTOCKSTATUS = 0 AND A.F_PAEZ_SINGLESHIPMENT = 0 AND AR.FCANOUTQTY > 0 AND AE.FMRPCLOSESTATUS = 'A' AND (C.FBILLNO IS NULL OR C.FBILLNO = ' ') AND A.FCUSTID = " + FCustid + " AND A.F_PAEZ_HEADLOCADDRESS = '" + FAddress + "'";
                    }
                    break;
                case "EXPRESS"://运输单信息
                    _SQL = @"SELECT DISTINCT '" + pString + @"' 当前运单,A.FCARRIAGENO 所有运单,LENGTH(A.FCARRIAGENO) - LENGTH(REPLACE(A.FCARRIAGENO, '/','')) + 1 数量,A.FBILLNO 出库单
                        , NVL(A.FAPPROVEDATE,TO_DATE('9999-12-31 23:59:59', 'YYYY-MM-DD HH24:MI:SS')) 审核日期,A.FDOCUMENTSTATUS 数据状态, NVL(B.FCLOSESTATUS,'B') 订单关闭, NVL(CL.FNAME, ' ') 客户
                    FROM T_SAL_OUTSTOCK A
                    INNER JOIN T_SAL_OUTSTOCKENTRY_R AR ON A.FID = AR.FID
                    LEFT JOIN T_SAL_ORDER B ON AR.FSRCBILLNO = B.FBILLNO
                    LEFT JOIN T_BD_CUSTOMER_L CL ON A.FCUSTOMERID = CL.FCUSTID AND CL.FLOCALEID = 2052
                    WHERE INSTR(A.FCARRIAGENO,'" + pString + "') > 0";
                    break;
                case "BCLIST"://根据客户分组条码
                    _SQL = @"SELECT WM_CONCAT(TO_CHAR(B.BARCODE)) BCLIST
                    FROM C##BARCODE2.PM_PRODUCTPACKAGE A
                    INNER JOIN C##BARCODE2.PM_BARCODE B ON A.ID = B.PACKAGEID AND B.INSTOCKSTATUS = 1 AND B.PACKAGESTATUS = 1 AND B.UTSTOCKSTATUS = 0
                    INNER JOIN T_SAL_ORDERENTRY C ON B.KDORDERFENTRYID = C.FENTRYID
                    INNER JOIN T_SAL_ORDER D ON C.FID = D.FID
                    INNER JOIN T_BD_CUSTOMER E ON D.FCUSTID = E.FCUSTID
                    WHERE A.UNIQUENO IN(" + pString + @") AND A.SEALEDFLAG = 1
                    GROUP BY E.FNUMBER";
                    break;
                default:
                    _SQL = string.Empty;
                    break;
            }

            DataTable dt = new DataTable();
            if (!_SQL.Equals(string.Empty))
                dt = ORAHelper.ExecuteTable(_SQL);

            dt.TableName = "TableInfo";
            return dt;
        }
        #endregion 

        #region 标量查询
        /// <summary>
        /// 标量查询
        /// </summary>
        /// <param name="pString">SQL参数</param>
        /// <param name="pType">类型标识</param>
        /// <param name="pIndex">序号</param>
        /// <returns></returns>
        public static object ExecuteScalar(string pString, string pType, int pIndex)
        {
            if (pType == "MAXBN")//获取箱号最大值
            {
                switch (pIndex)
                {
                    case 1://整单
                        _SQL = @"SELECT NVL(MAX(NVL(A.BOXNUMBER, 0)), 0) MAXBOXNUMBER
                        FROM C##BARCODE2.PM_PRODUCTPACKAGE A
                        INNER JOIN C##BARCODE2.PM_BARCODE B ON A.ID = B.PACKAGEID
                        INNER JOIN T_SAL_ORDERENTRY CE ON B.KDORDERFENTRYID = CE.FENTRYID
                        INNER JOIN T_SAL_ORDER C ON C.FID = CE.FID
                        WHERE C.FBILLNO = '" + pString + "'";
                        break;
                    case 2://拼单
                        _SQL = @"SELECT NVL(MAX(NVL(A.BOXNUMBER, 0)), 0) MAXBOXNUMBER
                        FROM C##BARCODE2.PM_PRODUCTPACKAGE A
                        INNER JOIN C##BARCODE2.PM_BARCODE B ON A.ID = B.PACKAGEID
                        INNER JOIN T_SAL_ORDERENTRY CE ON B.KDORDERFENTRYID = CE.FENTRYID
                        INNER JOIN T_SAL_ORDER C ON C.FID = CE.FID
                        LEFT JOIN C##BARCODE2.PM_SENDPLANENTRY DE ON C.FID = DE.ORDERINTERID
                        LEFT JOIN C##BARCODE2.PM_SENDPLAN D ON DE.FINTERID = D.FINTERID
                        WHERE D.FBILLNO = '" + pString + "'";
                        break;
                    case 3://非整非拼
                        _SQL = @"SELECT NVL(MAX(NVL(A.BOXNUMBER, 0)), 0) MAXBOXNUMBER
                        FROM C##BARCODE2.PM_PRODUCTPACKAGE A
                        INNER JOIN C##BARCODE2.PM_BARCODE B ON A.ID = B.PACKAGEID
                        INNER JOIN T_SAL_ORDERENTRY CE ON B.KDORDERFENTRYID = CE.FENTRYID
                        INNER JOIN T_SAL_ORDERENTRY_R CR ON CE.FENTRYID = CR.FENTRYID
                        INNER JOIN T_SAL_ORDER C ON C.FID = CE.FID
                        LEFT JOIN C##BARCODE2.PM_SENDPLANENTRY DE ON C.FID = DE.ORDERINTERID
                        LEFT JOIN C##BARCODE2.PM_SENDPLAN D ON DE.FINTERID = D.FINTERID
                        WHERE B.INSTOCKSTATUS = 1   AND C.F_PAEZ_SINGLESHIPMENT = 0 AND (D.FBILLNO IS NULL OR D.FBILLNO = ' ') AND CR.FCANOUTQTY > 0 AND C.FCUSTID = " + pString;
                        break;
                    default:
                        _SQL = string.Empty;
                        break;
                }
            }
            else if (pType == "OBJECT")
            {
                switch (pIndex)
                {
                    case 1://求PDA日志中指定运单号的记录数
                        _SQL = @"SELECT COUNT(*) FROM DM_EXCEPTIONRECORD WHERE FNUMBER = '" + pString + "'";
                        break;
                    default:
                        _SQL = string.Empty;
                        break;
                }
            }

            if (!_SQL.Equals(string.Empty))
                return ORAHelper.ExecuteScalar(_SQL);
            return null;
        }
        #endregion

        #region 生产入库
        /// <summary>
        /// 生产入库
        /// </summary>
        /// <param name="pBarcode">条码组</param>
        /// <param name="pInventory">仓库代码</param>
        /// <returns></returns>
        public static string InStock(string pBarcode, string pInventory)
        {
            //string strInstockBillNo = string.Empty;
            //DataTable dt = null;
            //K3CloudApiClient client = null;
            //OracleConnection OrlConn = new OracleConnection(ConnectionString);

            //if (pBarcode.Trim().Length == 0) return "条码错误";
            ////根据Barcode在数据库找到对应的生产订单信息
            //string strBarcodes = string.Empty;
            //while (pBarcode.IndexOf(",") > 0)
            //{
            //    strBarcodes += "'" + pBarcode.Substring(0, pBarcode.IndexOf(",")) + "',";
            //    pBarcode = pBarcode.Substring(pBarcode.IndexOf(",") + 1, pBarcode.Length - pBarcode.IndexOf(",") - 1);
            //}
            //strBarcodes += "'" + pBarcode + "'";

            //string strSQL = @"SELECT BC.BARCODE,ORG.FNUMBER FPRDORGID,NVL(ORG2.FNUMBER, ORG.FNUMBER) FOWNERID,NVL(ORG3.FNUMBER, ORG.FNUMBER) FSTOCKINORGID
            //    , B.FID, BE.FENTRYID, B.FBILLNO MOBILLNO, MTL.FNUMBER MATERIALID, BE.FPRODUCTTYPE, UNT.FNUMBER FUNITID
            //    , BE.FCOSTRATE, DEP.FNUMBER FWORKSHOPID, BQ.FNOSTOCKINQTY FQTY, COUNT(1) FREALQTY
            //FROM C##BARCODE2.PM_BarCode BC
            //INNER JOIN T_PRD_MOENTRY BE ON BC.KDTASKFENTRYID = BE.FENTRYID
            //INNER JOIN T_PRD_MOENTRY_A BA ON BE.FENTRYID = BA.FENTRYID
            //INNER JOIN T_PRD_MOENTRY_Q BQ ON BA.FENTRYID = BQ.FENTRYID
            //INNER JOIN T_PRD_MO B ON BE.FID = B.FID                     
            //INNER JOIN T_BD_MATERIAL MTL ON BC.KDMTLID = MTL.FMATERIALID
            //INNER JOIN T_BD_UNIT UNT ON BE.FUNITID = UNT.FUNITID
            //INNER JOIN T_BD_DEPARTMENT DEP ON BE.FWORKSHOPID = DEP.FDEPTID
            //LEFT JOIN T_ORG_ORGANIZATIONS ORG ON B.FPRDORGID = ORG.FORGID
            //LEFT JOIN T_ORG_ORGANIZATIONS ORG2 ON BA.FINSTOCKOWNERID = ORG2.FORGID
            //LEFT JOIN T_ORG_ORGANIZATIONS ORG3 ON BE.FSTOCKINORGID = ORG3.FORGID
            //WHERE BC.BARCODE IN(" + strBarcodes + @") AND BC.INSTOCKSTATUS = 0
            //GROUP BY BC.BARCODE, ORG.FNUMBER, ORG2.FNUMBER, ORG3.FNUMBER, B.FID, BE.FENTRYID, B.FBILLNO, MTL.FNUMBER, BE.FPRODUCTTYPE, UNT.FNUMBER, BE.FCOSTRATE, DEP.FNUMBER, BQ.FNOSTOCKINQTY";

            //dt = new DataTable();
            //try
            //{
            //    OrlConn.Open();
            //    OracleCommand cmd = OrlConn.CreateCommand();
            //    cmd.CommandText = strSQL;
            //    OracleDataAdapter adp = new OracleDataAdapter(cmd.CommandText, OrlConn);
            //    adp.Fill(dt);
            //}
            //catch (Exception ex)
            //{
            //    return "查询数据库错误:" + ex.Message;
            //}
            //finally
            //{
            //    OrlConn.Close();
            //}
            //if (dt.Rows.Count < 1) return "在数据库找不到数据";//扫描Barcode后在数据库找不到数据

            //client = new K3CloudApiClient(_URL);
            //var bLogin = client.Login(_ZTID, _UserName, _PWD, 2052);
            //if (bLogin)
            //{
            //    // 开始构建Web API参数对象
            //    // 参数根对象：包含Creator、NeedUpDateFields、Model这三个子参数
            //    // using Newtonsoft.Json.Linq;(需引用Newtonsoft.Json.dll)
            //    JObject jsonRoot = new JObject();

            //    // Creator: 创建用户
            //    jsonRoot.Add("Creator", "PDA");

            //    // NeedUpDateFields: 哪些字段需要更新？为空则表示参数中全部字段，均需要更新
            //    jsonRoot.Add("NeedUpDateFields", new JArray(""));

            //    // Model: 单据详细数据参数
            //    JObject model = new JObject();
            //    jsonRoot.Add("Model", model);

            //    // 单据主键：必须填写，系统据此判断是新增还是修改单据；新增单据，填0
            //    model.Add("FID", 0);

            //    // 采购组织：必须填写，是基础资料字段
            //    JObject basedata = new JObject();
            //    basedata.Add("FNumber", dt.Rows[0]["FSTOCKINORGID"].ToString());
            //    model.Add("FStockOrgId", basedata);

            //    basedata = new JObject();
            //    basedata.Add("FNumber", dt.Rows[0]["FPRDORGID"].ToString());
            //    model.Add("FPrdOrgId", basedata);

            //    basedata = new JObject();
            //    basedata.Add("FNumber", dt.Rows[0]["FOWNERID"].ToString());
            //    model.Add("FOwnerId0", basedata);

            //    basedata = new JObject();
            //    basedata.Add("FNumber", "SCRKD02_SYS");
            //    model.Add("FBillType", basedata);

            //    //采购日期（FDate）
            //    model.Add("FDate", DateTime.Today);

            //    basedata = new JObject();
            //    basedata.Add("FNumber", "PRE001");
            //    model.Add("FCurrId", basedata);

            //    // 开始构建单据体参数：集合参数JArray
            //    JArray entryRows = new JArray();
            //    // 把单据体行集合，添加到model中，以单据体Key为标识
            //    string entityKey = "FEntity";
            //    model.Add(entityKey, entryRows);

            //    // 通过循环创建单据体行：示例代码仅创建一行
            //    for (int i = 0; i < dt.Rows.Count; i++)
            //    {
            //        // 添加新行，把新行加入到单据体行集合
            //        JObject entryRow = new JObject();
            //        entryRows.Add(entryRow);

            //        // 单据体主键：必须填写，系统据此判断是新增还是修改行
            //        entryRow.Add("FEntryID", 0);

            //        //物料(FMaterialId)：基础资料，填写编码
            //        basedata = new JObject();
            //        basedata.Add("FNumber", dt.Rows[i]["MATERIALID"].ToString());
            //        entryRow.Add("FMaterialId", basedata);

            //        entryRow.Add("FProductType", dt.Rows[i]["FPRODUCTTYPE"].ToString());

            //        basedata = new JObject();
            //        basedata.Add("FNumber", dt.Rows[i]["FUNITID"].ToString());
            //        entryRow.Add("FBaseUnitId", basedata);

            //        basedata = new JObject();
            //        basedata.Add("FNumber", dt.Rows[i]["FUNITID"].ToString());
            //        entryRow.Add("FUnitId", basedata);

            //        entryRow.Add("FMustQty", dt.Rows[i]["FQTY"].ToString());
            //        entryRow.Add("FRealQty", dt.Rows[i]["FREALQTY"].ToString());
            //        entryRow.Add("FCostRate", dt.Rows[i]["FCOSTRATE"].ToString());
            //        entryRow.Add("FBaseMustQty", dt.Rows[i]["FQTY"].ToString());
            //        entryRow.Add("FBaseRealQty", dt.Rows[i]["FREALQTY"].ToString());
            //        entryRow.Add("FBasePRDRealLQTY", dt.Rows[i]["FREALQTY"].ToString());
            //        entryRow.Add("FOwnerTypeId", "BD_OwnerOrg");

            //        entryRow.Add("FMoBillNo", dt.Rows[i]["MOBILLNO"].ToString());
            //        entryRow.Add("FMoId", dt.Rows[i]["FID"].ToString());
            //        entryRow.Add("FMoEntryId", dt.Rows[i]["FENTRYID"].ToString());
            //        if (decimal.Parse(dt.Rows[i]["FQTY"].ToString()) == decimal.Parse(dt.Rows[i]["FREALQTY"].ToString())) entryRow.Add("FISFINISHED", 1);
            //        entryRow.Add("FISBACKFLUSH", 1);

            //        basedata = new JObject();
            //        basedata.Add("FNumber", dt.Rows[i]["FOWNERID"].ToString());
            //        entryRow.Add("FOwnerId", basedata);

            //        basedata = new JObject();
            //        basedata.Add("FNumber", pInventory);
            //        entryRow.Add("FStockId", basedata);

            //        basedata = new JObject();
            //        basedata.Add("FNumber", dt.Rows[i]["FWORKSHOPID"].ToString());
            //        entryRow.Add("FWorkShopId1", basedata);

            //        // 创建与源单之间的关联关系，以支持上查与反写源单
            //        // 本示例演示创建与采购申请单之间的关联关系
            //        // 源单类型、源单编号
            //        entryRow.Add("FSrcBillTypeId", "PRD_MO");
            //        entryRow.Add("FSrcBillNo", dt.Rows[i]["MOBILLNO"].ToString());

            //        // 创建Link行集合
            //        JArray linkRows = new JArray();

            //        // 添加到单据体行中：Link子单据体标识 = 关联主单据体标识(POOrderEntry) + _Link
            //        string linkEntityKey = string.Format("{0}_Link", entityKey);
            //        entryRow.Add(linkEntityKey, linkRows);

            //        // 创建Link行：
            //        // 如有多条源单行，则分别创建Link行记录各条源单行信息
            //        JObject linkRow = new JObject();
            //        linkRows.Add(linkRow);

            //        // 填写Link行上的字段值
            //        // 特别说明：Link子单据体上字段的标识，必须在前面增加子单据体标识

            //        // FFlowId : 业务流程图，可选
            //        string fldFlowIdKey = string.Format("{0}_FFlowId", linkEntityKey);
            //        linkRow.Add(fldFlowIdKey, "");

            //        // FFlowLineId ：业务流程图路线，可选
            //        string fldFlowLineIdKey = string.Format("{0}_FFlowLineId", linkEntityKey);
            //        linkRow.Add(fldFlowLineIdKey, "");

            //        // FRuleId ：两单之间的转换规则内码，必填
            //        // 可以通过如下SQL语句到数据库获取
            //        // select FID, *
            //        // from T_META_CONVERTRULE 
            //        // where FSOURCEFORMID = 'PUR_Requisition' 
            //        // and FTARGETFORMID = 'PUR_PurchaseOrder' 
            //        // and FDEVTYPE = 0;
            //        string fldRuleIdKey = string.Format("{0}_FRuleId", linkEntityKey);
            //        linkRow.Add(fldRuleIdKey, "PRD_MO-PRD_INSTOCK");

            //        // FSTableName ：必填，源单单据体表格编码，通过如下语句获取：
            //        // SELECT FTableNumber 
            //        // FROM t_bf_tabledefine 
            //        // WHERE fformid = 'PUR_Requisition' 
            //        // AND fentitykey = 'FEntity'
            //        // 如果如上语句未返回结果，请到K/3 Cloud中，手工选单一次，后台会自动产生表格编码
            //        string fldSTableNameKey = string.Format("{0}_FSTableName", linkEntityKey);
            //        linkRow.Add(fldSTableNameKey, "T_PRD_MOENTRY");

            //        // FSBillId ：必填，源单单据内码
            //        string fldSBillIdKey = string.Format("{0}_FSBillId", linkEntityKey);
            //        linkRow.Add(fldSBillIdKey, int.Parse(dt.Rows[i]["FID"].ToString()));

            //        // FSId : 必填，源单单据体行内码。如果源单主关联实体是单据头，则此属性也填写源单单据内码
            //        string fldSIdKey = string.Format("{0}_FSId", linkEntityKey);
            //        linkRow.Add(fldSIdKey, int.Parse(dt.Rows[i]["FENTRYID"].ToString()));

            //        // FEntity_Link_FBaseQtyOld ：数量原始携带值，下推时，从源单带了多少下来
            //        // 在合并下推时，系统会把多个源行的数量，合并后填写在单据体数量字段上的；
            //        // 合并后对各源单行的反写，需要使用合并前的数量进行反写，也就是本字段上记录的数量
            //        // 因此，如果有合并下推，本字段就必须填写，否则反写不准确
            //        // 如果没有合并下推，单据体数量会直接反写到唯一的源行上，本字段就不需填写
            //        string fldBaseQtyOldKey = string.Format("{0}_FBaseUnitQtyOld", linkEntityKey);
            //        linkRow.Add(fldBaseQtyOldKey, decimal.Parse(dt.Rows[i]["FQTY"].ToString()));

            //        // FEntity_Link_FBaseQty ：数量实际携带值，下推后，用户可以手工修改数量值；此字段存储最终的数量值
            //        // 可选字段：
            //        // 在保存时，系统会自动把单据体上数量值，更新到此字段；因此，这个字段可以不用填写（即使填写了，也会被覆盖）
            //        string fldBaseQtyKey = string.Format("{0}_FBaseUnitQty", linkEntityKey);
            //        linkRow.Add(fldBaseQtyKey, decimal.Parse(dt.Rows[i]["FREALQTY"].ToString()));

            //    }
            //    // 调用Web API接口服务，保存采购订单
            //    strInstockBillNo = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Save", new object[] { "PRD_INSTOCK", jsonRoot.ToString() });
            //    JObject jo = JObject.Parse(strInstockBillNo);

            //    if (!jo["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
            //    {
            //        strInstockBillNo = string.Empty;
            //        for (int i = 0; i < ((IList)jo["Result"]["ResponseStatus"]["Errors"]).Count; i++)
            //            strInstockBillNo += "[" + dt.Rows[i]["BARCODE"].ToString() + "]" + jo["Result"]["ResponseStatus"]["Errors"][i]["Message"].Value<string>() + "\r\n";//保存不成功返错误信息
            //    }
            //    else
            //    {
            //        strInstockBillNo = "ID:" + jo["Result"]["Id"].Value<string>() + ";Number:" + jo["Result"]["Number"].Value<string>();//保存成功返回入库单FID和单据编号FBILLNO

            //        client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Submit", new object[] { "PRD_INSTOCK", "{\"CreateOrgId\":\"0\",\"Numbers\":[\"" + jo["Result"]["Number"].Value<string>() + "\"]}" });//根据入库单号提交单据
            //        client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Audit", new object[] { "PRD_INSTOCK", "{\"CreateOrgId\":\"0\",\"Numbers\":[\"" + jo["Result"]["Number"].Value<string>() + "\"]}" });//根据入库单号审核单据
            //    }
            //}
            //else return "ERP对接失败";
            //return strInstockBillNo;//返回格式:ID:xxxx;Number:xxxx

            //20190330
            string strInstockBillNo = string.Empty;
            DataTable dt;
            K3CloudApiClient client;

            if (pBarcode.Trim().Length == 0) return "条码错误";
            //根据Barcode在数据库找到对应的生产订单信息
            string strBarcodes = string.Empty;
            while (pBarcode.IndexOf(",") > 0)
            {
                strBarcodes += "'" + pBarcode.Substring(0, pBarcode.IndexOf(",")) + "',";
                pBarcode = pBarcode.Substring(pBarcode.IndexOf(",") + 1, pBarcode.Length - pBarcode.IndexOf(",") - 1);
            }
            strBarcodes += "'" + pBarcode + "'";

            _SQL = @"SELECT BC.BARCODE,ORG.FNUMBER FPRDORGID,NVL(ORG2.FNUMBER, ORG.FNUMBER) FOWNERID,NVL(ORG3.FNUMBER, ORG.FNUMBER) FSTOCKINORGID
                , B.FID, BE.FENTRYID, B.FBILLNO MOBILLNO, MTL.FNUMBER MATERIALID, BE.FPRODUCTTYPE, UNT.FNUMBER FUNITID
                , BE.FCOSTRATE, DEP.FNUMBER FWORKSHOPID, BQ.FNOSTOCKINQTY FQTY, COUNT(1) FREALQTY
            FROM C##BARCODE2.PM_BarCode BC
            INNER JOIN T_PRD_MOENTRY BE ON BC.KDTASKFENTRYID = BE.FENTRYID
            INNER JOIN T_PRD_MOENTRY_A BA ON BE.FENTRYID = BA.FENTRYID
            INNER JOIN T_PRD_MOENTRY_Q BQ ON BA.FENTRYID = BQ.FENTRYID
            INNER JOIN T_PRD_MO B ON BE.FID = B.FID                     
            INNER JOIN T_BD_MATERIAL MTL ON BC.KDMTLID = MTL.FMATERIALID
            INNER JOIN T_BD_UNIT UNT ON BE.FUNITID = UNT.FUNITID
            INNER JOIN T_BD_DEPARTMENT DEP ON BE.FWORKSHOPID = DEP.FDEPTID
            LEFT JOIN T_ORG_ORGANIZATIONS ORG ON B.FPRDORGID = ORG.FORGID
            LEFT JOIN T_ORG_ORGANIZATIONS ORG2 ON BA.FINSTOCKOWNERID = ORG2.FORGID
            LEFT JOIN T_ORG_ORGANIZATIONS ORG3 ON BE.FSTOCKINORGID = ORG3.FORGID
            WHERE BC.BARCODE IN(" + strBarcodes + @") AND BC.INSTOCKSTATUS = 0
            GROUP BY BC.BARCODE, ORG.FNUMBER, ORG2.FNUMBER, ORG3.FNUMBER, B.FID, BE.FENTRYID, B.FBILLNO, MTL.FNUMBER, BE.FPRODUCTTYPE, UNT.FNUMBER, BE.FCOSTRATE, DEP.FNUMBER, BQ.FNOSTOCKINQTY";

            dt = ORAHelper.ExecuteTable(_SQL);

            if (dt==null ||dt.Rows.Count == 0) return "在数据库找不到数据";//扫描Barcode后在数据库找不到数据

            client = new K3CloudApiClient(_URL);
            var bLogin = client.Login(_ZTID, _UserName, _PWD, 2052);
            if (bLogin)
            {
                // 开始构建Web API参数对象
                // 参数根对象：包含Creator、NeedUpDateFields、Model这三个子参数
                // using Newtonsoft.Json.Linq;(需引用Newtonsoft.Json.dll)
                JObject jsonRoot = new JObject();

                // Creator: 创建用户
                jsonRoot.Add("Creator", "PDA");

                // NeedUpDateFields: 哪些字段需要更新？为空则表示参数中全部字段，均需要更新
                jsonRoot.Add("NeedUpDateFields", new JArray(""));

                // Model: 单据详细数据参数
                JObject model = new JObject();
                jsonRoot.Add("Model", model);

                // 单据主键：必须填写，系统据此判断是新增还是修改单据；新增单据，填0
                model.Add("FID", 0);

                // 采购组织：必须填写，是基础资料字段
                JObject basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["FSTOCKINORGID"].ToString());
                model.Add("FStockOrgId", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["FPRDORGID"].ToString());
                model.Add("FPrdOrgId", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["FOWNERID"].ToString());
                model.Add("FOwnerId0", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", "SCRKD02_SYS");
                model.Add("FBillType", basedata);

                //采购日期（FDate）
                model.Add("FDate", DateTime.Today);

                basedata = new JObject();
                basedata.Add("FNumber", "PRE001");
                model.Add("FCurrId", basedata);

                // 开始构建单据体参数：集合参数JArray
                JArray entryRows = new JArray();
                // 把单据体行集合，添加到model中，以单据体Key为标识
                string entityKey = "FEntity";
                model.Add(entityKey, entryRows);

                // 通过循环创建单据体行：示例代码仅创建一行
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    // 添加新行，把新行加入到单据体行集合
                    JObject entryRow = new JObject();
                    entryRows.Add(entryRow);

                    // 单据体主键：必须填写，系统据此判断是新增还是修改行
                    entryRow.Add("FEntryID", 0);

                    //物料(FMaterialId)：基础资料，填写编码
                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["MATERIALID"].ToString());
                    entryRow.Add("FMaterialId", basedata);

                    entryRow.Add("FProductType", dt.Rows[i]["FPRODUCTTYPE"].ToString());

                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["FUNITID"].ToString());
                    entryRow.Add("FBaseUnitId", basedata);

                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["FUNITID"].ToString());
                    entryRow.Add("FUnitId", basedata);

                    entryRow.Add("FMustQty", dt.Rows[i]["FQTY"].ToString());
                    entryRow.Add("FRealQty", dt.Rows[i]["FREALQTY"].ToString());
                    entryRow.Add("FCostRate", dt.Rows[i]["FCOSTRATE"].ToString());
                    entryRow.Add("FBaseMustQty", dt.Rows[i]["FQTY"].ToString());
                    entryRow.Add("FBaseRealQty", dt.Rows[i]["FREALQTY"].ToString());
                    entryRow.Add("FBasePRDRealLQTY", dt.Rows[i]["FREALQTY"].ToString());
                    entryRow.Add("FOwnerTypeId", "BD_OwnerOrg");

                    entryRow.Add("FMoBillNo", dt.Rows[i]["MOBILLNO"].ToString());
                    entryRow.Add("FMoId", dt.Rows[i]["FID"].ToString());
                    entryRow.Add("FMoEntryId", dt.Rows[i]["FENTRYID"].ToString());
                    if (decimal.Parse(dt.Rows[i]["FQTY"].ToString()) == decimal.Parse(dt.Rows[i]["FREALQTY"].ToString())) entryRow.Add("FISFINISHED", 1);
                    entryRow.Add("FISBACKFLUSH", 1);

                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["FOWNERID"].ToString());
                    entryRow.Add("FOwnerId", basedata);

                    basedata = new JObject();
                    basedata.Add("FNumber", pInventory);
                    entryRow.Add("FStockId", basedata);

                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["FWORKSHOPID"].ToString());
                    entryRow.Add("FWorkShopId1", basedata);

                    // 创建与源单之间的关联关系，以支持上查与反写源单
                    // 本示例演示创建与采购申请单之间的关联关系
                    // 源单类型、源单编号
                    entryRow.Add("FSrcBillTypeId", "PRD_MO");
                    entryRow.Add("FSrcBillNo", dt.Rows[i]["MOBILLNO"].ToString());

                    // 创建Link行集合
                    JArray linkRows = new JArray();

                    // 添加到单据体行中：Link子单据体标识 = 关联主单据体标识(POOrderEntry) + _Link
                    string linkEntityKey = string.Format("{0}_Link", entityKey);
                    entryRow.Add(linkEntityKey, linkRows);

                    // 创建Link行：
                    // 如有多条源单行，则分别创建Link行记录各条源单行信息
                    JObject linkRow = new JObject();
                    linkRows.Add(linkRow);

                    // 填写Link行上的字段值
                    // 特别说明：Link子单据体上字段的标识，必须在前面增加子单据体标识

                    // FFlowId : 业务流程图，可选
                    string fldFlowIdKey = string.Format("{0}_FFlowId", linkEntityKey);
                    linkRow.Add(fldFlowIdKey, "");

                    // FFlowLineId ：业务流程图路线，可选
                    string fldFlowLineIdKey = string.Format("{0}_FFlowLineId", linkEntityKey);
                    linkRow.Add(fldFlowLineIdKey, "");

                    // FRuleId ：两单之间的转换规则内码，必填
                    // 可以通过如下SQL语句到数据库获取
                    // select FID, *
                    // from T_META_CONVERTRULE 
                    // where FSOURCEFORMID = 'PUR_Requisition' 
                    // and FTARGETFORMID = 'PUR_PurchaseOrder' 
                    // and FDEVTYPE = 0;
                    string fldRuleIdKey = string.Format("{0}_FRuleId", linkEntityKey);
                    linkRow.Add(fldRuleIdKey, "PRD_MO-PRD_INSTOCK");

                    // FSTableName ：必填，源单单据体表格编码，通过如下语句获取：
                    // SELECT FTableNumber 
                    // FROM t_bf_tabledefine 
                    // WHERE fformid = 'PUR_Requisition' 
                    // AND fentitykey = 'FEntity'
                    // 如果如上语句未返回结果，请到K/3 Cloud中，手工选单一次，后台会自动产生表格编码
                    string fldSTableNameKey = string.Format("{0}_FSTableName", linkEntityKey);
                    linkRow.Add(fldSTableNameKey, "T_PRD_MOENTRY");

                    // FSBillId ：必填，源单单据内码
                    string fldSBillIdKey = string.Format("{0}_FSBillId", linkEntityKey);
                    linkRow.Add(fldSBillIdKey, int.Parse(dt.Rows[i]["FID"].ToString()));

                    // FSId : 必填，源单单据体行内码。如果源单主关联实体是单据头，则此属性也填写源单单据内码
                    string fldSIdKey = string.Format("{0}_FSId", linkEntityKey);
                    linkRow.Add(fldSIdKey, int.Parse(dt.Rows[i]["FENTRYID"].ToString()));

                    // FEntity_Link_FBaseQtyOld ：数量原始携带值，下推时，从源单带了多少下来
                    // 在合并下推时，系统会把多个源行的数量，合并后填写在单据体数量字段上的；
                    // 合并后对各源单行的反写，需要使用合并前的数量进行反写，也就是本字段上记录的数量
                    // 因此，如果有合并下推，本字段就必须填写，否则反写不准确
                    // 如果没有合并下推，单据体数量会直接反写到唯一的源行上，本字段就不需填写
                    string fldBaseQtyOldKey = string.Format("{0}_FBaseUnitQtyOld", linkEntityKey);
                    linkRow.Add(fldBaseQtyOldKey, decimal.Parse(dt.Rows[i]["FQTY"].ToString()));

                    // FEntity_Link_FBaseQty ：数量实际携带值，下推后，用户可以手工修改数量值；此字段存储最终的数量值
                    // 可选字段：
                    // 在保存时，系统会自动把单据体上数量值，更新到此字段；因此，这个字段可以不用填写（即使填写了，也会被覆盖）
                    string fldBaseQtyKey = string.Format("{0}_FBaseUnitQty", linkEntityKey);
                    linkRow.Add(fldBaseQtyKey, decimal.Parse(dt.Rows[i]["FREALQTY"].ToString()));

                }
                // 调用Web API接口服务，保存采购订单
                strInstockBillNo = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Save", new object[] { "PRD_INSTOCK", jsonRoot.ToString() });
                JObject jo = JObject.Parse(strInstockBillNo);

                if (!jo["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
                {
                    strInstockBillNo = string.Empty;
                    for (int i = 0; i < ((IList)jo["Result"]["ResponseStatus"]["Errors"]).Count; i++)
                        strInstockBillNo += "[" + dt.Rows[i]["BARCODE"].ToString() + "]" + jo["Result"]["ResponseStatus"]["Errors"][i]["Message"].Value<string>() + "\r\n";//保存不成功返错误信息
                }
                else
                {
                    strInstockBillNo = "ID:" + jo["Result"]["Id"].Value<string>() + ";Number:" + jo["Result"]["Number"].Value<string>();//保存成功返回入库单FID和单据编号FBILLNO

                    client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Submit", new object[] { "PRD_INSTOCK", "{\"CreateOrgId\":\"0\",\"Numbers\":[\"" + jo["Result"]["Number"].Value<string>() + "\"]}" });//根据入库单号提交单据
                    client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Audit", new object[] { "PRD_INSTOCK", "{\"CreateOrgId\":\"0\",\"Numbers\":[\"" + jo["Result"]["Number"].Value<string>() + "\"]}" });//根据入库单号审核单据
                }
            }
            else return "ERP对接失败";
            return strInstockBillNo;//返回格式:ID:xxxx;Number:xxxx
        }
        #endregion

        #region 销售出库
        /// <summary>
        /// 销售出库
        /// </summary>
        /// <param name="pBarcodeList">条码组</param>
        /// <returns></returns>
        public static string SalOutStock(string pBarcodeList)
        {
            DataTable dt;
            K3CloudApiClient client;
            string strSalOutBillNO = string.Empty;

            //根据条码列表查找主产品信息
            if (pBarcodeList.Length == 0)
                return "箱号不正确";

            _SQL = @"SELECT A.FBILLNO, CUST.FNUMBER CUSTID, NVL(A.F_PAEZ_HEADLOCADDRESS, ' ') FADDRESS, ORG.FNUMBER SALEORGID, ORG2.FNUMBER STOCKORGID
              ,CUR.FNUMBER FSETTLECURRID, NVL(CUR2.FNUMBER, ' ') LOCALCURRID, MTL.FNUMBER MATERIALID, UNT.FNUMBER FUNITID, NVL(STK.FNUMBER, 'HWL16') STOCKID
              ,NVL(SP.FBILLNO, ' ') SHARESHIPMENT, AE.FID, AE.FENTRYID, AF.FPRICE, AF.FTAXPRICE, MAX(AR.FREMAINOUTQTY) FREMAINOUTQTY, COUNT(BC.BARCODE) FREALQTY
              ,NVL(ASS.FNUMBER, ' ') FDELIVERYMETHOD, NVL(ASS2.FNUMBER, ' ') FHEADDELIVERYWAY, A.F_PAEZ_CONTACTS, A.F_PAEZ_CONTACTNUMBER, A.FNOTE
              ,AE.FNOTE FENTRYNOTE, CASE WHEN A.F_PAEZ_SINGLESHIPMENT = '0' THEN 'false' ELSE 'true' END F_PAEZ_SINGLESHIPMENT, AE.PRODUCTIONSEQ, DEP.FNUMBER FWORKSHOPID, AE.F_PAEZ_SALEDATE
              ,CASE WHEN BT.FNUMBER IN('XSDD01_SYS','XSDD03_SYS','XSDD04_SYS','XSDD05_SYS') THEN 'XSCKD01_SYS' WHEN BT.FNUMBER = 'XSDD02_SYS' THEN 'XSCKD02_SYS' WHEN BT.FNUMBER = 'XSDD07_SYS' THEN 'XSCKD04_SYS' WHEN BT.FNUMBER = 'XSDD09_SYS' THEN 'XSCKD06_SYS' WHEN BT.FNUMBER = 'XSDD08_SYS' THEN 'XSCKD05_SYS' ELSE 'XSCKD01_SYS' END FBILLTYPEID
              ,NVL(DEP2.FNUMBER,' ') FSALEDEPTID
            FROM C##BARCODE2.PM_PRODUCTPACKAGE PK
            INNER JOIN C##BARCODE2.PM_BARCODE BC ON PK.ID = BC.PACKAGEID AND BC.INSTOCKSTATUS = 1 AND BC.PACKAGESTATUS = 1 AND BC.UTSTOCKSTATUS = 0
            INNER JOIN C##BARCODE2.PM_PRODUCETASK TK ON BC.TASKID = TK.ID
            INNER JOIN T_SAL_ORDERENTRY AE ON BC.KDORDERFENTRYID = AE.FENTRYID
            INNER JOIN T_SAL_ORDERENTRY_F AF ON AE.FENTRYID = AF.FENTRYID
            INNER JOIN T_SAL_ORDERENTRY_R AR ON AF.FENTRYID = AR.FENTRYID
            INNER JOIN T_SAL_ORDER A ON AE.FID = A.FID
            LEFT JOIN T_SAL_ORDERFIN AFI ON A.FID = AFI.FID
            LEFT JOIN T_BD_CURRENCY CUR ON AFI.FSETTLECURRID = CUR.FCURRENCYID
            LEFT JOIN T_BD_CURRENCY CUR2 ON AFI.FLOCALCURRID = CUR2.FCURRENCYID
            INNER JOIN T_BD_CUSTOMER CUST ON A.FCUSTID = CUST.FCUSTID
            INNER JOIN T_BD_DEPARTMENT DEP ON AE.F_PRODUCTDEPARTMENT = DEP.FDEPTID
            LEFT JOIN T_BD_DEPARTMENT DEP2 ON A.FSALEDEPTID = DEP2.FDEPTID
            INNER JOIN T_BAS_BILLTYPE BT ON A.FBILLTYPEID = BT.FBILLTYPEID
            INNER JOIN T_BD_MATERIAL MTL ON AE.FMATERIALID = MTL.FMATERIALID
            INNER JOIN T_BD_UNIT UNT ON AE.FUNITID = UNT.FUNITID
            INNER JOIN T_ORG_ORGANIZATIONS ORG ON A.FSALEORGID = ORG.FORGID
            INNER JOIN T_ORG_ORGANIZATIONS ORG2 ON AE.FSTOCKORGID = ORG2.FORGID
            LEFT JOIN T_BD_STOCK STK ON BC.KDINWAREHOUSEID = STK.FSTOCKID
            LEFT JOIN T_BAS_ASSISTANTDATAENTRY ASS ON A.FDELIVERYMETHOD = ASS.FENTRYID
            LEFT JOIN T_BAS_ASSISTANTDATAENTRY ASS2 ON A.FHEADDELIVERYWAY = ASS2.FENTRYID
            LEFT JOIN C##BARCODE2.PM_SENDPLANENTRY SPE ON A.FID = SPE.ORDERINTERID
            LEFT JOIN C##BARCODE2.PM_SENDPLAN SP ON SPE.FINTERID = SP.FINTERID
            WHERE BC.BARCODE IN(" + pBarcodeList + @") AND PK.SEALEDFLAG = 1 AND AE.FMRPCLOSESTATUS = 'A' AND AR.FREMAINOUTQTY > 0
            GROUP BY CUST.FNUMBER,A.F_PAEZ_HEADLOCADDRESS,ORG.FNUMBER,ORG2.FNUMBER,CUR.FNUMBER,CUR2.FNUMBER,MTL.FNUMBER,UNT.FNUMBER,STK.FNUMBER,SP.FBILLNO,A.FBILLNO,AE.FID,AE.FENTRYID,AF.FPRICE,AF.FTAXPRICE,ASS.FNUMBER,ASS2.FNUMBER,A.F_PAEZ_CONTACTS,A.F_PAEZ_CONTACTNUMBER,A.FNOTE,AE.FNOTE,A.F_PAEZ_SINGLESHIPMENT,AE.PRODUCTIONSEQ,DEP.FNUMBER,AE.F_PAEZ_SALEDATE,BT.FNUMBER,DEP2.FNUMBER
            ORDER BY CUST.FNUMBER";

            dt = ORAHelper.ExecuteTable(_SQL);
            if (dt == null || dt.Rows.Count == 0) return "在数据库找不到数据";//在数据库找不到数据

            //添加辅料（如车标等没有条码的产品）
            bool bIsMPRD = false;//判断物料是否主产品
            string strRemove = string.Empty;//拼接辅料出库字符串
            DataTable dtFL;//获取辅料明细信息
            DataTable dtTemp;//获取不需携带出库物料编码
            
            dtTemp = ORAHelper.ExecuteTable("SELECT FNUMBER FROM DM_NUMBERMATCH WHERE FTYPE = 'UTMTL' AND ISUSE = '1' AND ISDELETE = '0' AND ISMATCH = '1'");

            if (dtTemp != null && dtTemp.Rows.Count > 0)
            {
                strRemove += "(";
                for (int i = 0; i < dtTemp.Rows.Count; i++)
                {
                    if (i != 0) strRemove += " OR ";
                    strRemove += " B.FNUMBER LIKE '" + dtTemp.Rows[i]["FNUMBER"].ToString() + "%' ";
                }
                strRemove += ")";
            }
            else
                strRemove = "B.FNUMBER NOT LIKE '311%' AND B.FNUMBER NOT LIKE '312%' AND B.FNUMBER NOT LIKE '313%' AND B.FNUMBER NOT LIKE '314010807%' AND B.FNUMBER NOT LIKE '3140110%' AND B.FNUMBER NOT LIKE '315%' AND B.FNUMBER NOT LIKE '40101%' AND B.FNUMBER NOT LIKE '40104%' AND B.FNUMBER NOT LIKE '40201%' AND B.FNUMBER NOT LIKE '403%' AND B.FNUMBER NOT LIKE '1801%' AND B.FNUMBER NOT LIKE '1802%'";
            if (dt.Rows[0]["F_PAEZ_SINGLESHIPMENT"].ToString() == "true")//取整单发货订单的辅料
            {
                _SQL = @"SELECT A.FID, AE.FENTRYID, A.FBILLNO, F.FNUMBER FCUSTID
                    , D.FNUMBER SALEORGID, E.FNUMBER STOCKORGID, AE.FQTY, A.FNOTE, AE.FNOTE FENTRYNOTE
                    , AE.PRODUCTIONSEQ, B.FNUMBER MATERIALID, C.FNUMBER FUNITID
                FROM T_SAL_ORDER A
                INNER JOIN T_SAL_ORDERENTRY AE ON A.FID = AE.FID
                INNER JOIN T_SAL_ORDERENTRY_R AR ON AE.FENTRYID = AR.FENTRYID
                INNER JOIN T_BD_MATERIAL B ON AE.FMATERIALID = B.FMATERIALID
                INNER JOIN T_BD_UNIT C ON AE.FUNITID = C.FUNITID
                INNER JOIN T_ORG_ORGANIZATIONS D ON A.FSALEORGID = D.FORGID
                INNER JOIN T_ORG_ORGANIZATIONS E ON AE.FSTOCKORGID = E.FORGID
                INNER JOIN T_BD_CUSTOMER F ON A.FCUSTID = F.FCUSTID
                WHERE A.F_PAEZ_SINGLESHIPMENT = 1 AND " + strRemove + " AND AR.FCANOUTQTY > 0 AND AE.FMRPCLOSESTATUS = 'A' AND A.FBILLNO = '" + dt.Rows[0]["FBILLNO"].ToString() + "'";
            }
            else if (dt.Rows[0]["SHARESHIPMENT"].ToString().Trim().Length > 0)//取拼单发货订单的辅料
            {
                _SQL = @"SELECT A.FID, AE.FENTRYID, A.FBILLNO, G.FNUMBER FCUSTID
                    , E.FNUMBER SALEORGID, F.FNUMBER STOCKORGID, AE.FQTY, A.FNOTE, AE.FNOTE FENTRYNOTE
                    , AE.PRODUCTIONSEQ, B.FNUMBER MATERIALID, C.FNUMBER FUNITID
                FROM T_SAL_ORDER A
                INNER JOIN T_SAL_ORDERENTRY AE ON A.FID = AE.FID
                INNER JOIN T_SAL_ORDERENTRY_R AR ON AE.FENTRYID = AR.FENTRYID
                INNER JOIN T_BD_MATERIAL B ON AE.FMATERIALID = B.FMATERIALID
                INNER JOIN T_BD_UNIT C ON AE.FUNITID = C.FUNITID
                INNER JOIN C##BARCODE2.PM_SENDPLANENTRY DE ON A.FID = DE.ORDERINTERID
                INNER JOIN C##BARCODE2.PM_SENDPLAN D ON DE.FINTERID = D.FINTERID
                INNER JOIN T_ORG_ORGANIZATIONS E ON A.FSALEORGID = E.FORGID
                INNER JOIN T_ORG_ORGANIZATIONS F ON AE.FSTOCKORGID = F.FORGID
                INNER JOIN T_BD_CUSTOMER G ON A.FCUSTID = G.FCUSTID
                WHERE A.F_PAEZ_SINGLESHIPMENT = 0 AND " + strRemove + " AND AR.FCANOUTQTY > 0 AND AE.FMRPCLOSESTATUS = 'A' AND D.FBILLNO = '" + dt.Rows[0]["SHARESHIPMENT"].ToString() + "' AND A.F_PAEZ_HEADLOCADDRESS = '" + dt.Rows[0]["FADDRESS"].ToString() + "'";
            }
            else//取非整非拼单类订单的辅料
            {
                //只取当前订单内的辅料
                string strOrderBillNos = string.Empty;//当前销售订单列表
                for (int j = 0; j < dt.Rows.Count; j++)
                {
                    if (j == 0) strOrderBillNos = "'" + dt.Rows[0]["FBILLNO"].ToString() + "'";
                    else if (!strOrderBillNos.Contains(dt.Rows[j]["FBILLNO"].ToString())) strOrderBillNos += ",'" + dt.Rows[j]["FBILLNO"].ToString() + "'";
                }
                _SQL = @"SELECT A.FID, AE.FENTRYID, A.FBILLNO, G.FNUMBER FCUSTID
                    , E.FNUMBER SALEORGID, F.FNUMBER STOCKORGID, AE.FQTY, A.FNOTE, AE.FNOTE FENTRYNOTE
                    , AE.PRODUCTIONSEQ, B.FNUMBER MATERIALID, C.FNUMBER FUNITID
                FROM T_SAL_ORDER A
                INNER JOIN T_SAL_ORDERENTRY AE ON A.FID = AE.FID
                INNER JOIN T_SAL_ORDERENTRY_R AR ON AE.FENTRYID = AR.FENTRYID
                INNER JOIN T_BD_MATERIAL B ON AE.FMATERIALID = B.FMATERIALID
                INNER JOIN T_BD_UNIT C ON AE.FUNITID = C.FUNITID
                LEFT JOIN C##BARCODE2.PM_SENDPLANENTRY DE ON A.FID = DE.ORDERINTERID
                LEFT JOIN C##BARCODE2.PM_SENDPLAN D ON DE.FINTERID = D.FINTERID
                INNER JOIN T_ORG_ORGANIZATIONS E ON A.FSALEORGID = E.FORGID
                INNER JOIN T_ORG_ORGANIZATIONS F ON AE.FSTOCKORGID = F.FORGID
                INNER JOIN T_BD_CUSTOMER G ON A.FCUSTID = G.FCUSTID
                WHERE A.F_PAEZ_SINGLESHIPMENT = 0 AND (D.FBILLNO IS NULL OR D.FBILLNO = ' ') AND " + strRemove + " AND AR.FCANOUTQTY > 0 AND AE.FMRPCLOSESTATUS = 'A' AND G.FNUMBER = '" + dt.Rows[0]["CUSTID"].ToString() + "' AND A.F_PAEZ_HEADLOCADDRESS = '" + dt.Rows[0]["FADDRESS"].ToString() + "' AND A.FBILLNO IN(" + strOrderBillNos + ")";
            }

            dtFL = ORAHelper.ExecuteTable(_SQL);
            if (dtFL != null && dtFL.Rows.Count > 0)//把辅料明细信息添加到dt中
            {
                DataRow drTmp = null;
                for (int i = 0; i < dtFL.Rows.Count; i++)
                {
                    bIsMPRD = false;
                    drTmp = dt.NewRow();
                    drTmp["FBILLNO"] = dtFL.Rows[i]["FBILLNO"].ToString();
                    drTmp["CUSTID"] = dtFL.Rows[i]["FCUSTID"].ToString();
                    drTmp["FADDRESS"] = dt.Rows[0]["FADDRESS"].ToString();
                    drTmp["SALEORGID"] = dtFL.Rows[i]["SALEORGID"].ToString();
                    drTmp["STOCKORGID"] = dtFL.Rows[i]["STOCKORGID"].ToString();

                    drTmp["FSETTLECURRID"] = dt.Rows[0]["FSETTLECURRID"].ToString();
                    drTmp["LOCALCURRID"] = dt.Rows[0]["LOCALCURRID"].ToString();
                    drTmp["MATERIALID"] = dtFL.Rows[i]["MATERIALID"].ToString();
                    drTmp["FUNITID"] = dtFL.Rows[i]["FUNITID"].ToString();
                    drTmp["STOCKID"] = dt.Rows[0]["STOCKID"].ToString();

                    drTmp["SHARESHIPMENT"] = dt.Rows[0]["SHARESHIPMENT"].ToString();
                    drTmp["FID"] = dtFL.Rows[i]["FID"].ToString();
                    drTmp["FENTRYID"] = dtFL.Rows[i]["FENTRYID"].ToString();
                    drTmp["FPRICE"] = "0";
                    drTmp["FTAXPRICE"] = "0";

                    drTmp["FREMAINOUTQTY"] = "0";
                    drTmp["FREALQTY"] = dtFL.Rows[i]["FQTY"].ToString();

                    drTmp["FDELIVERYMETHOD"] = dt.Rows[0]["FDELIVERYMETHOD"].ToString();
                    drTmp["FHEADDELIVERYWAY"] = dt.Rows[0]["FHEADDELIVERYWAY"].ToString();
                    drTmp["F_PAEZ_CONTACTS"] = dt.Rows[0]["F_PAEZ_CONTACTS"].ToString();
                    drTmp["F_PAEZ_CONTACTNUMBER"] = dt.Rows[0]["F_PAEZ_CONTACTNUMBER"].ToString();
                    drTmp["FNOTE"] = dtFL.Rows[i]["FNOTE"].ToString();

                    drTmp["FENTRYNOTE"] = dtFL.Rows[i]["FENTRYNOTE"].ToString();
                    drTmp["F_PAEZ_SINGLESHIPMENT"] = dt.Rows[0]["F_PAEZ_SINGLESHIPMENT"].ToString();
                    drTmp["PRODUCTIONSEQ"] = dtFL.Rows[i]["PRODUCTIONSEQ"].ToString();
                    drTmp["FWORKSHOPID"] = dt.Rows[0]["FWORKSHOPID"].ToString();
                    drTmp["F_PAEZ_SALEDATE"] = dt.Rows[0]["F_PAEZ_SALEDATE"].ToString();

                    drTmp["FBILLTYPEID"] = dt.Rows[0]["FBILLTYPEID"].ToString();
                    drTmp["FSALEDEPTID"] = dt.Rows[0]["FSALEDEPTID"].ToString();

                    for (int j = 0; j < dt.Rows.Count; j++)
                    {
                        if (dtFL.Rows[i]["MATERIALID"].ToString() == dt.Rows[j]["MATERIALID"].ToString())
                        {
                            bIsMPRD = true;
                            break;
                        }
                    }
                    //物料不是主产品时添加到辅料信息里
                    if (!bIsMPRD)
                        dt.Rows.Add(drTmp);
                }
            }

            client = new K3CloudApiClient(_URL);
            var bLogin = client.Login(_ZTID, _UserName, _PWD, 2052);
            if (bLogin)
            {
                JObject jsonRoot = new JObject();
                jsonRoot.Add("Creator", "PDA");
                jsonRoot.Add("NeedUpDateFields", new JArray(""));

                JObject model = new JObject();
                jsonRoot.Add("Model", model);
                model.Add("FID", 0);

                JObject basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["FBILLTYPEID"].ToString());
                model.Add("FBillTypeID", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["SALEORGID"].ToString());
                model.Add("FSaleOrgId", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["SALEORGID"].ToString());
                model.Add("FSettleOrgID", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["FSALEDEPTID"].ToString());
                model.Add("FSaleDeptId", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["STOCKORGID"].ToString());
                model.Add("FStockOrgId", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["CUSTID"].ToString());
                model.Add("FCustomerID", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["CUSTID"].ToString());
                model.Add("FSettleID", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["CUSTID"].ToString());
                model.Add("FReceiverID", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["CUSTID"].ToString());
                model.Add("FPayerID", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["FSETTLECURRID"].ToString());
                model.Add("FSettleCurrID", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["LOCALCURRID"].ToString());
                model.Add("FLocalCurrID", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["FDELIVERYMETHOD"].ToString());
                model.Add("FDELIVERYMETHOD", basedata);

                basedata = new JObject();
                basedata.Add("FNumber", dt.Rows[0]["FHEADDELIVERYWAY"].ToString());
                model.Add("FHEADDELIVERYWAY", basedata);

                model.Add("F_PAEZ_CONTACTS", dt.Rows[0]["F_PAEZ_CONTACTS"].ToString());
                model.Add("F_PAEZ_CONTACTNUMBER", dt.Rows[0]["F_PAEZ_CONTACTNUMBER"].ToString());
                model.Add("F_PAEZ_SINGLESHIPMENT", dt.Rows[0]["F_PAEZ_SINGLESHIPMENT"].ToString());
                model.Add("FNOTE", dt.Rows[0]["FNOTE"].ToString());

                basedata = new JObject();
                basedata.Add("FNumber", "HLTX01_SYS");
                model.Add("FExchangeTypeID", basedata);

                model.Add("FDate", DateTime.Today);

                model.Add("FReceiveAddress", dt.Rows[0]["FADDRESS"].ToString());
                model.Add("F_PAEZ_HEADLOCADDRESS", dt.Rows[0]["FADDRESS"].ToString());
                model.Add("FOwnerTypeIdHead", "BD_OwnerOrg");

                // 开始构建单据体参数：集合参数JArray
                JArray entryRows = new JArray();
                string entityKey = "FEntity";
                model.Add(entityKey, entryRows);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    JObject entryRow = new JObject();
                    entryRows.Add(entryRow);
                    entryRow.Add("FEntryID", 0);
                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["MATERIALID"].ToString());
                    entryRow.Add("FMaterialId", basedata);
                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["FUNITID"].ToString());
                    entryRow.Add("FSalUnitID", basedata);

                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["FUNITID"].ToString());
                    entryRow.Add("FUnitId", basedata);

                    entryRow.Add("FPrice", dt.Rows[i]["FPRICE"].ToString());
                    entryRow.Add("FTaxPrice", dt.Rows[i]["FTAXPRICE"].ToString());

                    entryRow.Add("FRealQty", dt.Rows[i]["FREALQTY"].ToString());
                    entryRow.Add("FSALUNITQTY", dt.Rows[i]["FREALQTY"].ToString());
                    entryRow.Add("FSALBASEQTY", dt.Rows[i]["FREALQTY"].ToString());
                    entryRow.Add("FPRICEBASEQTY", dt.Rows[i]["FREALQTY"].ToString());
                    entryRow.Add("FEntryTaxRate", 0);
                    entryRow.Add("FOwnerTypeID", "BD_OwnerOrg");

                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["STOCKORGID"].ToString());
                    entryRow.Add("FOwnerID", basedata);

                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["STOCKID"].ToString());
                    entryRow.Add("FStockID", basedata);

                    basedata = new JObject();
                    basedata.Add("FNumber", "KCZT01_SYS");
                    entryRow.Add("FStockStatusID", basedata);

                    entryRow.Add("FEntrynote", dt.Rows[i]["FENTRYNOTE"].ToString());

                    entryRow.Add("FProductionseq", dt.Rows[i]["PRODUCTIONSEQ"].ToString());
                    entryRow.Add("F_PAEZ_Saledate", DateTime.Parse(dt.Rows[i]["F_PAEZ_SALEDATE"].ToString()));
                    basedata = new JObject();
                    basedata.Add("FNumber", dt.Rows[i]["FWORKSHOPID"].ToString());
                    entryRow.Add("FWorkshopid", basedata);

                    entryRow.Add("FSrcBillTypeId", "SAL_SALEORDER");
                    entryRow.Add("FSrcBillNo", dt.Rows[i]["FBILLNO"].ToString());
                    entryRow.Add("FSOORDERNO", dt.Rows[i]["FBILLNO"].ToString());

                    // 创建Link行集合
                    JArray linkRows = new JArray();
                    string linkEntityKey = string.Format("{0}_Link", entityKey);
                    entryRow.Add(linkEntityKey, linkRows);
                    JObject linkRow = new JObject();
                    linkRows.Add(linkRow);
                    string fldFlowIdKey = string.Format("{0}_FFlowId", linkEntityKey);
                    linkRow.Add(fldFlowIdKey, "");
                    string fldFlowLineIdKey = string.Format("{0}_FFlowLineId", linkEntityKey);
                    linkRow.Add(fldFlowLineIdKey, "");
                    string fldRuleIdKey = string.Format("{0}_FRuleId", linkEntityKey);
                    linkRow.Add(fldRuleIdKey, "SAL_SALEORDER-SAL_OUTSTOCK");
                    string fldSTableNameKey = string.Format("{0}_FSTableName", linkEntityKey);
                    linkRow.Add(fldSTableNameKey, "T_SAL_ORDERENTRY");
                    string fldSBillIdKey = string.Format("{0}_FSBillId", linkEntityKey);
                    linkRow.Add(fldSBillIdKey, int.Parse(dt.Rows[i]["FID"].ToString()));
                    string fldSIdKey = string.Format("{0}_FSId", linkEntityKey);
                    linkRow.Add(fldSIdKey, int.Parse(dt.Rows[i]["FENTRYID"].ToString()));
                    string fldBaseQtyOldKey = string.Format("{0}_FBaseUnitQtyOld", linkEntityKey);
                    linkRow.Add(fldBaseQtyOldKey, decimal.Parse(dt.Rows[i]["FREALQTY"].ToString()));
                    string fldBaseQtyKey = string.Format("{0}_FBaseUnitQty", linkEntityKey);
                    linkRow.Add(fldBaseQtyKey, decimal.Parse(dt.Rows[i]["FREALQTY"].ToString()));
                }
                strSalOutBillNO = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Save", new object[] { "SAL_OUTSTOCK", jsonRoot.ToString() });

                JObject jo = JObject.Parse(strSalOutBillNO);
                if (!jo["Result"]["ResponseStatus"]["IsSuccess"].Value<bool>())
                {
                    strSalOutBillNO = string.Empty;
                    for (int i = 0; i < ((IList)jo["Result"]["ResponseStatus"]["Errors"]).Count; i++)
                        strSalOutBillNO += jo["Result"]["ResponseStatus"]["Errors"][i]["Message"].Value<string>() + "\r\n";//保存不成功返错误信息
                }
                else
                {
                    strSalOutBillNO = "ID:" + jo["Result"]["Id"].Value<string>() + ";Number:" + jo["Result"]["Number"].Value<string>();//保存成功返回入库单FID和单据编号FBILLNO

                    client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Submit", new object[] { "SAL_OUTSTOCK", "{\"CreateOrgId\":\"0\",\"Numbers\":[\"" + jo["Result"]["Number"].Value<string>() + "\"]}" });//根据出库单号提交单据
                    client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Audit", new object[] { "SAL_OUTSTOCK", "{\"CreateOrgId\":\"0\",\"Numbers\":[\"" + jo["Result"]["Number"].Value<string>() + "\"]}" });//根据出库单号审核单据

                    //修改主产品对应销售订单的可出数量、可出数量（销售基本）、可出数量（库存基本）的值（公式修改：可出数量=关联采购/生产数量-累计出库数量 改为 可出数量=可出数量-累计出库数量）
                    _SQL = @"UPDATE T_SAL_ORDERENTRY_R A
                    SET A.FCANOUTQTY = A.FCANOUTQTY - A.FSTOCKOUTQTY, A.FBASECANOUTQTY = A.FBASECANOUTQTY - A.FSTOCKOUTQTY, A.FSTOCKBASECANOUTQTY = A.FSTOCKBASECANOUTQTY - A.FSTOCKOUTQTY
                    WHERE A.FCANOUTQTY > 0 AND
                    EXISTS
                    (
                        SELECT 1 FROM C##BARCODE2.PM_BarCode B
                        WHERE A.FENTRYID = B.KDORDERFENTRYID AND B.BARCODE IN(" + pBarcodeList + @")
                    )";
                    ORAHelper.ExecuteNonQuery(_SQL);
                    //修改辅料产品对应销售订单的出数量、可出数量（销售基本）、可出数量（库存基本）的值
                    if (dtFL.Rows.Count > 0)
                    {
                        string strFentryids = string.Empty;
                        for (int i = 0; i < dtFL.Rows.Count; i++)
                        {
                            if (i > 0) strFentryids += ",";
                            strFentryids += dtFL.Rows[i]["FENTRYID"].ToString();
                        }
                        _SQL = @"UPDATE T_SAL_ORDERENTRY_R
                        SET FCANOUTQTY = FCANOUTQTY - FSTOCKOUTQTY,FBASECANOUTQTY = FBASECANOUTQTY - FSTOCKOUTQTY,FSTOCKBASECANOUTQTY = FSTOCKBASECANOUTQTY - FSTOCKOUTQTY
                        WHERE FCANOUTQTY > 0 AND FENTRYID IN(" + strFentryids + ")";
                        ORAHelper.ExecuteNonQuery(_SQL);
                    }
                }
            }
            else return "ERP对接失败";
            return strSalOutBillNO;//返回格式:ID:xxxx;Number:xxxx|ID:xxxx;Number:xxxx
        }
        #endregion

        #region 装箱
        /// <summary>
        /// 装箱
        /// </summary>
        /// <param name="pPackageId">箱号内码</param>
        /// <param name="pCustId">客户内码</param>
        /// <param name="pBarcodes">条码组</param>
        /// <param name="pVolume">体积</param>
        /// <param name="pType">类型标识</param>
        /// <param name="pMaxBoxNumber">箱号最大值</param>
        /// <returns></returns>
        public static string Package(int pPackageId, int pCustId, string pBarcodes, double pVolume, int pType, int pMaxBoxNumber)
        {
            OracleConnection OrlConn = new OracleConnection(_ConnectionString);

            switch (pType)
            {
                case 0://解封
                    _SQL = @"BEGIN
                    UPDATE C##BARCODE2.PM_PRODUCTPACKAGE
                    SET SEALEDFLAG = 0
                    WHERE  ID = " + pPackageId.ToString() + @";
                    COMMIT;
                    --DBMS_OUTPUT.PUT_LINE('解封成功');
                    EXCEPTION
                    WHEN OTHERS THEN 
                        ROLLBACK; -- 出现异常则回滚事务
                    --DBMS_OUTPUT.PUT_LINE('解封失败');
                    DBMS_OUTPUT.PUT_LINE(SQLERRM);
                    END;";
                    break;
                case 1://装箱
                    _SQL = @"BEGIN
                    UPDATE C##BARCODE2.PM_BarCode
                    SET PACKAGESTATUS = 1, PACKAGEID = " + pPackageId.ToString() + @"
                    WHERE BARCODE IN(" + pBarcodes + @");
                    UPDATE C##BARCODE2.PM_PRODUCTPACKAGE
                    SET SEALEDFLAG = 0, KDCUSTID = " + pCustId.ToString() + ", BOXNUMBER = " + pMaxBoxNumber.ToString() + @"
                    WHERE  ID = " + pPackageId.ToString() + @";
                    UPDATE C##BARCODE2.PM_BarCode A
                    SET A.PACKAGESTATUS = 0, A.PACKAGEID = NULL
                    WHERE A.BARCODE NOT IN(" + pBarcodes + @")
                    AND EXISTS
                    (
                        SELECT 1 FROM C##BARCODE2.PM_PRODUCTPACKAGE B
                        WHERE A.PACKAGEID = B.ID AND B.ID = " + pPackageId.ToString() + @"
                    );
                    COMMIT;
                    --DBMS_OUTPUT.PUT_LINE('装箱成功');
                    EXCEPTION
                    WHEN OTHERS THEN 
                        ROLLBACK;
                    --DBMS_OUTPUT.PUT_LINE('装箱失败');
                    DBMS_OUTPUT.PUT_LINE(SQLERRM);
                    END;";
                    break;
                case 2://封箱
                    _SQL = @"BEGIN
                    UPDATE C##BARCODE2.PM_BarCode
                    SET PACKAGESTATUS = 1, PACKAGEID = " + pPackageId.ToString() + @"
                    WHERE BARCODE IN(" + pBarcodes + @");
                    UPDATE C##BARCODE2.PM_PRODUCTPACKAGE
                    SET SEALEDFLAG = 1, VOLUME = " + pVolume.ToString() + ", KDCUSTID = " + pCustId.ToString() + @"
                    WHERE ID = " + pPackageId.ToString() + @";
                    UPDATE C##BARCODE2.PM_BarCode A
                    SET A.PACKAGESTATUS = 0, A.PACKAGEID = NULL
                    WHERE A.BARCODE NOT IN(" + pBarcodes + @")
                    AND EXISTS
                    (
                        SELECT 1 FROM C##BARCODE2.PM_PRODUCTPACKAGE B
                        WHERE A.PACKAGEID = B.ID AND B.ID = " + pPackageId.ToString() + @"
                    );
                    COMMIT;
                    --DBMS_OUTPUT.PUT_LINE('装箱成功');
                    EXCEPTION
                    WHEN OTHERS THEN
                        ROLLBACK;
                    --DBMS_OUTPUT.PUT_LINE('装箱失败');
                    DBMS_OUTPUT.PUT_LINE(SQLERRM);
                    END;";
                    break;
                default:
                    _SQL = string.Empty;
                    break;
            }

            if (!_SQL.Equals(string.Empty))
            {
                try
                {
                    OrlConn.Open();
                    OracleCommand cmd = OrlConn.CreateCommand();
                    cmd.CommandText = _SQL;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
                finally
                {
                    OrlConn.Close();
                }
            }

            return string.Empty;
        }
        #endregion

        #region 保存PDA日志
        /// <summary>
        /// 保存日志记录
        /// </summary>
        /// <param name="pFNumbers">单号</param>
        /// <param name="pOperator">员工</param>
        /// <param name="pType">业务类型</param>
        /// <param name="pFLAG">状态标识</param>
        /// <param name="pDescription">描述</param>
        /// <param name="pIP">IP地址</param>
        /// <param name="pBARCODES">条码组</param>
        /// <param name="pERMESSAGE">异常信息</param>
        /// <param name="pMOBILLS">生产单号组</param>
        /// <param name="pMOENTRYID">生产分录内码</param>
        /// <param name="pORDERBILLS">订单号组</param>
        /// <param name="pORDERENTRYID">订单分录内码</param>
        /// <param name="pMATERIALID">物料内码</param>
        public static void SaveLog(string pFNumbers, string pOperator, string pType, int pFLAG, string pDescription, string pIP, string pBARCODES, string pERMESSAGE, string pMOBILLS, string pMOENTRYID, string pORDERBILLS, string pORDERENTRYID, string pMATERIALID)
        {
            _SQL = @"INSERT INTO DM_EXCEPTIONRECORD(FNUMBER,CREATOR,FTYPE,FFLAGE,DESCRIPTION,FIP,BARCODES,ERMESSAGE,MOBILLS,MOENTRYID,ORDERBILLS,ORDERENTRYID,MATERIALID)
            VALUES('" + pFNumbers + "','" + pOperator + "','" + pType + "'," + pFLAG.ToString() + ",'" + pDescription + "','" + pIP + "','" + pBARCODES + "','" + pERMESSAGE + "','" + pMOBILLS + "','" + pMOENTRYID + "','" + pORDERBILLS + "','" + pORDERENTRYID + "','" + pMATERIALID + "')";

            ORAHelper.ExecuteNonQuery(_SQL);
        }
        #endregion
    }
}