using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace LSportsServer
{
    public static class CMySql
    {
        public static Queue<string> lstQuery = new Queue<string>();
        public static object objQueryList = new object();

        public static string GetDBConnectString()
        {
            string db_server = "server=" + CDefine.DB_ADDR + ";";
            db_server += "port=" + CDefine.DB_PORT + ";";
            db_server += "database=" + CDefine.DB_NAME + ";";
            db_server += "uid=" + CDefine.DB_USER + ";";
            db_server += "pwd=" + CDefine.DB_PASS + ";";
            db_server += "CharSet=utf8;";

            return db_server;
        }


        public static int ExcuteQuery(string sql)
        {
            int nSn = 0;

            // CGlobal.ShowConsole(sql);
            using (MySqlConnection mysqlCon = new MySqlConnection(GetDBConnectString()))
            {
                mysqlCon.Open();
                MySqlCommand command = new MySqlCommand(sql, mysqlCon);
                command.ExecuteNonQuery();
                nSn = Convert.ToInt32(command.LastInsertedId);

                mysqlCon.Close();
            }

            return nSn;
        }

        public static void  ExcuteQueryList(List<string> lstSql)
        {
            using (MySqlConnection mysqlCon = new MySqlConnection(GetDBConnectString()))
            {
                mysqlCon.Open();

                foreach(string sql in lstSql)
                {
                    CGlobal.ShowConsole(sql);
                    MySqlCommand command = new MySqlCommand(sql, mysqlCon);
                    command.ExecuteNonQuery();

                    Thread.Sleep(10);
                }
                mysqlCon.Close();
            }
        }

        public static void ExcuteCommonQuery()
        {
            MySqlConnection mysqlCon = new MySqlConnection(GetDBConnectString());
            mysqlCon.Open();

            string strQuery = "";

            while (true)
            {
                strQuery = "";

                lock (objQueryList)
                {
                    if (lstQuery.Count > 0)
                    {
                        strQuery = lstQuery.Dequeue();
                    }
                }

                if (strQuery == "")
                {
                    Thread.Sleep(10);
                    continue;
                }

                try
                {
                    CGlobal.ShowConsole(strQuery);

                    new MySqlCommand(strQuery, mysqlCon).ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);

                    mysqlCon.Close();
                    break;
                }

                Thread.Sleep(10);
            }

            Thread.Sleep(100);

            ExcuteCommonQuery();
        }

        public static void PushCommonQuery(string strQuery)
        {
            lock (objQueryList)
            {
                lstQuery.Enqueue(strQuery);
            }
        }

        public static DataRowCollection GetDataQuery(string sql)
        {
            // CGlobal.ShowConsole(sql);
            try
            {
                using (MySqlConnection mysqlCon = new MySqlConnection(GetDBConnectString()))
                {
                    mysqlCon.Open();
                    DataSet dataset = new DataSet();
                    MySqlDataAdapter adapter = new MySqlDataAdapter();
                    adapter.SelectCommand = new MySqlCommand(sql, mysqlCon);
                    adapter.Fill(dataset);
                    DataRowCollection list = dataset.Tables[0].Rows;

                    mysqlCon.Close();
                    
                    return list;
                }
            }
            catch
            {
                DataTable tb = new DataTable();
                DataRowCollection list = tb.Rows;
                return list;
            }
        }
    }
}
