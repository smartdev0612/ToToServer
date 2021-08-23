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
        //private static WebSocket m_wsData;
        private static WebSocket m_wsPrematch;
        private static WebSocket m_wsLive;
        

        public static void Connect()
        {
            // API로부터 리그정보로드 
            // CLeague clsLeague = new CLeague();
            // clsLeague.SaveLeagueInfo(687890);

            /*use*/
            if (CDefine.USE_PREMATCH == "yes")
            {
                m_wsPrematch = new WebSocket($"ws://{CDefine.ADDR_SEVER}:{CDefine.SOCKET_PREMATCH}");
                m_wsPrematch.OnOpen += Prematch_OnOpen;
                m_wsPrematch.OnError += Prematch_OnError;
                m_wsPrematch.OnClose += Prematch_OnClose;
                m_wsPrematch.OnMessage += Prematch_OnMessage;

                m_wsPrematch.Connect();
                //new Thread(() => StartThread(CDefine.LSPORTS_PREMATCH)).Start();
            }

            if (CDefine.USE_LIVE == "yes")
            {
                m_wsLive = new WebSocket($"ws://{CDefine.ADDR_SEVER}:{CDefine.SOCKET_LIVE}");
                m_wsLive.OnOpen += Live_OnOpen;
                m_wsLive.OnError += Prematch_OnError;
                m_wsLive.OnClose += Prematch_OnClose;
                m_wsLive.OnMessage += Live_OnMessage;

                m_wsLive.Connect();
                //new Thread(() => StartThread(CDefine.LSPORTS_LIVE)).Start();
            }

            new Thread(() => StartCheckFinished()).Start();
            new Thread(() => StartCheckSchedule()).Start();
            new Thread(() => StartCheckLive()).Start();
        }

        private static void Prematch_OnClose(object sender, CloseEventArgs e)
        {
            (sender as WebSocket).Connect();
        }

        private static void Prematch_OnError(object sender, ErrorEventArgs e)
        {
            (sender as WebSocket).Close();
        }

        private static void Prematch_OnOpen(object sender, EventArgs e)
        {
            CGlobal.ShowConsole("Prematch socket connected!");
        }

        private static void Live_OnOpen(object sender, EventArgs e)
        {
            CGlobal.ShowConsole("Live socket connected!");
        }

        //private static void Data_OnOpen(object sender, EventArgs e)
        //{
        //    CGlobal.ShowConsole("Data socket connected!");
        //}

        private static void Prematch_OnMessage(object sender, MessageEventArgs e)
        {
            string strPacket = e.Data.ToString();
            //CGlobal.AddLSportsPacket(CDefine.LSPORTS_PREMATCH, strPacket);
            StartParsingData(strPacket, CDefine.LSPORTS_PREMATCH);
        }

        private static void Live_OnMessage(object sender, MessageEventArgs e)
        {
            string strPacket = e.Data.ToString();
            //CGlobal.AddLSportsPacket(CDefine.LSPORTS_LIVE, strPacket);
            StartParsingData(strPacket, CDefine.LSPORTS_LIVE);
        }

        //private static void Data_OnMessage(object sender, MessageEventArgs e)
        //{
        //    string strPacket = e.Data.ToString();
        //    CGlobal.AddLSportsPacket(CDefine.LSPORTS_DATA, strPacket);
        //}

        private static void StartThread(int nFlag)
        {
            while (true)
            {
                string strPacket = CGlobal.GetLSportsPacket(nFlag);
                if (strPacket == string.Empty || strPacket == null)
                {
                    Thread.Sleep(50);
                    continue;
                }

                if(nFlag == CDefine.LSPORTS_DATA)
                {
                    StartParsingData(strPacket);
                }
                else
                {
                    StartParsingData(strPacket, nFlag);
                }
            }
        }

        private static void StartParsingData(string strPacket)
        {
            try
            {
                CDataPacket clsPacket = JsonConvert.DeserializeObject<CDataPacket>(strPacket);

                JObject objMarkets = JObject.Parse(clsPacket.Markets);
                List<JToken> lstBody = objMarkets["Body"].ToList();
                if (lstBody.Count == 0)
                    return;

                foreach(JToken objBody in lstBody)
                {
                    List<JToken> lstMarket = objBody["Markets"].ToList();
                    if (lstMarket.Count == 0)
                        return;

                    JToken objFixture = JObject.Parse(clsPacket.Fixture);
                    long nFixtureID = Convert.ToInt64(objFixture["FixtureId"]);
                    CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                    if (clsGame == null)
                    {
                        clsGame = new CGame(nFixtureID);
                        clsGame.UpdateInfo(objFixture);

                        if (clsGame.m_nCode > 0 && clsGame.m_nStatus < 3)
                        {
                            string strTime = $"{clsGame.m_strDate} {clsGame.m_strHour}:{clsGame.m_strMin}:00";
                            DateTime dtTime = CMyTime.ConvertStrToTime(strTime);
                            DateTime dtLimit = CMyTime.GetMyTime().AddDays(3);
                            if (dtTime < dtLimit)
                            {
                                clsGame.UpdateMarket(lstMarket, 0);
                                clsGame.UpdateScore(objFixture);
                                //if (clsGame.GetBetRateCount() > 0)
                                CGlobal.AddGameInfo(clsGame);
                            }
                        }
                    }
                    else
                    {
                        clsGame.UpdateInfo(objFixture);
                        clsGame.UpdateMarket(lstMarket, 0);
                        clsGame.UpdateScore(objFixture);
                    }
                }
                

            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        private static void StartParsingData(string strPacket, int nFlag)
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
                        // CGlobal.WriteLogAsync(strPacket);
                        GameFixture(objBody, nFlag);
                        break;
                    case 2:
                        // CGlobal.ShowConsole("GameScore");
                        GameScore(objBody, nFlag);
                        break;
                    case 3:
                        // CGlobal.ShowConsole("GameMarket");
                        GameMarket(objBody, nFlag);
                        break;
                    case 35:
                        // CGlobal.ShowConsole("GameResult");
                        GameResult(objBody, nFlag);
                        break;
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
            }
        }

        private static void GameFixture(JToken objBody, int nLive)
        {
            List<JToken> lstEvent = objBody["Events"].ToList();
            if (lstEvent.Count == 0)
            {
                return;
            }

            foreach(JToken objFixture in lstEvent)
            {
                long nFixtureID = Convert.ToInt64(objFixture["FixtureId"]);
                CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                if (clsGame == null)
                {
                    clsGame = new CGame(nFixtureID);
                    CGlobal.AddGameInfo(clsGame);
                }
                clsGame.UpdateInfo(objFixture);
            }

            return;
        }

        private static void GameScore(JToken objBody, int nLive)
        {
            List<JToken> lstEvent = objBody["Events"].ToList();
            if (lstEvent.Count == 0)
            {
                return;
            }

            foreach(JToken objEvent in lstEvent)
            {
                long nFixtureID = Convert.ToInt64(objEvent["FixtureId"]);
                CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                if (clsGame == null)
                {
                    clsGame = new CGame(nFixtureID);
                    CGlobal.AddGameInfo(clsGame);
                }
                clsGame.UpdateScore(objEvent);
            }
        }

        private static void GameMarket(JToken objBody, int nLive)
        {
            List<JToken> lstEvent = objBody["Events"].ToList();
            if (lstEvent.Count == 0)
            {
                return;
            }

            foreach(JToken objEvent in lstEvent)
            {
                long nFixtureID = Convert.ToInt64(objEvent["FixtureId"]);
                CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureID);
                if (clsGame == null)
                {
                    clsGame = new CGame(nFixtureID);
                    CGlobal.AddGameInfo(clsGame);
                }

                if (objEvent["Markets"] == null || !objEvent["Markets"].HasValues)
                {
                    return;
                }
                List<JToken> lstMarket = objEvent["Markets"].ToList();
                clsGame.UpdateMarket(lstMarket, nLive);
                
                if(nLive == 2)
                {
                    string strLog = "---------------------------------------------------------------------------\n";
                    strLog += nFixtureID + "\n";
                    strLog += JsonConvert.SerializeObject(lstMarket) + "\n";
                    strLog += "---------------------------------------------------------------------------";

                    // CGlobal.WriteLogAsync(strLog);
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
                {
                    clsGame = new CGame(nFixtureID);
                    CGlobal.AddGameInfo(clsGame);
                }

                foreach(JToken objMarket in lstMarket)
                {
                    clsGame.UpdateResult(objMarket, nLive);
                }
            }
        }

        private static void StartCheckFinished()
        {
            while(true)
            {
                string sql = "SELECT tbl_temp.* FROM (SELECT tb_child.sn AS childSn, tb_child.live AS childLive, tb_child.game_sn, tb_subchild.* FROM tb_total_betting LEFT JOIN tb_subchild ON tb_total_betting.sub_child_sn = tb_subchild.sn LEFT JOIN tb_child ON tb_subchild.child_sn = tb_child.sn WHERE tb_total_betting.result = 0 AND tb_total_betting.betid <> '' UNION SELECT tb_child.sn AS childSn, tb_child.live AS childLive, tb_child.game_sn, tb_subchild.* FROM tb_subchild LEFT JOIN tb_child ON tb_subchild.child_sn = tb_child.sn WHERE tb_subchild.status < 3 AND tb_child.special < 5) tbl_temp WHERE tbl_temp.game_sn IS NOT NULL GROUP BY tbl_temp.sn";
                DataRowCollection list = CMySql.GetDataQuery(sql);

                long nOldFixtureId = 0;
                List<long> lstFixtureID = new List<long>();
                foreach (DataRow info in list)
                {
                    if (info["game_sn"] == System.DBNull.Value)
                        continue;

                    long nFixtureId = Convert.ToInt64(info["game_sn"]);
                    int nChildSn = CGlobal.ParseInt(info["childSn"]);
                    int nChildLive = CGlobal.ParseInt(info["childLive"]);
                    int nLive = CGlobal.ParseInt(info["live"]);
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
                            clsGame.GetPrematchBetRateList().Add(clsRate);
                        }
                    }
                    else
                    {
                        CBetRate clsRate = clsGame.GetLiveBetRateList().Find(value => value.m_nCode == nSubChildSn);
                        if (clsRate == null)
                        {
                            clsRate = new CBetRate(clsGame);
                            clsRate.LoadInfo(info);
                            clsGame.GetLiveBetRateList().Add(clsRate);
                        }
                    }

                    if(nOldFixtureId != nFixtureId)
                    {
                        lstFixtureID.Add(nFixtureId);
                        nOldFixtureId = nFixtureId;
                    }
                }

                List<CGame> lstGame = CGlobal.GetGameList().FindAll(value => value.m_nStatus >= 2);
                foreach(CGame clsGame in lstGame)
                {
                    if (lstFixtureID.Exists(value => value == clsGame.m_nFixtureID) == false)
                        lstFixtureID.Add(clsGame.m_nFixtureID);
                }


                foreach (long nFixtureID in lstFixtureID)
                {
                    GetGameInfoFromApi(nFixtureID);
                }

                Thread.Sleep(30 * 1000);
            }
        }

        private static void StartCheckLive()
        {
            while(true)
            {
                List<CGame> lstSubGame = CGlobal.GetGameList();
                List<CGame> lstGame = lstSubGame.FindAll(value => value.CheckLive());
                foreach(CGame clsGame in lstGame)
                {
                    GetInPlayInfoFromApi(clsGame.m_nFixtureID);
                }

                Thread.Sleep(3000);
            }
        }

        private static void StartCheckSchedule()
        {
            List<CSports> lstSports = CGlobal.GetSportsList().FindAll(value => value.m_nUse == 1);

            while(true)
            {
                foreach(CSports info in lstSports)
                {
                    try
                    {
                        string strUrl = $"http://175.125.95.163:3034/LSports/playSchedule?{info.m_nCode}";
                        string str = CHttp.GetResponseString(strUrl);

                        JToken objPacket = JObject.Parse(str);
                        if (objPacket["Body"] == null)
                            continue;

                        JToken objBody = objPacket["Body"];
                        if (objBody["InPlayEvents"] == null || !objBody["InPlayEvents"].HasValues)
                            continue;

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
                    catch(Exception err)
                    {
                        CGlobal.ShowConsole(err.Message);
                    }
                   
                }

                try
                {
                    List<CGame> clsGameList = CGlobal.GetGameList();
                    if(clsGameList != null)
                        clsGameList.RemoveAll(value => value.IsFinishGame() && value.GetGameDateTime() < CMyTime.GetMyTime().AddDays(-1));
                }
                catch (Exception err)
                {
                    CGlobal.ShowConsole(err.Message);
                }
               
                Thread.Sleep(1000 * 60);
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

            string strReq = $"http://{CDefine.ADDR_SEVER}:{CDefine.HTTP_PORT}/LSports/getGameEvent?{nFixtureId}";
            string strPacket = CHttp.GetResponseString(strReq);
            try
            {
                JObject objMarkets = JObject.Parse(strPacket);
                List<JToken> lstBody = objMarkets["Body"].ToList();
                if (lstBody.Count == 0)
                {
                    return;
                }

                foreach(JToken objBody in lstBody)
                {
                    if (objBody["Fixture"] == null || !objBody["Fixture"].HasValues)
                    {
                        continue;
                    }

                    JToken objFixture = objBody;

                    if (objBody["Markets"] == null || !objBody["Markets"].HasValues)
                    {
                        continue;
                    }

                    List<JToken> lstMarket = objBody["Markets"].ToList();

                    if (lstMarket.Count == 0)
                    {
                        continue;
                    }

                    JToken objScore = objBody;

                    clsGame.UpdateInfo(objFixture);
                    clsGame.UpdateMarket(lstMarket, 0);
                    clsGame.UpdateScore(objScore);

                    Thread.Sleep(200);
                }
            }
            catch
            {
                return;
            }

        }

        public static void GetInPlayInfoFromApi(long nFixtureId)
        {
            string strReq = $"http://{CDefine.ADDR_SEVER}:{CDefine.HTTP_PORT}/LSports/inplay?{nFixtureId}";
            string strPacket = CHttp.GetResponseString(strReq);
            try
            {
                JObject objMarkets = JObject.Parse(strPacket);

                if (objMarkets["Body"] == null || !objMarkets["Body"].HasValues)
                {
                    return;
                }
                JToken objBody = objMarkets["Body"];
                if(objBody["Events"] == null || !objBody["Events"].HasValues)
                {
                    return;
                }

                List<JToken> lstEvents = objBody["Events"].ToList();


                foreach (JToken objEvent in lstEvents)
                {
                    if (objEvent["Fixture"] == null || !objEvent["Fixture"].HasValues)
                    {
                        return;
                    }

                    JToken objFixture = objEvent;

                    if (objEvent["Markets"] == null || !objEvent["Markets"].HasValues)
                    {
                        return;
                    }

                    List<JToken> lstMarket = objEvent["Markets"].ToList();

                    if (lstMarket.Count == 0)
                    {
                        return;
                    }

                    JToken objScore = objEvent;

                    CGame clsGame = CGlobal.GetGameInfoByFixtureID(nFixtureId);
                    if (clsGame == null)
                    {
                        clsGame = new CGame(nFixtureId);
                        CGlobal.AddGameInfo(clsGame);
                    }

                    clsGame.UpdateInfo(objFixture);
                    clsGame.UpdateMarket(lstMarket, 3);
                    clsGame.UpdateScore(objScore);

                    Thread.Sleep(200);
                }
            }
            catch
            {
                return;
            }

        }

        public static void LoadGameInfoToDB()
        {
            CGlobal.ShowConsole("Start Loading GameInfo...");

            DataRowCollection list = CEntry.SelectGame();
            foreach (DataRow info in list)
            {
                CGame clsInfo = new CGame();
                clsInfo.LoadInfo(info);
                if(clsInfo.CheckGame())
                {
                    CGlobal.AddGameInfo(clsInfo);

                    GetGameInfoFromApi(clsInfo.m_nFixtureID);
                    CGlobal.ShowConsole($"Loading {clsInfo.m_nFixtureID} game! *************************");

                }
                else
                {
                    continue;
                }
                    
            }

            while(true)
            {
                List<CGame> lstInfo = CGlobal.GetGameList().FindAll(value => value.CheckGame() == false);

                foreach(CGame clsInfo in lstInfo)
                {
                    GetGameInfoFromApi(clsInfo.m_nFixtureID);
                    CGlobal.ShowConsole($"Loading {clsInfo.m_nFixtureID} game! *************************");

                    Thread.Sleep(2000);
                }

                Thread.Sleep(1000);
            }
        }
    }
}
