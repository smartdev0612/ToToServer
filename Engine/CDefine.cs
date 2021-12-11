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
        public static string SERVER_MINI_PORT;

        public static string DB_ADDR;
        public static string DB_NAME;
        public static string DB_USER;
        public static string DB_PASS;
        public static string DB_PORT;

        public static string LSPORTS_ADDRESS;
        public static string LSPORTS_PREMATCH_LIVE;
        public static string LSPORTS_INPLAY_LIVE;
        public static string LSPORTS_PREMATCH_DATA;
        public static string LSPORTS_INPLAY_DATA;
        public static string LSPORTS_SCHEDULE;
        public static string LSPORTS_HTTP_PORT;

        public static string USE_PREMATCH;
        public static string USE_LIVE;
        public static string USE_POWERBALL;
        public static string USE_POWERLADDER;

        public static string USE_WSS;

        public static string POWER_SERVER;


        public const int LSPORTS_DATA = 0x00;
        public const int LSPORTS_PREMATCH = 0x01;
        public const int LSPORTS_INPLAY = 0x02;


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

                CDefine.LSPORTS_ADDRESS = node.SelectSingleNode("lsports_address").InnerText;
                CDefine.LSPORTS_PREMATCH_LIVE = node.SelectSingleNode("lsports_prematch_live").InnerText;
                CDefine.LSPORTS_INPLAY_LIVE = node.SelectSingleNode("lsports_inplay_live").InnerText;
                CDefine.LSPORTS_PREMATCH_DATA = node.SelectSingleNode("lsports_prematch_data").InnerText;
                CDefine.LSPORTS_INPLAY_DATA = node.SelectSingleNode("lsports_inplay_data").InnerText;
                CDefine.LSPORTS_SCHEDULE = node.SelectSingleNode("lsports_schedule").InnerText;
                CDefine.LSPORTS_HTTP_PORT = node.SelectSingleNode("lsports_http_port").InnerText;

                CDefine.POWER_SERVER = node.SelectSingleNode("power_server").InnerText;

                CDefine.USE_PREMATCH = node.SelectSingleNode("use_prematch").InnerText;
                CDefine.USE_LIVE = node.SelectSingleNode("use_live").InnerText;
                CDefine.USE_POWERBALL = node.SelectSingleNode("use_powerball").InnerText;
                CDefine.USE_POWERLADDER = node.SelectSingleNode("use_powerladder").InnerText;

                CDefine.SERVER_HTTP = node.SelectSingleNode("server_http").InnerText;
                CDefine.SERVER_ADDR = node.SelectSingleNode("server_addr").InnerText;
                CDefine.SERVER_PORT = node.SelectSingleNode("server_port").InnerText;
                CDefine.SERVER_MINI_PORT = node.SelectSingleNode("server_mini_port").InnerText;

                CDefine.USE_WSS = node.SelectSingleNode("use_wss").InnerText;
            }
        }
    }
}
