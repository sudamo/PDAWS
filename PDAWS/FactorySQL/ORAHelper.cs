/**
 * Copyright
 * DM Software Inc.
 * 
 * This is the confidential proprietary property of DM Software Inc. This document is 
 * protected by copyright. No part of it may be reproduced or copied without the prior written
 * permission of DM Software Inc. DM products are supplied under licence and
 * may be used only in accordance with the terms of the contractual agreement between DM
 * and the licence holder. All products, brand names and trademarks referred to in this 
 * publication are the property of DM or third party owners. Unauthorised use may
 * constitute an infringement. DM Software Inc reserves the right to change information
 * contained in this publication without notice. All efforts have been made to ensure accuracy
 * however DM Software Inc does not assume responsibility for errors or for any
 * consequences arising from errors in this publication. 
 *
 * Author: Damo
 * Created: November 1, 2018
 * 
 * Modified Date     Description of Change
 * ======== ======== =====================
 * DM	10-24-18 Add methods GenInExpression(int), GenInExpression(string), ReviseSQL(string)
 */

namespace PDAWS.FactorySQL
{
    using System.Data;
    using Oracle.ManagedDataAccess.Client;
    internal static class ORAHelper
    {
        private static string _ConnectionString;
        static ORAHelper()
        {
            _ConnectionString = cnCB.ConnectionString;
        }

        internal static int ExecuteNonQuery(string pCommandText)
        {
            OracleConnection conn = new OracleConnection(_ConnectionString);

            try
            {
                conn.Open();
                OracleCommand cmd = conn.CreateCommand();
                cmd.CommandText = pCommandText;
                return cmd.ExecuteNonQuery();
            }
            catch { return -1; }
            finally
            {
                conn.Close();
            }
        }
        internal static object ExecuteScalar(string pCommandText)
        {
            object o = new object();
            OracleConnection conn = new OracleConnection(_ConnectionString);

            try
            {
                conn.Open();
                OracleCommand cmd = conn.CreateCommand();
                cmd.CommandText = pCommandText;
                o = cmd.ExecuteScalar();
            }
            catch { return null; }
            finally
            {
                conn.Close();
            }
            return o;
        }
        internal static DataTable ExecuteTable(string pCommandText)
        {
            DataTable dt = new DataTable();
            OracleConnection conn = new OracleConnection(_ConnectionString);

            try
            {
                conn.Open();
                OracleDataAdapter adp = new OracleDataAdapter(pCommandText, conn);
                adp.SelectCommand.CommandTimeout = 10000;
                adp.Fill(dt);
            }
            catch { return null; }
            finally
            {
                conn.Close();
            }

            return dt;
        }

        #region 数据库操作
        /// <summary>
        /// 数据库操作
        /// </summary>
        /// <param name="pType">0、NonQuery;1、Scalar;2、Reader;3、DataTable;4、DataSet</param>
        /// <param name="pStrSQL">SQL Sentence</param>
        /// <returns></returns>
        internal static object SqlOperation(int pType, string pStrSQL)
        {
            object obj;
            OracleDataAdapter adp;
            DataTable dt;
            DataSet ds;

            OracleConnection conn = new OracleConnection(_ConnectionString);

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