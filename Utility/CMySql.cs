using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace LSportsServer
{
    public static class CMySql
    {
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


        public static long ExcuteQuery(string sql)
        {
            long nSn = 0;

            CGlobal.ShowConsole(sql);
            using (MySqlConnection mysqlCon = new MySqlConnection(GetDBConnectString()))
            {
                mysqlCon.Open();
                MySqlCommand command = new MySqlCommand(sql, mysqlCon);
                command.ExecuteNonQuery();
                nSn = command.LastInsertedId;

                mysqlCon.Close();
            }

            return nSn;
        }


        public static DataRowCollection GetDataQuery(string sql)
        {
            CGlobal.ShowConsole(sql);
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
