using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LSportsServer
{
    public static class CGlobal
    {
        private static List<CSports> _lstSports;
        private static List<CCountry> _lstCountry;
        private static List<CLeague> _lstLeague;
        private static List<CMarket> _lstMarket;
        private static List<CTeam> _lstTeam;
        private static List<CPeriod> _lstPeriod;

        private static List<CGame> _lstGame;
        private static Queue<string>[] _lstStrLSportsPacket;

        private static CGameServer _wsServer;


        public static void InitProcess()
        {
            CDefine.LoadConfigFromXml();

            _lstSports = new List<CSports>();
            _lstCountry = new List<CCountry>();
            _lstLeague = new List<CLeague>();
            _lstMarket = new List<CMarket>();
            _lstTeam = new List<CTeam>();
            _lstPeriod = new List<CPeriod>();
            _lstGame = new List<CGame>();

            _lstStrLSportsPacket = new Queue<string>[3]
            {
                new Queue<string>(), new Queue<string>(), new Queue<string>()
            };

            LoadInfoFromDB();

            CLSports.Connect();
            //CPowerball.StartPowerball();
            CEngine.StartRealProcess();
            CServer.Start();
        }

        private static void LoadInfoFromDB()
        {
            DataRowCollection list = CEntry.SelectSports();
            foreach (DataRow info in list)
            {
                CSports clsInfo = new CSports();
                clsInfo.LoadInfo(info);
                _lstSports.Add(clsInfo);
            }
            Console.WriteLine($"Sports info {_lstSports.Count}");

            list = CEntry.SelectCountry();
            foreach (DataRow info in list)
            {
                CCountry clsInfo = new CCountry();
                clsInfo.LoadInfo(info);
                _lstCountry.Add(clsInfo);
            }
            Console.WriteLine($"Country info {_lstCountry.Count}");

            list = CEntry.SelectLeague();
            foreach (DataRow info in list)
            {
                CLeague clsInfo = new CLeague();
                clsInfo.LoadInfo(info);
                _lstLeague.Add(clsInfo);
            }
            Console.WriteLine($"League info {_lstLeague.Count}");

            list = CEntry.SelectTeam();
            foreach (DataRow info in list)
            {
                CTeam clsInfo = new CTeam();
                clsInfo.LoadInfo(info);
                _lstTeam.Add(clsInfo);
            }
            Console.WriteLine($"Team info {_lstTeam.Count}");

            list = CEntry.SelectMarket();
            foreach (DataRow info in list)
            {
                CMarket clsInfo = new CMarket();
                clsInfo.LoadInfo(info);
                _lstMarket.Add(clsInfo);
            }
            Console.WriteLine($"Market info {_lstMarket.Count}");

            list = CEntry.SelectPeriod();
            foreach (DataRow info in list)
            {
                CPeriod clsInfo = new CPeriod();
                clsInfo.LoadInfo(info);
                _lstPeriod.Add(clsInfo);
            }
            Console.WriteLine($"Period info {_lstPeriod.Count}");
                
            new Thread(() => CLSports.LoadGameInfoToDB()).Start();
        }

        public static CSports GetSportsInfoByCode(int nCode)
        {
            return _lstSports.Find(value => value.m_nCode == nCode);
        }

        public static List<CSports> GetSportsList()
        {
            return _lstSports;
        }

        public static CCountry GetCountryInfoByCode(int nCode)
        {
            return _lstCountry.Find(value => value.m_nCode == nCode);
        }

        public static CLeague  GetLeagueInfoByCode(int nCode)
        {
            return _lstLeague.Find(value => value.m_nCode == nCode);
        }

        public static CMarket GetMarketInfoByCode(int nCode)
        {
            return _lstMarket.Find(value => value.m_nCode == nCode);
        }

        public static CTeam GetTeamInfoByCode(int nCode)
        {
            return _lstTeam.Find(value => value.m_nCode == nCode);
        }

        public static CGame GetGameInfoByCode(int nCode)
        {
            return _lstGame.Find(value => value.m_nCode == nCode);
        }

        public static CGame GetGameInfoByFixtureID(long nFixtureID)
        {
            return _lstGame.Find(value => value.m_nFixtureID == nFixtureID);
        }

        public static List<CGame> GetGameList()
        {
            return _lstGame;
        }

        public static CPeriod GetPeriodInfoByCode(int nSports, int nPeriod)
        {
            return _lstPeriod.Find(value => value.m_nPeriod == nPeriod && value.m_nSports == nSports);
        }

        public static void AddGameInfo(CGame clsInfo)
        {
            _lstGame.Add(clsInfo);
        }

        public static void RemoveGame(CGame clsInfo)
        {
            _lstGame.Remove(clsInfo);
        }

        public static void AddLSportsPacket(int nFlag, string strPacket)
        {
            lock (_lstStrLSportsPacket[nFlag])
            {
                _lstStrLSportsPacket[nFlag].Enqueue(strPacket);
            }
        }

        public static string GetLSportsPacket(int nFlag)
        {
            string strPacket = string.Empty;
            lock(_lstStrLSportsPacket[nFlag])
            {
                if (_lstStrLSportsPacket[nFlag].Count > 0)
                    strPacket = _lstStrLSportsPacket[nFlag].Dequeue();
            }

            return strPacket;
        }

        public static void ShowConsole(string strMsg)
        {
            Console.WriteLine($"{CMyTime.GetMyTimeStr()}        {strMsg}");
        }

        public static async Task WriteLogAsync(string strLog)
        {
            using StreamWriter file = new("WriteLog.txt", append: true);
            await file.WriteLineAsync(strLog);
        }

        public static bool CheckMarketID(int nID)
        {
            return _lstMarket.Exists(value => value.m_nUse == 1 && value.m_nCode == nID);
        }

        public static void SetBroadcastSocket(CGameServer ws)
        {
            _wsServer = ws;
        }

        public static void BroadCastPowerTime(int nGNum, int nDNum, string strTime)
        {
            CPacket packet = new CPacket();
            packet.m_nPacketCode = CDefine.PACKET_POWERBALL_TIME;

            DateTime now = CMyTime.GetMyTime();
            string strYear = now.ToString("yyyy");
            string strMonth = now.ToString("MM");
            string strDay = now.ToString("dd");
            string strHour = now.ToString("HH");
            string strMin = now.ToString("mm");
            string strSec = now.ToString("ss");

            string strPacket = $"{strYear}|{strMonth}|{strDay}|{strHour}|{strMin}|{strSec}|{nGNum}|{strTime}|{nDNum}";
            packet.m_strPacket = strPacket;
            
            if (_wsServer != null)
            {
                _wsServer.BroadCastPacket(packet);
                CGlobal.ShowConsole("Send Mini Time");
            }
            else
            {
                CGlobal.ShowConsole("Websocket Null");
            }
        }

        public static int ParseInt<T>(T value)
        {
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }
    }
}
