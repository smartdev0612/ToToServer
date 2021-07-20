using Newtonsoft.Json.Linq;
using System;
using System.Data;

namespace LSportsServer
{
    public class CBetRate : MBetRate, ILSports
    {
        public double m_dOrder;             //현시순서를 정렬하기 위한 변수  
        private CGame m_clsGame;


        public int m_nFamily => GetFamily();

        public CBetRate(CGame clsGame)
        {
            m_clsGame = clsGame;
            m_nGame = clsGame.m_nCode;
            m_nStatus = 1;
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
            m_fHRate = Convert.ToSingle(info["new_home_rate"]);
            m_fDRate = Convert.ToSingle(info["new_draw_rate"]);
            m_fARate = Convert.ToSingle(info["new_away_rate"]);
            m_fHBase = Convert.ToSingle(info["home_rate"]);
            m_fDBase = Convert.ToSingle(info["draw_rate"]);
            m_fABase = Convert.ToSingle(info["away_rate"]);
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
        }

        public bool CheckWinDrawLose()
        {
            if (m_nMarket == 1 || m_nMarket == 52 || m_nMarket == 226)
                return true;
            else
                return false;
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
            m_fABase = clsRate.m_fDBase;
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

            ChangeAdminRate();

            if (m_nStatus > 2 && m_nCode > 0)
            {
                UpdateResult(info);
                if(m_clsGame.IsFinishGame() && m_clsGame.IsFinishAllRate())
                {
                    CGlobal.RemoveGame(m_clsGame);
                }
            }
        }

        public void ChangeAdminRate(float fRate = 0.0f)
        {
            CMarket clsMarket = CGlobal.GetMarketInfoByCode(m_nMarket);
            if (clsMarket.m_nPeriod < m_clsGame.m_nPeriod && m_nStatus < 2)
            {
                m_nStatus = 2;
            }


            if (fRate == 0.0f)
                fRate = clsMarket.m_fRate;

            m_fHRate = Convert.ToSingle(Math.Round(m_fHRate * fRate, 2));
            m_fDRate = Convert.ToSingle(Math.Round(m_fDRate * fRate, 2));
            m_fARate = Convert.ToSingle(Math.Round(m_fARate * fRate, 2));

            
            if (clsMarket.m_nFamily == 1)
            {
                if (m_fHRate < 1.1f || m_fDRate < 1.1f || m_fARate < 1.1f)
                {
                    m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
                }
            }
            else if(clsMarket.m_nFamily == 2 || clsMarket.m_nFamily == 7 || clsMarket.m_nFamily == 8 || clsMarket.m_nFamily == 10)
            {
                if (m_fHRate < 1.1f || m_fARate < 1.1f)
                {
                    m_nStatus = m_nStatus >= 2 ? m_nStatus : 2;
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

        private void UpdateResult(CBetInfo info)
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


            if (m_nMarket == 3 || m_nMarket == 342 || m_nMarket == 866)
            {
                //핸디캡일때 처리
                string[] lststrWinTeam = { "Home", "Away", "Draw", "Cancel" };
                string strHandiWin = lststrWinTeam[nIndex];
                string query = $"UPDATE tb_child SET handi_winner = '{strHandiWin}' WHERE sn = '{m_nGame}'";
                CMySql.ExcuteQuery(query);
            }

            //베팅테이블에서 해당 베팅자료를 얻어온다.
            string sql = $"SELECT tb_total_betting.* FROM tb_total_betting WHERE tb_total_betting.betid = '{info.m_strBetID}' AND result = 0";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            if (list.Count == 0)
            {
                return;
            }

            int nResult = info.m_nSettlement;
            if (nResult == 2)
                nResult = 1;
            else if (nResult == 1)
                nResult = 2;
            else if (nResult == -1)
                nResult = 4;
            else if (nResult == 3)
                nResult = 4;

            foreach (DataRow betInfo in list)
            {
                int nBetResult = nResult;
                if (Convert.ToString(betInfo["home_rate"]) == "1.00" && Convert.ToString(betInfo["away_rate"]) == "1.00" && Convert.ToString(betInfo["draw_rate"]) == "1.00")
                {
                    nBetResult = 4;
                }
                int nSn = CGlobal.ParseInt(betInfo["sn"]);
                sql = $"UPDATE tb_total_betting SET result = {nBetResult} WHERE sn = {nSn}";
                CMySql.ExcuteQuery(sql);

                CResult.CalculateSportResult(nSn);
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
