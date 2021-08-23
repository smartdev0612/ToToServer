using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CGame : MGame, ILSports
    {
        public bool m_bCheck;
        private List<CBetRate> m_lstPrematchBetRate;
        private List<CBetRate> m_lstLiveBetRate;
        private List<CScore> m_lstScore;
        public string m_strGameTime { get { return $"{m_strDate} {m_strHour}:{m_strMin}"; } }

        public CGame()
        {
            m_lstPrematchBetRate = new List<CBetRate>();
            m_lstLiveBetRate = new List<CBetRate>();
            m_lstScore = new List<CScore>();

            m_nSpecial = 1;
        }

        public CGame(long nFixtureID)
        {
            m_nFixtureID = nFixtureID;
            m_lstPrematchBetRate = new List<CBetRate>();
            m_lstLiveBetRate = new List<CBetRate>();
            m_lstScore = new List<CScore>();

            m_nSpecial = 1;
        }

        public MBase GetModel()
        {
            return this as MGame;
        }

        public DateTime GetGameDateTime()
        {
            return CMyTime.ConvertStrToTime(m_strGameTime + ":00");
        }

        public void LoadInfo(DataRow info)
        {
            m_nCode = CGlobal.ParseInt(info["sn"]);
            m_nFixtureID = Convert.ToInt64(info["game_sn"]);
            m_nSports = CGlobal.ParseInt(info["sport_id"]);
            m_nLeague = CGlobal.ParseInt(info["league_sn"]);
            CLeague clsLeague = CGlobal.GetLeagueInfoByCode(m_nLeague);
            if (clsLeague == null)
                return;
            m_nCountry = clsLeague.m_nCountry;
            m_strDate = Convert.ToString(info["gameDate"]);
            m_strHour = Convert.ToString(info["gameHour"]);
            m_strMin = Convert.ToString(info["gameTime"]);
            m_nHomeTeam = CGlobal.ParseInt(info["home_team_id"]);
            m_nAwayTeam = CGlobal.ParseInt(info["away_team_id"]);
            m_nPeriod = CGlobal.ParseInt(info["game_period"]);
            m_nStatus = CGlobal.ParseInt(info["status"]);
            m_nHomeScore = CGlobal.ParseInt(info["home_score"]);
            m_nAwayScore = CGlobal.ParseInt(info["away_score"]);
            m_strWinTeam = Convert.ToString(info["win_team"]);
            m_nSpecial = CGlobal.ParseInt(info["special"]);
            m_nSpecified = CGlobal.ParseInt(info["is_specified_special"]);
            m_nType = CGlobal.ParseInt(info["type"]);
            m_nLive = CGlobal.ParseInt(info["live"]);

            m_bCheck = true;
        }

        public bool IsFinishGame()
        {
            if (m_nStatus == 3 || m_nStatus == 4 || m_nStatus == 5 || m_nStatus == 6 || m_nStatus == 7)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<CBetRate> GetPrematchBetRateList()
        {
            return m_lstPrematchBetRate;
        }

        public List<CBetRate> GetLiveBetRateList()
        {
            return m_lstLiveBetRate;
        }

        public void SetLiveFlag()
        {
            if(this.CheckLive() == false)
            {
                new Thread(() => CLSports.GetInPlayInfoFromApi(this.m_nFixtureID)).Start();
                this.m_nLive = 2;
                CEntry.SaveGameInfoToDB(this);
            }

            if (this.m_nStatus < 2)
            {
                this.m_nStatus = 2;
            }

            CBetRate clsLiveRate = m_lstLiveBetRate.Find(value => value.CheckWinDrawLose(this.m_nSports));
            if(clsLiveRate == null)
            {
                //라이브상태일때 승무패가 없다면 프리매치배당을 복사해주어야 한다.
                clsLiveRate = new CBetRate(this);
                CBetRate clsPreRate = m_lstLiveBetRate.Find(value => value.CheckWinDrawLose(this.m_nSports));
                if(clsPreRate != null)
                {
                    clsLiveRate.CopyObject(clsPreRate);
                    clsLiveRate.m_nStatus = 2;
                }
                m_lstLiveBetRate.Add(clsLiveRate);
            }

            if(clsLiveRate.m_nLive < 2)
            {
                clsLiveRate.m_nLive = 2;
                clsLiveRate.m_nStatus = 2;
            }
                
        }

        public bool CheckLive()
        {
            return m_nLive == 2;
        }

        public CBetRate GetPreRateInfoByBetID(string strBetID)
        {
            CBetRate betRate = m_lstPrematchBetRate.Find(value => value.m_strHBetCode == strBetID || value.m_strDBetCode == strBetID || value.m_strABetCode == strBetID);
            return betRate;
        }

        public CBetRate GetLiveRateInfoByBetID(string strBetID)
        {
            CBetRate betRate = m_lstLiveBetRate.Find(value => value.m_strHBetCode == strBetID || value.m_strDBetCode == strBetID || value.m_strABetCode == strBetID);
            return betRate;
        }

        public int GetPreRateCount()
        {
            return m_lstPrematchBetRate.Count;
        }

        public string UpdateSchedule()
        {
            if(m_nLive == 0)
            {
                m_nLive = 0;
                return $" or game_sn = '{this.m_nFixtureID}'";
            }
            else
            {
                return string.Empty;
            }
        }

        private void CheckFinishGame()
        {
            if (this.IsFinishGame())
            {
                m_strWinTeam = "Draw";
                if (m_nHomeScore > m_nAwayScore)
                {
                    m_strWinTeam = "Home";
                }
                else if (m_nHomeScore < m_nAwayScore)
                {
                    m_strWinTeam = "Away";
                }

                //결과처리가 다 되였다.
                CEntry.SaveGameInfoToDB(this);

                foreach(CScore score in m_lstScore)
                {
                    CEntry.SaveScoreInfoToDB(score);
                }

                if(this.IsFinishAllRate())
                {
                    CGlobal.RemoveGame(this);
                }
            }
        }

        public bool IsFinishAllRate()
        {
            if (!m_lstPrematchBetRate.Exists(value => value.IsFinished() == false && value.m_nCode > 0) && !m_lstLiveBetRate.Exists(value => value.IsFinished() == false && value.m_nCode > 0))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void UpdateInfo(JToken objInfo)
        {
            objInfo = objInfo["Fixture"];
            m_nSports = CGlobal.ParseInt(objInfo.SelectToken("Sport").SelectToken("Id"));
            CSports clsSports = CGlobal.GetSportsInfoByCode(m_nSports);
            if (clsSports == null || clsSports.m_nUse == 0)
            {
                CGlobal.RemoveGame(this);
                return;
            }
            m_nLeague = CGlobal.ParseInt(objInfo.SelectToken("League").SelectToken("Id"));
            CLeague clsLeague = CGlobal.GetLeagueInfoByCode(m_nLeague);
            if (clsLeague == null || clsLeague.m_nUse == 0)
            {
                CGlobal.RemoveGame(this);
                return;
            }

            CCountry clsCountry = CGlobal.GetCountryInfoByCode(clsLeague.m_nCountry);
            if (clsCountry == null || clsCountry.m_nUse == 0)
            {
                CGlobal.RemoveGame(this);
                return;
            }
            m_nCountry = clsCountry.m_nCode;

            List<JToken> lstParticipant = objInfo["Participants"].ToList();
            if (lstParticipant.Count != 2)
            {
                return;
            }
            if (CGlobal.ParseInt(lstParticipant[0]["Position"]) == 1)
            {
                m_nHomeTeam = CGlobal.ParseInt(lstParticipant[0]["Id"]);
                m_nAwayTeam = CGlobal.ParseInt(lstParticipant[1]["Id"]);
            }
            else
            {
                m_nHomeTeam = CGlobal.ParseInt(lstParticipant[1]["Id"]);
                m_nAwayTeam = CGlobal.ParseInt(lstParticipant[0]["Id"]);
            }

            CTeam clsHomeTeam = CGlobal.GetTeamInfoByCode(m_nHomeTeam);
            if(clsHomeTeam == null)
            {
                CGlobal.RemoveGame(this);
                return;
            }
            CTeam clsAwayTeam = CGlobal.GetTeamInfoByCode(m_nAwayTeam);
            if (clsAwayTeam == null)
            {
                CGlobal.RemoveGame(this);
                return;
            }


            DateTime dateTime = CMyTime.ConvertStrToTime(Convert.ToString(objInfo["StartDate"]));
            DateTime startTime = CMyTime.ConvertFromUnixTimestamp(CMyTime.ConvertToUnixTimestamp(dateTime));
            m_strDate = startTime.ToString("yyyy-MM-dd");
            m_strHour = startTime.ToString("HH");
            m_strMin = startTime.ToString("mm");

            int nStatus = CGlobal.ParseInt(objInfo["Status"]);
            if (nStatus == 9)
            {
                //bool bCheck = CheckLiveGame();
                //if(bCheck)
                //{
                //    SetLiveFlag();
                //}
                //else
                //{
                //    nStatus = 1;
                //}
                //nStatus = 1;
            }
            m_nStatus = nStatus;

            if(m_nCode == 0)
            {
                m_nCode = CEntry.InsertGameToDB(this);
            }

            m_bCheck = true;
            CheckFinishGame();
        }
        
        public bool CheckLiveGame()
        {
            string sql = $"SELECT live FROM tb_child WHERE sn = {m_nCode}";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            if(list.Count > 0)
            {
                int nLive = CGlobal.ParseInt(list[0]["live"]);
                return nLive == 2;
            }
            else
            {
                return false;
            }
        }

        public bool CheckGame()
        {
            return m_bCheck;
        }

        public void UpdateScore(JToken objInfo)
        {
            JToken objLiveScore = objInfo["Livescore"];
            if (objLiveScore == null || !objLiveScore.HasValues)
                return;

            JToken objScoreboard = objLiveScore["Scoreboard"];
            if (objScoreboard == null || !objScoreboard.HasValues)
                return;

            int nStatus = CGlobal.ParseInt(objScoreboard["Status"]);
            if (objScoreboard["Results"] == null || !objScoreboard["Results"].HasValues)
                return;

            List<JToken> lstResult = objScoreboard["Results"].ToList();
            if (lstResult.Count < 2)
                return;

            m_nPeriod = CGlobal.ParseInt(objScoreboard["CurrentPeriod"]);
            if (CGlobal.ParseInt(lstResult[0]["Position"]) == 1)
            {
                m_nHomeScore = CGlobal.ParseInt(lstResult[0]["Value"]);
                m_nAwayScore = CGlobal.ParseInt(lstResult[1]["Value"]);
            }
            else
            {
                m_nHomeScore = CGlobal.ParseInt(lstResult[1]["Value"]);
                m_nAwayScore = CGlobal.ParseInt(lstResult[0]["Value"]);
            }

            CEntry.SaveScoreToDB(this);


            if (objLiveScore["Periods"] == null || !objLiveScore["Periods"].HasValues)
                return;

            List<JToken> lstPeriod = objLiveScore["Periods"].ToList();
            for (int i = 0; i < lstPeriod.Count; i++)
            {
                if (lstPeriod[i]["Results"] == null || !lstPeriod[i]["Results"].HasValues)
                {
                    continue;
                }
                lstResult = lstPeriod[i]["Results"].ToList();
                if (lstResult.Count < 2)
                {
                    continue;
                }
                int nPeriod = CGlobal.ParseInt(lstPeriod[i]["Type"]);
                CScore scoreInfo = m_lstScore.Find(value => value.m_nPeriod == nPeriod);
                if (scoreInfo == null)
                {
                    scoreInfo = new CScore(m_nFixtureID);
                    scoreInfo.m_nPeriod = nPeriod;
                    m_lstScore.Add(scoreInfo);
                }

                if (CGlobal.ParseInt(lstResult[0]["Position"]) == 1)
                {
                    scoreInfo.m_nHomeScore = CGlobal.ParseInt(lstResult[0]["Value"]);
                    scoreInfo.m_nAwayScore = CGlobal.ParseInt(lstResult[1]["Value"]);
                }
                else
                {
                    scoreInfo.m_nHomeScore = CGlobal.ParseInt(lstResult[1]["Value"]);
                    scoreInfo.m_nAwayScore = CGlobal.ParseInt(lstResult[0]["Value"]);
                }

                scoreInfo.m_nIsFinished = Convert.ToBoolean(lstPeriod[i]["IsFinished"]) ? 1 : 0;
                scoreInfo.m_nIsConfirmed = Convert.ToBoolean(lstPeriod[i]["IsConfirmed"]) ? 1 : 0;
            }

            CheckFinishGame();
        }

        public void UpdateMarket(List<JToken> lstMarket, int nLive)
        {
            List<int> lstMarketID = new List<int>();
            List<string> lstStrApi = new List<string>();
            List<CBetRate> lstRate = this.m_lstLiveBetRate.ToList();

            foreach (JToken objMarket in lstMarket)
            {
                lstStrApi.Clear();

                int nMarketID = CGlobal.ParseInt(objMarket["Id"]);
                lstMarketID.Add(nMarketID);
                if (CGlobal.CheckMarketID(nMarketID))
                {
                    UpdateBetRate(objMarket, nLive, lstStrApi);
                    lstRate.FindAll(value => value.m_nMarket == nMarketID && lstStrApi.Exists(val => val == value.m_strApi) == false).ForEach(value => value.m_nStatus = 2);
                }
            }

            if(nLive == 3)
            {
                lstRate = lstRate.FindAll(value => lstMarketID.Exists(val => val == value.m_nMarket) == false);
                lstRate.ForEach(value => value.m_nStatus = 2);
            }

            CheckFinishGame();
        }

        public void UpdateResult(JToken objInfo, int nLive)
        {
            int nMarketId = CGlobal.ParseInt(objInfo["Id"]);
            if (CGlobal.CheckMarketID(nMarketId))
            {
                UpdateBetRate(objInfo, nLive);
            }

            CheckFinishGame();
        }

        private void UpdateBetRate(JToken objInfo, int nLive, List<string> lstStrApi = null)
        {
            if (m_bCheck == false)
                return;

            if (objInfo["Providers"] == null || !objInfo["Providers"].HasValues)
                return;

            List<JToken> lstProvider = objInfo["Providers"].ToList();
            if (lstProvider.Count == 0)
                return;

            int nMarketID = CGlobal.ParseInt(objInfo["Id"]);

            foreach (JToken objProvider in lstProvider)
            {
                int nProviderCnt = lstProvider.Count;
                string strApi = Convert.ToString(objProvider["Name"]);
                if(lstStrApi != null)
                {
                    lstStrApi.Add(strApi);
                }

                CMarket clsMarket = CGlobal.GetMarketInfoByCode(nMarketID);

                //
                if (nLive >= 2 && strApi != "Bet365")
                    continue;

                if (objProvider["Bets"] == null || !objProvider["Bets"].HasValues)
                    continue;

                List<JToken> lstBet = objProvider["Bets"].ToList();
                if (lstBet.Count == 0)
                    continue;

                List<CBetInfo> lstBetInfo = new List<CBetInfo>();
                foreach (JToken obj in lstBet)
                {
                    CBetInfo info = new CBetInfo(m_nFixtureID);
                    info.UpdateInfo(obj as JObject);
                    lstBetInfo.Add(info);
                }

                switch (clsMarket.m_nFamily)
                {
                    case 1: //승무패
                        this.Parsing1X2(lstBetInfo, strApi, nMarketID, nLive, nProviderCnt);
                        break;
                    case 2: //승패
                        this.Parsing12(lstBetInfo, strApi, nMarketID, nLive, nProviderCnt);
                        break;
                    case 7: //언더오버
                        this.ParsingUnderOver(lstBetInfo, strApi, nMarketID, nLive, nProviderCnt);
                        break;
                    case 8: //아시안핸디캡
                    case 9:
                        this.ParsingAsianHandicap(lstBetInfo, strApi, nMarketID, nLive, nProviderCnt);
                        break;
                    case 10: //홀짝
                        this.ParsingOddEven(lstBetInfo, strApi, nMarketID, nLive, nProviderCnt);
                        break;
                    case 11: //정확한스코어
                        this.ParsingCorrectScore(lstBetInfo, strApi, nMarketID, nLive, nProviderCnt);
                        break;
                    case 12: //더블챤스
                        this.ParsingDoubleChance(lstBetInfo, strApi, nMarketID, nLive, nProviderCnt);
                        break;
                    case 47: //승무패 및 언더오버
                        this.Parsing1X2UnderOver(lstBetInfo, strApi, nMarketID, nLive, nProviderCnt);
                        break;
                }
            }
        }


        private CBetRate GetBetRateInfoByMarketID(int nMarketID, string strApi, int nLive)
        {
            CBetRate clsBetRate = null;

            if (nLive < 2)
            {
                clsBetRate = m_lstPrematchBetRate.Find(value => value.m_nMarket == nMarketID);
                if (clsBetRate == null)
                {
                    clsBetRate = new CBetRate(this);
                    clsBetRate.m_nMarket = nMarketID;
                    clsBetRate.m_strApi = strApi;
                    m_lstPrematchBetRate.Add(clsBetRate);
                }
            }
            else
            {
                clsBetRate = m_lstLiveBetRate.Find(value => value.m_nMarket == nMarketID);
                if (clsBetRate == null)
                {
                    clsBetRate = new CBetRate(this);
                    clsBetRate.m_nMarket = nMarketID;
                    clsBetRate.m_strApi = strApi;
                    m_lstLiveBetRate.Add(clsBetRate);
                }
                if (clsBetRate.m_strApi == string.Empty)
                {
                    clsBetRate.m_strApi = strApi;
                }
            }

            return clsBetRate;
        }

        private CBetRate GetBetRateInfoByMarketIDAndBaseLine(int nMarketID, string strBaseLine, string strApi, int nLive)
        {
            CBetRate clsBetRate = null;

            if (nLive < 2)
            {
                clsBetRate = m_lstPrematchBetRate.Find(value => value.m_nMarket == nMarketID && value.m_strBLine == strBaseLine);
                if (clsBetRate == null)
                {
                    clsBetRate = new CBetRate(this);
                    clsBetRate.m_nMarket = nMarketID;
                    clsBetRate.m_strApi = strApi;
                    clsBetRate.m_strBLine = strBaseLine;
                    m_lstPrematchBetRate.Add(clsBetRate);
                }
            }
            else
            {
                clsBetRate = m_lstLiveBetRate.Find(value => value.m_nMarket == nMarketID && value.m_strBLine == strBaseLine);
                if (clsBetRate == null)
                {
                    clsBetRate = new CBetRate(this);
                    clsBetRate.m_nMarket = nMarketID;
                    clsBetRate.m_strBLine = strBaseLine;
                    clsBetRate.m_strApi = strApi;
                    m_lstLiveBetRate.Add(clsBetRate);
                }
                if (clsBetRate.m_strApi == string.Empty)
                {
                    clsBetRate.m_strApi = strApi;
                }
            }

            return clsBetRate;
        }

        private CBetRate GetBetRateInfoByHBetID(int nMarketID, string strBetID, string strApi, int nLive)
        {
            CBetRate clsBetRate = null;

            if (nLive < 2)
            {
                clsBetRate = m_lstPrematchBetRate.Find(value => value.m_strHBetCode == strBetID);
                if (clsBetRate == null)
                {
                    clsBetRate = new CBetRate(this);
                    clsBetRate.m_nMarket = nMarketID;
                    clsBetRate.m_strApi = strApi;
                    m_lstPrematchBetRate.Add(clsBetRate);
                }
            }
            else
            {
                clsBetRate = m_lstLiveBetRate.Find(value => value.m_strHBetCode == strBetID);
                if (clsBetRate == null)
                {
                    clsBetRate = new CBetRate(this);
                    clsBetRate.m_nMarket = nMarketID;
                    clsBetRate.m_strApi = strApi;
                    m_lstLiveBetRate.Add(clsBetRate);
                }
                if (clsBetRate.m_strApi == string.Empty)
                {
                    clsBetRate.m_strApi = strApi;
                }
            }

            return clsBetRate;
        }

        //승무패
        private void Parsing1X2(List<CBetInfo> lstBet, string strApi, int nMarketID, int nLive, int nProviderCnt)
        {
            foreach (CBetInfo info in lstBet)
            {
                int nIndex = -1;
                if (info.m_strName == "1")
                    nIndex = 0;
                else if (info.m_strName == "2")
                    nIndex = 1;
                else if (info.m_strName == "X")
                    nIndex = 2;

                CBetRate clsBetRate = GetBetRateInfoByMarketID(nMarketID, strApi, nLive);

                if (clsBetRate.m_strApi != strApi)
                {
                    continue;
                }

                clsBetRate.UpdateInfo(nIndex, info, nLive);
            }
        }

        //승패
        private void Parsing12(List<CBetInfo> lstBet, string strApi, int nMarketID, int nLive, int nProviderCnt)
        {
            foreach (CBetInfo info in lstBet)
            {
                int nIndex = -1;
                if (info.m_strName == "1")
                    nIndex = 0;
                else if (info.m_strName == "2")
                    nIndex = 1;

                CBetRate clsBetRate = GetBetRateInfoByMarketID(nMarketID, strApi, nLive);

                if (clsBetRate.m_strApi != strApi)
                {
                    continue;
                }
                clsBetRate.UpdateInfo(nIndex, info, nLive);
            }
        }

        //언더오버, 언더오버 1쿼터
        private void ParsingUnderOver(List<CBetInfo> lstBet, string strApi, int nMarketID, int nLive, int nProviderCnt)
        {
            foreach (CBetInfo info in lstBet)
            {
                string strBaseLine = info.m_strBaseLine;
                string strTemp = strBaseLine; //.Substring(0, 5);
                strTemp = strTemp.Replace("(", "");
                strTemp = strTemp.Replace(" ", "");
                int nPos = strTemp.IndexOf(".");
                strTemp = strTemp.Substring(nPos);

                if (strTemp != ".5")   //strTemp != ".0" && 
                {
                    continue;
                }

                CBetRate clsBetRate = GetBetRateInfoByMarketIDAndBaseLine(nMarketID, info.m_strBaseLine, strApi, nLive);

                if (clsBetRate.m_strApi != strApi)
                {
                    continue;
                }

                int nIndex = -1;

                if (info.m_strName == "Under")
                {
                    nIndex = 0;
                    clsBetRate.m_strHLine = info.m_strLine;

                    strTemp = clsBetRate.m_strHLine.Trim();
                    clsBetRate.m_dOrder = Convert.ToDouble(strTemp);
                }
                else if (info.m_strName == "Over")
                {
                    nIndex = 1;
                    clsBetRate.m_strALine = info.m_strLine;
                }

                clsBetRate.UpdateInfo(nIndex, info, nLive);

                double nTScore = m_nHomeScore + m_nAwayScore;
                if (m_nSports == CDefine.LSPORTS_SPORTS_SOCCER)
                {
                    if (nTScore >= clsBetRate.m_dOrder && clsBetRate.m_nStatus < 2)
                    {
                        clsBetRate.m_nStatus = 2;
                    }
                }
                else if (m_nSports == CDefine.LSPORTS_SPORTS_BASKETBALL)
                {
                    if (nTScore - 3 >= clsBetRate.m_dOrder && clsBetRate.m_nStatus < 2)
                    {
                        clsBetRate.m_nStatus = 2;
                    }
                }
                else if (m_nSports == CDefine.LSPORTS_SPORTS_BASEBALL)
                {
                    if (nTScore - 3 >= clsBetRate.m_dOrder && clsBetRate.m_nStatus < 2)
                    {
                        clsBetRate.m_nStatus = 2;
                    }
                }
                else if (m_nSports == CDefine.LSPORTS_SPORTS_VOLLEYBALL)
                {
                    if (nTScore - 3 >= clsBetRate.m_dOrder && clsBetRate.m_nStatus < 2)
                    {
                        clsBetRate.m_nStatus = 2;
                    }
                }
                else if (m_nSports == CDefine.LSPORTS_SPORTS_HOCKEY)
                {
                    if (nTScore - 1 >= clsBetRate.m_dOrder && clsBetRate.m_nStatus < 2)
                    {
                        clsBetRate.m_nStatus = 2;
                    }
                }
            }
        }

        //아시안핸디캡
        private void ParsingAsianHandicap(List<CBetInfo> lstBet, string strApi, int nMarketID, int nLive, int nProviderCnt)
        {
            foreach (CBetInfo info in lstBet)
            {
                string strBaseLine = info.m_strBaseLine;
                string strTemp = strBaseLine.Substring(0, 5);
                strTemp = strTemp.Replace("(", "");
                strTemp = strTemp.Replace(" ", "");
                int nPos = strTemp.IndexOf(".");
                strTemp = strTemp.Substring(nPos);

                if (strTemp != ".5" && strTemp != ".0")
                {
                    continue;
                }

                CBetRate clsBetRate = GetBetRateInfoByMarketIDAndBaseLine(nMarketID, info.m_strBaseLine, strApi, nLive);

                if (clsBetRate.m_strApi != strApi)
                {
                    continue;
                }

                int nIndex = -1;

                if (info.m_strName == "1")
                {
                    nIndex = 0;
                    clsBetRate.m_strHLine = info.m_strLine;

                    strTemp = clsBetRate.m_strHLine.Substring(0, 5);
                    strTemp = strTemp.Replace("(", "");
                    strTemp = strTemp.Replace(" ", "");
                    strTemp = strTemp.Trim();

                    clsBetRate.m_dOrder = Convert.ToDouble(strTemp);
                }
                else if (info.m_strName == "2")
                {
                    nIndex = 1;
                    clsBetRate.m_strALine = info.m_strLine;
                }
                clsBetRate.UpdateInfo(nIndex, info, nLive);
            }
        }

        //홀짝
        private void ParsingOddEven(List<CBetInfo> lstBet, string strApi, int nMarketID, int nLive, int nProviderCnt)
        {
            foreach (CBetInfo info in lstBet)
            {
                int nIndex = -1;
                if (info.m_strName == "Odd")
                    nIndex = 0;
                else if (info.m_strName == "Even")
                    nIndex = 1;

                CBetRate clsBetRate = GetBetRateInfoByMarketID(nMarketID, strApi, nLive);

                if (clsBetRate.m_strApi != strApi)
                {
                    continue;
                }
                clsBetRate.UpdateInfo(nIndex, info, nLive);
            }
        }

        //정확한스코어
        private void ParsingCorrectScore(List<CBetInfo> lstBet, string strApi, int nMarketID, int nLive, int nProviderCnt)
        {
            foreach (CBetInfo info in lstBet)
            {
                CBetRate clsBetRate = GetBetRateInfoByHBetID(nMarketID, info.m_strBetID, strApi, nLive);

                if (clsBetRate.m_strApi != strApi)
                {
                    continue;
                }
                clsBetRate.UpdateInfo(0, info, nLive);

                string[] score = clsBetRate.m_strHName.Trim().Split('-');
                if (score.Length != 2)
                    return;

                int nHomeScore = CGlobal.ParseInt(score[0]);
                int nAwayScore = CGlobal.ParseInt(score[1]);

                clsBetRate.m_dOrder = nHomeScore * 1000 + nAwayScore;

                if (clsBetRate.m_nStatus < 2 && (nHomeScore <= m_nHomeScore || nAwayScore <= m_nAwayScore))
                {
                    clsBetRate.m_nStatus = 2;
                }
            }
        }

        //더블챤스
        private void ParsingDoubleChance(List<CBetInfo> lstBet, string strApi, int nMarketID, int nLive, int nProviderCnt)
        {
            foreach (CBetInfo info in lstBet)
            {
                int nIndex = -1;
                string strName = info.m_strName;

                if (strName == "1X")
                    nIndex = 0;
                else if (strName == "X2")
                    nIndex = 1;
                else if (strName == "12")
                    nIndex = 2;

                CBetRate clsBetRate = GetBetRateInfoByMarketID(nMarketID, strApi, nLive);

                if (clsBetRate.m_strApi != strApi)
                {
                    continue;
                }
                clsBetRate.UpdateInfo(nIndex, info, nLive);
            }
        }

        //승무패 및 언더오버
        private void Parsing1X2UnderOver(List<CBetInfo> lstBet, string strApi, int nMarketID, int nLive, int nProviderCnt)
        {
            foreach (CBetInfo info in lstBet)
            {
                string strBaseLine = info.m_strBaseLine;
                string strTemp = strBaseLine; //.Substring(0, 5);
                strTemp = strTemp.Replace("(", "");
                strTemp = strTemp.Replace(" ", "");
                int nPos = strTemp.IndexOf(".");
                strTemp = strTemp.Substring(nPos);

                if (strTemp != ".0" && strTemp != ".5")
                {
                    continue;
                }

                CBetRate clsBetRate = GetBetRateInfoByHBetID(nMarketID, info.m_strBetID, strApi, nLive);
                clsBetRate.m_strBLine = info.m_strBaseLine;
                clsBetRate.m_strHLine = info.m_strLine;

                if (clsBetRate.m_strApi != strApi)
                {
                    continue;
                }
                clsBetRate.UpdateInfo(0, info, nLive);
            }
        }
    }
}
