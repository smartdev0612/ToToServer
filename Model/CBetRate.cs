using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;

namespace LSportsServer
{
    public class CBetRate : MBetRate, ILSports
    {
        public double m_dOrder;                             // 현시순서를 정렬하기 위한 변수  
        public CGame m_clsGame;
        private double[] m_lstTicks = {0, 0, 0};            // 배당업데이트시간 보관. 가장 최근에 변경된 배당만 업데이트.
        private int[] m_lstSetAdminRate = { 0, 0, 0 };      // 0: 환수률 적용안됨, 1: 적용됨

        public int m_nFamily => GetFamily();

        public CBetRate(CGame clsGame)
        {
            m_clsGame = clsGame;
            m_nStatus = 0;
        }

        public MBase GetModel()
        {
            return this as MBetRate;
        }

        public bool IsFinished()
        {
            return m_nStatus == 3;
        }

        public void LoadInfo(DataRow info)
        {
            m_nCode = CGlobal.ParseInt(info["sn"]);
            m_nMarket = CGlobal.ParseInt(info["betting_type"]);
            m_strHBetCode = Convert.ToString(info["home_betid"]);
            m_strDBetCode = Convert.ToString(info["draw_betid"]);
            m_strABetCode = Convert.ToString(info["away_betid"]);
            m_fHRate = Convert.ToDouble(info["new_home_rate"]);
            m_fDRate = Convert.ToDouble(info["new_draw_rate"]);
            m_fARate = Convert.ToDouble(info["new_away_rate"]);
            m_fHBase = Convert.ToDouble(info["home_rate"]);
            m_fDBase = Convert.ToDouble(info["draw_rate"]);
            m_fABase = Convert.ToDouble(info["away_rate"]);
            m_strHLine = Convert.ToString(info["home_line"]);
            m_strDLine = Convert.ToString(info["draw_line"]);
            m_strALine = Convert.ToString(info["away_line"]);
            m_strBLine = Convert.ToString(info["base_line"]);
            m_strHName = Convert.ToString(info["home_name"]);
            m_strDName = Convert.ToString(info["draw_name"]);
            m_strAName = Convert.ToString(info["away_name"]);
            m_nStatus = CGlobal.ParseInt(info["status"]);
            m_strApi = Convert.ToString(info["apiName"]);
            m_nLive = CGlobal.ParseInt(info["live"]);
            m_nWin = CGlobal.ParseInt(info["win"]);
            m_nResult = CGlobal.ParseInt(info["result"]);
            m_nViewFlag = CGlobal.ParseInt(info["view_flag"]);
        }

        public bool CheckWinDrawLose(int nSports)
        {
            bool isWinLose = false;
            switch(nSports)
            {
                case 6046: // 축구
                    if (m_nMarket == 1)
                        isWinLose = true;
                    break;
                case 48242: // 농구
                    if (m_nMarket == 226)
                        isWinLose = true;
                    break;
                case 154830: // 배구
                    if (m_nMarket == 52)
                        isWinLose = true;
                    break;
                case 154914: // 야구
                    if (m_nMarket == 226)
                        isWinLose = true;
                    break;
                case 35232: // 아이스 하키
                    if (m_nMarket == 1)
                        isWinLose = true;
                    break;
                case 687890: // E스포츠
                    if (m_nMarket == 52)
                        isWinLose = true;
                    break;
            }
            return isWinLose;
        }

        public void CopyObject(CBetRate clsRate)
        {
            m_nCode = 0;
            m_nMarket = clsRate.m_nMarket;
            m_strHBetCode = string.Empty;
            m_strDBetCode = string.Empty;
            m_strABetCode = string.Empty;
            m_fHRate = clsRate.m_fHRate;
            m_fDRate = clsRate.m_fDRate;
            m_fARate = clsRate.m_fARate;
            m_fHBase = clsRate.m_fHBase;
            m_fDBase = clsRate.m_fDBase;
            m_fABase = clsRate.m_fABase;
            m_strHLine = clsRate.m_strHLine;
            m_strDLine = clsRate.m_strDLine;
            m_strALine = clsRate.m_strALine;
            m_strBLine = clsRate.m_strBLine;
            m_strHName = clsRate.m_strHName;
            m_strDName = clsRate.m_strDName;
            m_strAName = clsRate.m_strAName;
            m_nStatus = clsRate.m_nStatus;
            m_strApi = string.Empty;
            m_nLive = clsRate.m_nLive;
            m_nWin = clsRate.m_nWin;
            m_nResult = clsRate.m_nResult;
            m_nViewFlag = clsRate.m_nViewFlag;
            //m_nTimeTick = clsRate.m_nTimeTick;
        }

        private int GetFamily()
        {
            CMarket clsMarket = CGlobal.GetMarketInfoByCode(m_nMarket);
            if (clsMarket == null)
                return 0;
            else
                return clsMarket.m_nFamily;
        }

        public void UpdateInfo(int nIndex, CBetInfo info, int nLive)
        {
            if (nIndex == -1)
            {
                return;
            }

            if (this.m_lstTicks[nIndex] > info.m_nTimeTick)
                return;

            this.m_lstTicks[nIndex] = info.m_nTimeTick;
            if (nIndex == 0)
            {
                m_strHBetCode = info.m_strBetID;
                m_strHName = info.m_strName;

                if(m_fHBase == 0.0f || info.m_nStatus < 2)
                {
                    m_fHBase = (float)Math.Round(info.m_fStartPrice, 2);
                }

                if(this.m_nLive < 2)
                {
                    m_fHRate = (float)Math.Round(info.m_fPrice, 2);
                }
                else
                {
                    if(this.m_nLive <= nLive)
                        m_fHRate = (float)Math.Round(info.m_fPrice, 2);
                }
            }
            else if (nIndex == 1)
            {
                m_strABetCode = info.m_strBetID;
                m_strAName = info.m_strName;

                if (m_fABase == 0.0f || info.m_nStatus < 2)
                {
                    m_fABase = (float)Math.Round(info.m_fStartPrice, 2);
                }

                if (this.m_nLive < 2)
                {
                    m_fARate = (float)Math.Round(info.m_fPrice, 2);
                }
                else
                {
                    if (this.m_nLive <= nLive)
                        m_fARate = (float)Math.Round(info.m_fPrice, 2);
                }
            }
            else if (nIndex == 2)
            {
                m_strDBetCode = info.m_strBetID;
                m_strDName = info.m_strName;

                if (m_fDBase == 0.0f || info.m_nStatus < 2)
                {
                    m_fDBase = (float)Math.Round(info.m_fStartPrice, 2);
                }

                if (this.m_nLive < 2)
                {
                    m_fDRate = (float)Math.Round(info.m_fPrice, 2);
                }
                else
                {
                    if (this.m_nLive <= nLive)
                        m_fDRate = (float)Math.Round(info.m_fPrice, 2);
                }
            }

            // 새로 업데이트된 배당은 환수률 적용안된것으로 설정.
            this.m_lstSetAdminRate[nIndex] = 0;

            if (nLive >= 2)
            {
                m_clsGame.SetLiveFlag();
            }

            if (m_nLive < nLive)
            {
                m_nLive = nLive;
                m_nLive = m_nLive > 2 ? 2 : m_nLive;
            }

            if (nLive >= 2)
            {
                this.m_nStatus = info.m_nStatus;
            }
            else
            {
                if(m_nLive < 2)
                    m_nStatus = info.m_nStatus;
            }

            ChangeAdminRate(nLive);

            if (m_nStatus > 2)
            {
                if(m_clsGame.IsFinishGame() && m_clsGame.IsFinishAllRate())
                {
                    CGlobal.RemoveGame(m_clsGame);
                }
            }

            
        }

        public void ChangeAdminRate(int nLive, double fRate = 0.0, bool bAdmin = false)
        {
            CMarket clsMarket = CGlobal.GetMarketInfoByCode(m_nMarket);
            if (clsMarket.m_nPeriod < m_clsGame.m_nPeriod && m_nStatus < 2)
            {
                m_nStatus = 2;
            }

            if (fRate == 0.0f)
                fRate = clsMarket.m_fRate;

            // 관리자에서 환수률 변경하는 경우
            if (bAdmin)
            {
                this.m_lstSetAdminRate[0] = 0;
                this.m_lstSetAdminRate[1] = 0;
                this.m_lstSetAdminRate[2] = 0;
            }

            // 환수률 적용. 이미 적용한 배당은 적용안함.
            if (this.m_lstSetAdminRate[0] == 0)
            {
                m_fHRate = Math.Round(m_fHRate * fRate, 2);
                this.m_lstSetAdminRate[0] = 1;
            }

            if (this.m_lstSetAdminRate[1] == 0)
            {
                m_fARate = Math.Round(m_fARate * fRate, 2);
                this.m_lstSetAdminRate[1] = 1;
            }

            if (this.m_lstSetAdminRate[2] == 0)
            {
                m_fDRate = Math.Round(m_fDRate * fRate, 2);
                this.m_lstSetAdminRate[2] = 1;
            }
                        
            if (clsMarket.m_nFamily == 1)
            {
                if (m_fHRate < 1.0f || m_fDRate < 1.0f || m_fARate < 1.0f)
                {
                    m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                }
            }
            else if(clsMarket.m_nFamily == 2)
            {
                if (m_fHRate < 1.0f || m_fARate < 1.0f)
                {
                    m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                }
            }
            else if (clsMarket.m_nFamily == 10)
            {
                if (m_fHRate < 1.1f || m_fARate < 1.1f)
                {
                    m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                }
            }
            else if(clsMarket.m_nFamily == 7 || clsMarket.m_nFamily == 8 || clsMarket.m_nFamily == 9) // 핸디캡, 언더오버
            {
                if(nLive < 2) // 프리매치
                {
                    if (m_clsGame.m_nSports == 154914) // 야구
                    {
                        if(m_nMarket == 281 || m_nMarket == 236)    // 핸디캡 (1~5이닝), 언더오버(1~5이닝) 배당을 1.8~2.1 사이만제공
                        {
                            if (m_fHRate < 1.8f || m_fHRate > 2.1f || m_fARate < 1.8f || m_fARate > 2.1f)
                            {
                                m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                            }
                        }
                        else
                        {
                            if (m_fHRate < 1.3f || m_fARate < 1.3f)     // 프리매치 야구 전체 핸디캡, 언더오버 배당 1.3이하 삭제
                            {
                                m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                            }
                        }
                    } 
                    else if (m_clsGame.m_nSports == 154830) // 배구
                    {
                        if (m_fHRate < 1.5f || m_fARate < 1.5f)
                        {
                            m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                        }
                    }
                    else if (m_clsGame.m_nSports == 687890) // 이스포츠
                    {
                        if (m_nMarket == 989 || m_nMarket == 990 || m_nMarket == 991 || m_nMarket == 1147 || m_nMarket == 1148 || m_nMarket == 1149 || m_nMarket == 1150 || m_nMarket == 1151 || m_nMarket == 1152 || m_nMarket == 1153)
                        {
                            if (m_fHRate < 1.8f || m_fHRate > 2.1f || m_fARate < 1.8f || m_fARate > 2.1f)
                            {
                                m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                            }
                        }
                        else
                        {
                            if (m_fHRate < 1.1f || m_fARate < 1.1f)
                            {
                                m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                            }
                        } 
                    }
                    else
                    {
                        if (m_fHRate < 1.1f || m_fARate < 1.1f)
                        {
                            m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                        }
                    }
                }
                else // 인플레이
                {
                    if (m_fHRate < 1.1f || m_fARate < 1.1f)
                    {
                        m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                    }
                }

                if (m_clsGame.m_nSports == 48242) // 농구
                {
                    if (m_fHRate < 1.6f || m_fARate < 1.6f)
                    {
                        m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                    }
                }
            }
            else if(clsMarket.m_nFamily == 11 || clsMarket.m_nFamily == 12 || clsMarket.m_nFamily == 47)
            {
                if (m_fHRate < 1.1f)
                {
                    m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                }
            }

            if (m_fHRate > 99.0f)
            {
                m_fHRate = 99.0f;
            }
            if (m_fDRate > 99.0f)
            {
                m_fDRate = 99.0f;
            }
            if (m_fARate > 99.0f)
            {
                m_fARate = 99.0f;
            }
        }

        public void UpdateResult(CBetInfo info)
        {
            int nIndex = 0;
            if (info.m_strName == "1" || info.m_strName == "Under" || info.m_strName == "Odd" || info.m_strName == "1X")
            {
                nIndex = 0;
            }
            else if (info.m_strName == "2" || info.m_strName == "Over" || info.m_strName == "Even" || info.m_strName == "X2")
            {
                nIndex = 1;
            }
            else if (info.m_strName == "X" || info.m_strName == "12")
            {
                nIndex = 2;
            }

            if (info.m_nSettlement > 2)
            {
                m_nWin = 0;
            }
            else if (info.m_nSettlement == 2)
            {
                if (nIndex == 0)
                    m_nWin = 1;
                else if (nIndex == 1)
                    m_nWin = 2;
                else if (nIndex == 2)
                    m_nWin = 3;
            }
            m_nResult = 1;

            CEntry.SaveBetRateInfoToDB(this);


            /*if (m_nMarket == 3 || m_nMarket == 342 || m_nMarket == 1558)
            {
                //핸디캡일때 처리
                string[] lststrWinTeam = { "Home", "Away", "Draw", "Cancel" };
                string strHandiWin = lststrWinTeam[nIndex];
                string query = $"UPDATE tb_child SET handi_winner = '{strHandiWin}' WHERE sn = '{m_clsGame.m_nCode}'";
                CMySql.ExcuteQuery(query);
            }*/

            //베팅테이블에서 해당 베팅자료를 얻어온다.
            /* string sql = $"SELECT tb_game_betting.* FROM tb_game_betting WHERE tb_game_betting.betid = '{info.m_strBetID}' AND result = 0";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            if (list.Count == 0)
            {
                return;
            } */

            object objResult = new object();
            List<string> lstResult = new List<string>();

            Console.WriteLine(info.m_strBetID + " --- ");

            lock (objResult)
            {
                string betid = lstResult.Find(value => value == info.m_strBetID);
                if (betid != null)
                    return;

                lstResult.Add(info.m_strBetID);
            }

            Console.WriteLine(" --- " + info.m_strBetID);

            List<CBetting> lstBetting = new List<CBetting>();
            lstBetting = CGlobal.GetSportsApiBettingByBetID(info.m_strBetID);
            if(lstBetting.Count > 0)
            {
                foreach(CBetting clsBetting in lstBetting)
                {
                    int nResult = info.m_nSettlement;
                    if (nResult == 2)
                        nResult = 1;
                    else if (nResult == 1)
                        nResult = 2;
                    else if (nResult == -1)
                        nResult = 4;
                    else if (nResult == 3)
                        nResult = 4;

                    int nBetResult = nResult;
                    if (Convert.ToString(clsBetting.m_fHomeRate) == "1.00" && Convert.ToString(clsBetting.m_fAwayRate) == "1.00" && Convert.ToString(clsBetting.m_fDrawRate) == "1.00")
                    {
                        nBetResult = 4;
                    }
                    int nSn = CGlobal.ParseInt(clsBetting.m_nCode);
                    string sql = $"UPDATE tb_game_betting SET result = {nBetResult} WHERE sn = {nSn}";
                    CMySql.ExcuteQuery(sql);

                    if (nBetResult > 0)
                    {
                        CGlobal.RemoveSportsApiBetting(clsBetting);
                    }

                    CResult.CalculateSportResult(nSn);
                }
            }

            lock (objResult)
            {
                lstResult.Remove(info.m_strBetID);
            }
        }

        public void InsertBetRateToDB()
        {
            if(m_nCode == 0)
            {
                m_nCode = CEntry.InsertBetRateInfoToDB(this);
            }
        }

    }
}
