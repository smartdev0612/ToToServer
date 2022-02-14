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
        private static List<CBetting> _lstApiBetting;
        private static List<long> _lstlnGetApiFixtureID;
        private static List<long> _lstlnGetLiveFixtureID;

        private static CGameServer _wsServer;
        private static CMiniGameServer _wsMiniServer;

        public static object _objLock = new object();

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
            _lstApiBetting = new List<CBetting>();

            _lstlnGetApiFixtureID = new List<long>();
            _lstlnGetLiveFixtureID = new List<long>();

            new Thread(CMySql.ExcuteCommonQuery).Start();

            CServer.Start();
            CMiniServer.Start();

            CLSports.Connect();
            CEngine.StartRealProcess();
            LoadInfoFromDB();

            CPowerball.StartPowerball();
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

            // API경기들에 배팅한 리력 적재
            list = CEntry.SelectSportsBetting();
            foreach (DataRow info in list)
            {
                CBetting clsInfo = new CBetting();
                clsInfo.LoadInfo(info);
                AddSportsApiBetting(clsInfo);
            }
            Console.WriteLine($"Sports API Betting info {_lstApiBetting.Count}");

            // 실시간게임 적재
            LoadRealtimeGameFromDB();

            CEngine.ClearDBThread();

            Task.Factory.StartNew(() => CLSports.LoadAvailableFixtures());

            new Thread(() => CLSports.LoadGameInfoToDB()).Start();
        }

        public static void LoadRealtimeGameFromDB()
        {
            DataRowCollection list = CEntry.SelectRealtimeGame();
            List<CGame> lstGame = CGlobal.GetGameList();
            foreach (DataRow row in list)
            {
                CGame clsGame = new CGame();
                if (lstGame.Exists(value => value.m_nFixtureID == CGlobal.ParseInt64(row["game_sn"])) == false)
                {
                    clsGame.LoadInfo(row);
                    CGlobal.AddGameInfo(clsGame);

                    DataRowCollection betRateList = CEntry.SelectBetRate(clsGame.m_nCode);
                    if (betRateList.Count > 0)
                    {
                        foreach (DataRow betRateRow in betRateList)
                        {
                            CBetRate clsBetRate = new CBetRate(clsGame);
                            clsBetRate.LoadInfo(betRateRow);
                            clsGame.AddPrematchBetRate(clsBetRate);
                        }
                    }
                }
                else
                {
                    clsGame = CGlobal.GetGameInfoByFixtureID(CGlobal.ParseInt64(row["game_sn"]));
                    if(clsGame != null)
                    {
                        clsGame.LoadInfo(row);

                        DataRowCollection betRateList = CEntry.SelectBetRate(clsGame.m_nCode);
                        if (betRateList.Count > 0)
                        {
                            foreach (DataRow betRateRow in betRateList)
                            {
                                CBetRate clsBetRate = new CBetRate(clsGame);
                                clsBetRate.LoadInfo(betRateRow);
                                clsGame.AddPrematchBetRate(clsBetRate);
                            }
                        }
                    }
                }
            }
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

        public static CLeague GetLeagueInfoBySn(int nSn)
        {
            return _lstLeague.Find(value => value.m_nSn == nSn);
        }

        public static void AddLeagueInfo(CLeague clsLeague)
        {
            lock(_lstLeague)
            {
                if(_lstLeague.Exists(value => value.m_nSn == clsLeague.m_nSn) == false)
                {
                    _lstLeague.Add(clsLeague);
                }
            }
        }

        public static void RemoveLeague(CLeague clsLeague)
        {
            lock (_lstLeague)
            {
                _lstLeague.Remove(clsLeague);
            }
        }

        public static CMarket GetMarketInfoByCode(int nCode)
        {
            return _lstMarket.Find(value => value.m_nCode == nCode);
        }

        public static CTeam GetTeamInfoByCode(int nCode)
        {
            return _lstTeam.Find(value => value.m_nCode == nCode);
        }

        public static CTeam GetTeamInfoBySn(int nSn)
        {
            return _lstTeam.Find(value => value.m_nSn == nSn);
        }

        public static void RemoveTeam(CTeam clsTeam)
        {
            lock (_lstTeam)
            {
                _lstTeam.Remove(clsTeam);
            }
        }

        public static void AddTeamInfo(CTeam clsTeam)
        {
            lock(_lstTeam)
            {
                if (_lstTeam.Exists(value => value.m_nSn == clsTeam.m_nSn) == false)
                {
                    _lstTeam.Add(clsTeam);
                }
            }
        }

        public static CGame GetGameInfoByCode(int nCode)
        {
            return _lstGame.Find(value => value.m_nCode == nCode);
        }

        public static CGame GetGameInfoByFixtureID(long nFixtureID)
        {
            return _lstGame.Find(value => value != null && value.m_nFixtureID == nFixtureID);
        }

        public static List<CGame> GetGameList()
        {
            return _lstGame;
        }

        public static int GetGameListCount()
        {
            return _lstGame.Count;
        }

        public static List<CBetting> GetSportsApiBettingList()
        {
            return _lstApiBetting;
        }

        public static int GetSportsApiBettingListCount()
        {
            return _lstApiBetting.Count;
        }

        public static CBetting GetSportsApiBettingBySn(int nSn)
        {
            return _lstApiBetting.Find(value => value.m_nCode == nSn);
        }

        public static List<CBetting> GetSportsApiBettingByBetID(string betid)
        {
            return _lstApiBetting.FindAll(value => value.m_strBetID == betid);
        }

        public static List<CBetting> GetSportsApiBettingByBettingNo(string betting_no)
        {
            return _lstApiBetting.FindAll(value => value.m_strBettingNo == betting_no);
        }

        public static void AddSportsApiBetting(CBetting clsBetting)
        {
            lock(_lstApiBetting)
            {
                if(_lstApiBetting.Exists(value => value.m_nCode == clsBetting.m_nCode) == false)
                {
                    _lstApiBetting.Add(clsBetting);
                } 
            }
        }

        public static void RemoveSportsApiBetting(CBetting clsBetting)
        {
            lock(_lstApiBetting)
            {
                _lstApiBetting.Remove(clsBetting);
            }
        }

        public static CPeriod GetPeriodInfoByCode(int nSports, int nPeriod)
        {
            return _lstPeriod.Find(value => value.m_nPeriod == nPeriod && value.m_nSports == nSports);
        }

        public static void AddGameInfo(CGame clsInfo)
        {
            lock(_lstGame)
            {
                if(_lstGame.Exists(value=>value.m_nFixtureID == clsInfo.m_nFixtureID) == false)
                {
                    _lstGame.Add(clsInfo);
                }
            }
        }

        public static void RemoveGame(CGame clsInfo)
        {
            lock (_lstGame)
            {
                _lstGame.Remove(clsInfo);
            }
        }

        public static void RemoveGameAtFixtureID(long nFixtureID)
        {
            lock (_lstGame)
            {
                _lstGame.RemoveAll(value => value.m_nFixtureID == nFixtureID);
            }
        }

        public static void ShowConsole(string strMsg)
        {
            Console.WriteLine($"{CMyTime.GetMyTimeStr()}        {strMsg}");
        }

        
        public static async Task WriteFixtureLogAsync(string strLog)
        {
            using StreamWriter file = new("WriteFixtureLog.txt", append: true);
            await file.WriteLineAsync(strLog);
        }

        public static async Task WriteScoreLogAsync(string strLog)
        {
            using StreamWriter file = new("WriteScoreLog.txt", append: true);
            await file.WriteLineAsync(strLog);
        }

        public static async Task WriteMarketLogAsync(string strLog)
        {
            using StreamWriter file = new("WriteMarketLog.txt", append: true);
            strLog += $"\n{CMyTime.GetMyTimeStr()}--------------------------------------------------------------------------------------------";
            await file.WriteLineAsync(strLog);
        }

        public static async Task WriteResultLogAsync(string strLog)
        {
            using StreamWriter file = new("WriteResultLog.txt", append: true);
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

        public static void SetBroadcastMiniSocket(CMiniGameServer ws)
        {
            _wsMiniServer = ws;
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
            
            if (_wsMiniServer != null)
            {
                _wsMiniServer.BroadCastPacket(packet);
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

        public static long ParseInt64<T>(T value)
        {
            try
            {
                return Convert.ToInt64(value);
            }
            catch
            {
                return 0;
            }
        }
    }
}
