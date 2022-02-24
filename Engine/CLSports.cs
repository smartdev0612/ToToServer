using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace LSportsServer
{
    public static class CLSports
    {
        private static WebSocket m_wsPrematchLive;
        private static WebSocket m_wsPrematchData;
        private static WebSocket m_wsInplayLive;
        private static WebSocket m_wsInplayData;
        private static WebSocket m_wsSchedule;
        

        public static void Connect()
        {
            /*use*/
            if (CDefine.USE_PREMATCH == "yes")
            {
                m_wsPrematchLive = new WebSocket($"ws://{CDefine.LSPORTS_ADDRESS}:{CDefine.LSPORTS_PREMATCH_LIVE}");
                m_wsPrematchLive.OnOpen += Socket_OnOpen;
                m_wsPrematchLive.OnError += Socket_OnError;
                m_wsPrematchLive.OnClose += Socket_OnClose;
                m_wsPrematchLive.OnMessage += OnRecvPrematchLive;

                m_wsPrematchLive.Connect();
                //new Thread(() => StartThread(CDefine.LSPORTS_PREMATCH)).Start();

                m_wsPrematchData = new WebSocket($"ws://{CDefine.LSPORTS_ADDRESS}:{CDefine.LSPORTS_PREMATCH_DATA}");
                m_wsPrematchData.OnOpen += Socket_OnOpen;
                m_wsPrematchData.OnError += Socket_OnError;
                m_wsPrematchData.OnClose += Socket_OnClose;
                m_wsPrematchData.OnMessage += OnRecvPrematchData;

                m_wsPrematchData.Connect();
            }

            if (CDefine.USE_LIVE == "yes")
            {
                m_wsInplayLive = new WebSocket($"ws://{CDefine.LSPORTS_ADDRESS}:{CDefine.LSPORTS_INPLAY_LIVE}");
                m_wsInplayLive.OnOpen += Socket_OnOpen;
                m_wsInplayLive.OnError += Socket_OnError;
                m_wsInplayLive.OnClose += Socket_OnClose;
                m_wsInplayLive.OnMessage += OnRecvInplayLive;

                m_wsInplayLive.Connect();
                //new Thread(() => StartThread(CDefine.LSPORTS_LIVE)).Start();

                m_wsInplayData = new WebSocket($"ws://{CDefine.LSPORTS_ADDRESS}:{CDefine.LSPORTS_INPLAY_DATA}");
                m_wsInplayData.OnOpen += Socket_OnOpen;
                m_wsInplayData.OnError += Socket_OnError;
                m_wsInplayData.OnClose += Socket_OnClose;
                m_wsInplayData.OnMessage += OnRecvInplayData;

                m_wsInplayData.Connect();
            }

            m_wsSchedule = new WebSocket($"ws://{CDefine.LSPORTS_ADDRESS}:{CDefine.LSPORTS_SCHEDULE}");
            m_wsSchedule.OnOpen += Socket_OnOpen;
            m_wsSchedule.OnError += Socket_OnError;
            m_wsSchedule.OnClose += Socket_OnClose;
            m_wsSchedule.OnMessage += OnRecvSchedule;

            m_wsSchedule.Connect();

            new Thread(() => StartCheckFinished()).Start();
            new Thread(() => StartCheckLive()).Start();
        }

        private static void Socket_OnClose(object sender, CloseEventArgs e)
        {
            (sender as WebSocket).Connect();
        }

        private static void Socket_OnError(object sender, ErrorEventArgs e)
        {
            (sender as WebSocket).Close();
        }

        private static void Socket_OnOpen(object sender, EventArgs e)
        {
            CGlobal.ShowConsole("socket connected!");
        }

        private static void OnRecvPrematchLive(object sender, MessageEventArgs e)
        {
            string strPacket = e.Data.ToString();
            ThreadPool.QueueUserWorkItem(StartPrematchParsingData, strPacket);
        }

        private static void StartPrematchParsingData(object objParam)
        {
            string strPacket = (string)objParam;
            StartParsingData(strPacket, CDefine.LSPORTS_PREMATCH);
        }
       

        private static void OnRecvPrematchData(object sender, MessageEventArgs e)
        {
            string strPacket = e.Data.ToString();
            ThreadPool.QueueUserWorkItem(StartPrematchHttpData, strPacket);
        }

        private static void StartPrematchHttpData(object objParam)
        {
            string strPacket = (string)objParam;
            try
            {
                JObject objMarkets = JObject.Parse(strPacket);
                List<JToken> lstBody = objMarkets["Body"].ToList();
                if (lstBody.Count == 0)
                    return;

                foreach (JToken objBody in lstBody)
                {
                    if (objBody["Fixture"] == null || !objBody["Fixture"].HasValues)
                        continue;

                    if (objBody["Markets"] == null || !objBody["Markets"].HasValues)
                        continue;

                    List<JToken> lstMarket = objBody["Markets"].ToList();

                    if (lstMarket.Count == 0)
                        continue;

                    JToken objFixture = objBody;
                    long nFixtureID = Convert.ToInt64(objFixture["FixtureId"]);
                    //CGlobal.ShowConsole($"ReceiveMarket {nFixtureID} game! *************************");
                    CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                    if (clsGame == null)
                    {
                        clsGame = new CGame(nFixtureID);
                        bool bValid = clsGame.UpdateInfo(objFixture);
                        if(bValid)
                            CGlobal.AddGameInfo(clsGame);
                    }

                    if(clsGame != null)
                    {
                        JToken objScore = objBody;

                        clsGame.UpdateInfo(objFixture);
                        clsGame.UpdateMarket(lstMarket, 0);
                        clsGame.UpdateScore(objScore);

                        clsGame.SetCheckMarket();
                    }
                }
            }
            catch
            {
                return;
            }
        }

        private static void OnRecvInplayLive(object sender, MessageEventArgs e)
        {
            string strPacket = e.Data.ToString();
            ThreadPool.QueueUserWorkItem(StartLiveParsingData, strPacket);
        }

        private static void StartLiveParsingData(object objParam)
        {
            string strPacket = (string)objParam;
            StartParsingData(strPacket, CDefine.LSPORTS_INPLAY);
        }

        private static void OnRecvInplayData(object sender, MessageEventArgs e)
        {
            string strPacket = e.Data.ToString();
            ThreadPool.QueueUserWorkItem(StartInplayHttpData, strPacket);
        }

        private static void StartInplayHttpData(object objParam)
        {
            string strPacket = (string)objParam;

            try
            {
                JObject objMarkets = JObject.Parse(strPacket);

                if (objMarkets["Body"] == null || !objMarkets["Body"].HasValues)
                    return;

                JToken objBody = objMarkets["Body"];
                if (objBody["Events"] == null || !objBody["Events"].HasValues)
                    return;

                List<JToken> lstEvents = objBody["Events"].ToList();


                foreach (JToken objEvent in lstEvents)
                {
                    if (objEvent["Fixture"] == null || !objEvent["Fixture"].HasValues)
                        continue;

                    JToken objFixture = objEvent;
                    long nFixtureID = Convert.ToInt64(objFixture["FixtureId"]);

                    if (objEvent["Markets"] == null || !objEvent["Markets"].HasValues)
                        continue;

                    List<JToken> lstMarket = objEvent["Markets"].ToList();

                    if (lstMarket.Count == 0)
                        continue;

                    JToken objScore = objEvent;

                    CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                    if (clsGame == null)
                    {
                        clsGame = new CGame(nFixtureID);
                        bool bValid = clsGame.UpdateInfo(objFixture);
                        if(bValid)
                            CGlobal.AddGameInfo(clsGame);
                    }

                    if(clsGame != null)
                    {
                        clsGame.UpdateInfo(objFixture);
                        clsGame.UpdateMarket(lstMarket, 3);
                        clsGame.UpdateScore(objScore);

                        clsGame.SetCheckMarket();
                    }
                }
            }
            catch
            {
                return;
            }
        }

        private static void OnRecvSchedule(object sender, MessageEventArgs e)
        {
            string strPacket = e.Data.ToString();

            try
            {
                JToken objPacket = JObject.Parse(strPacket);
                if (objPacket["Body"] == null)
                    return;

                JToken objBody = objPacket["Body"];
                if (objBody["InPlayEvents"] == null || !objBody["InPlayEvents"].HasValues)
                    return;

                List<JToken> list = objBody["InPlayEvents"].ToList();

                string strWhere = "sn = 0";
                foreach (JToken obj in list)
                {
                    long nFixtureID = Convert.ToInt64(obj["FixtureId"]);
                    CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                    if (clsGame == null)
                        continue;

                    strWhere += clsGame.UpdateSchedule();
                }

                if (strWhere != "sn = 0")
                    CEntry.SetGameSchedule(strWhere);
                Thread.Sleep(5000);
            }
            catch (Exception err)
            {
                CGlobal.ShowConsole(err.Message);
            }
        }

        private static void StartParsingData(string strPacket, int nLive)
        {
            try
            {
                JObject objPacket = JObject.Parse(strPacket);
                JToken objHeader = objPacket["Header"];
                int nType = CGlobal.ParseInt(objHeader["Type"]);
                if (nType == 32)
                    return;

                JToken objBody = objPacket["Body"];

                switch (nType)
                {
                    case 1:
                        // CGlobal.ShowConsole("GameFixture");
                        // CGlobal.WriteFixtureLogAsync(strPacket);
                        string strGameListLog = $"******** Game List => " + Convert.ToString(CGlobal.GetGameListCount() + " *********");
                        CGlobal.ShowConsole(strGameListLog);

                        string strBettingListLog = $"******** Betting List => " + Convert.ToString(CGlobal.GetSportsApiBettingListCount() + " *********");
                        CGlobal.ShowConsole(strBettingListLog);

                        GameFixture(objBody, nLive);
                        break;
                    case 2:
                        // CGlobal.ShowConsole("GameScore");
                        // CGlobal.WriteScoreLogAsync(strPacket);
                        GameScore(objBody, nLive);
                        break;
                    case 3:
                        // CGlobal.ShowConsole("GameMarket");
                        GameMarket(objBody, nLive);
                        
                        //if(nLive == CDefine.LSPORTS_INPLAY)
                        //    _ = CGlobal.WriteMarketLogAsync(strPacket);
                        break;
                    case 35:
                        // CGlobal.ShowConsole("GameResult");
                        // CGlobal.WriteResultLogAsync(strPacket);
                        GameResult(objBody, nLive);
                        break;
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
            }
        }

        // 게임시작시간이 현재 시간으로부터 3일후이면 True, 그 이후 경기이면 False.
        private static bool CheckAddGame(JToken objFixture)
        {
            bool bFlag = false;
            JToken objInfo = objFixture["Fixture"];
            DateTime dateTime = CMyTime.ConvertStrToTime(Convert.ToString(objInfo["StartDate"]));
            DateTime startTime = CMyTime.ConvertFromUnixTimestamp(CMyTime.ConvertToUnixTimestamp(dateTime));
            DateTime dtLimit = CMyTime.GetMyTime().AddDays(3);
            if (startTime < dtLimit)
            {
                bFlag = true;
            }

            return bFlag;
        }

        private static void GameFixture(JToken objBody, int nLive)
        {
            List<JToken> lstEvent = objBody["Events"].ToList();
            if (lstEvent.Count == 0)
                return;

            foreach(JToken objFixture in lstEvent)
            {
                long nFixtureID = Convert.ToInt64(objFixture["FixtureId"]);
                CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                if (clsGame == null)
                {
                    clsGame = new CGame(nFixtureID);
                    bool bValid = clsGame.UpdateInfo(objFixture);
                    if(bValid)
                        CGlobal.AddGameInfo(clsGame);
                } 
                
                if(clsGame != null)
                {
                    clsGame.UpdateInfo(objFixture);
                }
            }

            return;
        }

        private static void GameScore(JToken objBody, int nLive)
        {
            List<JToken> lstEvent = objBody["Events"].ToList();
            if (lstEvent.Count == 0)
                return;

            foreach(JToken objEvent in lstEvent)
            {
                long nFixtureID = Convert.ToInt64(objEvent["FixtureId"]);
                CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                if (clsGame == null)
                    continue;

                clsGame.UpdateScore(objEvent);
            }
        }

        private static void GameMarket(JToken objBody, int nLive)
        {
            List<JToken> lstEvent = objBody["Events"].ToList();
            if (lstEvent.Count == 0)
                return;

            foreach(JToken objEvent in lstEvent)
            {
                if (objEvent["Markets"] == null || !objEvent["Markets"].HasValues)
                    continue;

                long nFixtureID = Convert.ToInt64(objEvent["FixtureId"]);
                CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                if (clsGame == null)
                    continue;

                List<JToken> lstMarket = objEvent["Markets"].ToList();
                clsGame.UpdateMarket(lstMarket, nLive);
                
                if(nLive == 2)
                {
                    string strLog = "---------------------------------------------------------------------------\n";
                    strLog += nFixtureID + "\n";
                    strLog += JsonConvert.SerializeObject(lstMarket) + "\n";
                    strLog += "---------------------------------------------------------------------------";
                    //if(nFixtureID == 7888538)
                        //CGlobal.WriteMarketLogAsync(strLog);
                }
            }
        }

        private static void GameResult(JToken objBody, int nLive)
        {
            if (objBody["Events"] == null || !objBody["Events"].HasValues)
                return;

            List<JToken> lstEvent = objBody["Events"].ToList();
            if (lstEvent.Count == 0)
                return;

            foreach(JToken objEvent in lstEvent)
            {
                if (objEvent["Markets"] == null || !objEvent["Markets"].HasValues)
                    continue;

                List<JToken> lstMarket = objEvent["Markets"].ToList();
                if (lstMarket.Count == 0)
                    continue;

                long nFixtureID = Convert.ToInt64(objEvent["FixtureId"]);
                CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                if (clsGame == null)
                    continue;

                foreach (JToken objMarket in lstMarket)
                {
                    clsGame.UpdateResult(objMarket, nLive);
                }
            }
        }

        private static void StartCheckFinished()
        {
            while(true)
            {
                string sql = "SELECT tbl_temp.* FROM (SELECT tb_child.sn AS childSn, tb_child.live AS childLive, tb_child.game_sn, tb_child.special, tb_subchild.* FROM tb_game_betting LEFT JOIN tb_subchild ON tb_game_betting.sub_child_sn = tb_subchild.sn LEFT JOIN tb_child ON tb_subchild.child_sn = tb_child.sn WHERE tb_game_betting.result = 0 AND tb_game_betting.betid <> '' UNION SELECT tb_child.sn AS childSn, tb_child.live AS childLive, tb_child.game_sn, tb_child.special, tb_subchild.* FROM tb_subchild LEFT JOIN tb_child ON tb_subchild.child_sn = tb_child.sn WHERE tb_subchild.status < 3 AND tb_child.special < 5) tbl_temp WHERE tbl_temp.game_sn IS NOT NULL GROUP BY tbl_temp.sn";
                DataRowCollection list = CMySql.GetDataQuery(sql);

                long nOldFixtureId = 0;
                foreach (DataRow info in list)
                {
                    if (info["game_sn"] == System.DBNull.Value)
                        continue;

                    long nFixtureId = Convert.ToInt64(info["game_sn"]);
                    int nChildSn = CGlobal.ParseInt(info["childSn"]);
                    int nChildLive = CGlobal.ParseInt(info["childLive"]);
                    int nLive = CGlobal.ParseInt(info["live"]);
                    int nSpecial = CGlobal.ParseInt(info["special"]);
                    CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureId);
                    if(clsGame == null)
                    {
                        clsGame = new CGame(nFixtureId);
                        clsGame.m_nCode = nChildSn;
                        clsGame.m_nLive = nChildLive;
                        CGlobal.AddGameInfo(clsGame);
                    }

                    int nSubChildSn = CGlobal.ParseInt(info["sn"]);

                    if(nLive < 2)
                    {
                        CBetRate clsRate = clsGame.GetPrematchBetRateList().Find(value => value.m_nCode == nSubChildSn);
                        if (clsRate == null)
                        {
                            clsRate = new CBetRate(clsGame);
                            clsRate.LoadInfo(info);
                            clsGame.AddPrematchBetRate(clsRate);
                        }
                    }
                    else
                    {
                        CBetRate clsRate = clsGame.GetLiveBetRateList().Find(value => value.m_nCode == nSubChildSn);
                        if (clsRate == null)
                        {
                            clsRate = new CBetRate(clsGame);
                            clsRate.LoadInfo(info);
                            clsGame.AddLiveBetRate(clsRate);
                        }
                    }

                    if(nOldFixtureId != nFixtureId)
                    {
                        if(nSpecial != 3)
                        {
                            GetGameInfoFromApi(nFixtureId);
                            nOldFixtureId = nFixtureId;
                        }
                    }
                }

                Thread.Sleep(10 * 1000);
            }
        }

        private static void StartCheckLive()
        {
            while(true)
            {
                List<CGame> lstGame = CGlobal.GetGameList();
                lstGame = lstGame.FindAll(value => value != null && value.m_nStatus >= 2).ToList();
                foreach (CGame clsGame in lstGame)
                {
                    if (clsGame.m_nSpecial != 3)
                    {
                        GetGameInfoFromApi(clsGame.m_nFixtureID);
                        Thread.Sleep(1000);
                    }
                }

                try
                {
                    lock (lstGame)
                    {
                        lstGame.RemoveAll(value => value != null && value.IsFinishGame() && value.m_strDate != null && value.m_strDate != "" && value.GetGameDateTime() < CMyTime.GetMyTime().AddDays(-1));
                    }
                }
                catch (Exception err)
                {
                    CGlobal.ShowConsole(err.Message);
                }
            }
        }

        private static void GetGameInfoFromApi(long nFixtureId)
        {
            CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureId);
            if (clsGame == null)
            {
                clsGame = new CGame(nFixtureId);
                CGlobal.AddGameInfo(clsGame);
            }

            if(clsGame.CheckLive())
            {
                //경기가 라이브라면 프리매치 라이브결과를 다 확인해주어야 한다.
                GetInPlayInfoFromApi(clsGame.m_nFixtureID);
            }

            string strReq = $"http://{CDefine.LSPORTS_ADDRESS}:{CDefine.LSPORTS_HTTP_PORT}/api/prematch/{nFixtureId}";
            string strPacket = CHttp.GetResponseString(strReq);
        }

        private static void GetInPlayInfoFromApi(long nFixtureId)
        {
            string strReq = $"http://{CDefine.LSPORTS_ADDRESS}:{CDefine.LSPORTS_HTTP_PORT}/api/inplay/{nFixtureId}";
            string strPacket = CHttp.GetResponseString(strReq);
        }

        public static void LoadGameInfoToDB()
        {
            CGlobal.ShowConsole("Start Loading GameInfo...");

            DataRowCollection list = CEntry.SelectGame();
            foreach (DataRow info in list)
            {
                long nFixtureID = Convert.ToInt64(info["game_sn"]);
                int nSpecial = CGlobal.ParseInt(info["special"]);
                CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                if(clsGame == null)
                {
                    clsGame = new CGame();
                    clsGame.LoadInfo(info);
                    CGlobal.AddGameInfo(clsGame);
                    if(nSpecial == 3)
                    {
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
                else
                {
                    clsGame.LoadInfo(info);
                    if (nSpecial == 3)
                    {
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

                if (nSpecial != 3)
                {
                    if (clsGame.CheckGame())
                    {
                        GetGameInfoFromApi(clsGame.m_nFixtureID);
                        //CGlobal.ShowConsole($"Loading {clsGame.m_nFixtureID} game! *************************");
                        Thread.Sleep(300);
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            while(true)
            {
                try
                {
                    LoadAvailableFixtures();
                    List<CGame> lstGame = CGlobal.GetGameList();
                    lstGame = lstGame.FindAll(value => value != null && value.CheckMarket() == false).ToList();

                    foreach (CGame clsInfo in lstGame)
                    {
                        if (clsInfo == null)
                            continue;

                        if (clsInfo.m_nSpecial != 3)
                        {
                            GetGameInfoFromApi(clsInfo.m_nFixtureID);
                            //CGlobal.ShowConsole($"CheckMarket {clsInfo.m_nFixtureID} game! *************************");
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch(Exception err)
                {
                    CGlobal.ShowConsole(err.Message);
                }
                

                Thread.Sleep(1000);
            }
        }

        // 현재시간으로부터 3일이내의 경기들만 가져옴.
        public static void LoadAvailableFixtures()
        {
            List<long> lstFixtureID = new List<long>();
            int nFromDate = (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            int nToDate = nFromDate + 3 * 24 * 3600;
            string strReq = $"http://{CDefine.LSPORTS_ADDRESS}:{CDefine.LSPORTS_HTTP_PORT}/api/fixtures?nFromDate={nFromDate}&nToDate={nToDate}";
            string strPacket = CHttp.GetResponseString(strReq);
            if (string.IsNullOrEmpty(strPacket) == true)
            {
                LoadAvailableFixtures();
                Thread.Sleep(1000);
            }

            JObject objPacket = JObject.Parse(strPacket);
            List<JToken> lstFixtures = objPacket["Body"].ToList();
            if (lstFixtures.Count > 0)
            {
                foreach (JToken objFixture in lstFixtures)
                {
                    long nFixtureID = CGlobal.ParseInt64(objFixture["FixtureId"]);
                    CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                    if (clsGame == null)
                    {
                        clsGame = new CGame(nFixtureID);
                        bool bValid = clsGame.UpdateInfo(objFixture);
                        if (bValid)
                            CGlobal.AddGameInfo(clsGame);
                    }

                    if (clsGame != null)
                    {
                        clsGame.UpdateInfo(objFixture);
                    }
                }

            }
        }
    }
}
