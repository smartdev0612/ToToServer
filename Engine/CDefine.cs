using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace LSportsServer
{
    public static class CDefine
    {
        public static string SERVER_HTTP;
        public static string SERVER_ADDR;
        public static string SERVER_PORT;

        public static string DB_ADDR;
        public static string DB_NAME;
        public static string DB_USER;
        public static string DB_PASS;
        public static string DB_PORT;

        public static string ADDR_SEVER;
        public static string SOCKET_PREMATCH;
        public static string SOCKET_LIVE;
        public static string SOCKET_DATA;
        public static string HTTP_PORT;

        public static string USE_PREMATCH;
        public static string USE_LIVE;
        public static string USE_POWERBALL;
        public static string USE_POWERLADDER;

        public static string POWER_SERVER;

        public static string API_URL;
        public static string API_USERNAME;
        public static string API_PASSWORD;
        public static string API_GUID;
        public static string API_PREMATCH_PACKAGE_ID;
        public static string API_INPLAY_PACKAGE_ID;

        public const int LSPORTS_DATA = 0x00;
        public const int LSPORTS_PREMATCH = 0x01;
        public const int LSPORTS_LIVE = 0x02;


        public const int LSPORTS_SPORTS_SOCCER = 6046;
        public const int LSPORTS_SPORTS_BASKETBALL = 48242;
        public const int LSPORTS_SPORTS_BASEBALL = 154914;
        public const int LSPORTS_SPORTS_VOLLEYBALL = 154830;
        public const int LSPORTS_SPORTS_HOCKEY = 35232;
        public const int LSPORTS_SPORTS_ESPORTS = 687890;



        //LSports
        public const int PACKET_SPORT_LIST = 0x01;
        public const int PACKET_SPORT_BET = 0x02;
        public const int PACKET_SPORT_AJAX = 0x03;

        //Powerball
        public const int PACKET_POWERBALL_TIME = 0x11;
        public const int PACKET_POWERBALL_BET = 0x12;

        //파워사다리
        public const int PACKET_POWERLADDER_BET = 0x22;

        //키노사다리
        public const int PACKET_KENOLADDER_BET = 0x32;


        public static void LoadConfigFromXml()
        {
            XmlDocument xmldoc = new XmlDocument();
            FileStream fs = new FileStream("config.xml", FileMode.Open, FileAccess.Read);
            xmldoc.Load(fs);
            XmlNodeList nodeList = xmldoc.GetElementsByTagName("config");

            foreach (XmlNode node in nodeList)
            {
                CDefine.DB_ADDR = node.SelectSingleNode("db_addr").InnerText;
                CDefine.DB_NAME = node.SelectSingleNode("db_name").InnerText;
                CDefine.DB_PORT = node.SelectSingleNode("db_port").InnerText;
                CDefine.DB_USER = node.SelectSingleNode("db_user").InnerText;
                CDefine.DB_PASS = node.SelectSingleNode("db_pass").InnerText;
                CDefine.API_URL = node.SelectSingleNode("api_url").InnerText;
                CDefine.API_USERNAME = node.SelectSingleNode("api_username").InnerText;
                CDefine.API_PASSWORD = node.SelectSingleNode("api_password").InnerText;
                CDefine.API_GUID = node.SelectSingleNode("api_guid").InnerText;
                CDefine.API_PREMATCH_PACKAGE_ID = node.SelectSingleNode("api_prematch_package_id").InnerText;
                CDefine.API_INPLAY_PACKAGE_ID = node.SelectSingleNode("api_live_package_id").InnerText;
                CDefine.SOCKET_PREMATCH = node.SelectSingleNode("socket_prematch").InnerText;
                CDefine.SOCKET_LIVE = node.SelectSingleNode("socket_live").InnerText;
                CDefine.SOCKET_DATA = node.SelectSingleNode("socket_data").InnerText;
                CDefine.ADDR_SEVER = node.SelectSingleNode("addr_server").InnerText;
                CDefine.HTTP_PORT = node.SelectSingleNode("http_port").InnerText;
                CDefine.POWER_SERVER = node.SelectSingleNode("power_server").InnerText;

                CDefine.USE_PREMATCH = node.SelectSingleNode("use_prematch").InnerText;
                CDefine.USE_LIVE = node.SelectSingleNode("use_live").InnerText;
                CDefine.USE_POWERBALL = node.SelectSingleNode("use_powerball").InnerText;
                CDefine.USE_POWERLADDER = node.SelectSingleNode("use_powerladder").InnerText;

                CDefine.SERVER_HTTP = node.SelectSingleNode("server_http").InnerText;
                CDefine.SERVER_ADDR = node.SelectSingleNode("server_addr").InnerText;
                CDefine.SERVER_PORT = node.SelectSingleNode("server_port").InnerText;
            }
        }
    }
}
