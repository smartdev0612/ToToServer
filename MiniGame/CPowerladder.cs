using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSportsServer
{
    public static class CPowerladder
    {
        private static bool m_bFlag;
        private static CPowerladderInfo m_pbInfo;

        public static void OnRecvMessage(List<string> packet)
        {
            int nGnum = CGlobal.ParseInt(packet[0]) + 1;
            if (m_pbInfo == null)
            {
                m_pbInfo = new CPowerladderInfo(nGnum);
                m_bFlag = true;
            }
            else if (m_bFlag && m_pbInfo.m_nGNum != nGnum)
            {
                m_bFlag = false;
                m_pbInfo.RecvResult(packet);
                m_pbInfo = new CPowerladderInfo(nGnum);
                m_bFlag = true;
            }
        }

        public static int GetGameTh()
        {
            return m_pbInfo.m_nDNum;
        }
    }

    public class CPowerladderInfo
    {
        public int m_nGNum;
        public int m_nDNum;
        public int m_nBall;
        public string m_strDate;

        private string m_strLR;
        private string m_strOE;
        private string m_strLine;


        public CPowerladderInfo(int nGNum)
        {
            m_nGNum = nGNum;
            DateTime dtNow = CMyTime.GetMyTime();
            m_strDate = dtNow.AddMinutes(5).ToString("yyyy-MM-dd");
            dtNow = dtNow.AddSeconds(50);
            m_nDNum = CGlobal.ParseInt(Math.Floor((double)(dtNow.Hour * 60 + dtNow.Minute) / 5)) + 1;
            if (m_nDNum > 288)
                m_nDNum -= 288;
        }

        public void RecvResult(List<string> packet)
        {
            m_nBall = CGlobal.ParseInt(packet[5]);
            m_strLR = m_nBall % 2 == 0 ? "R" : "L";
            m_strLine = m_nBall > 14 ? "4" : "3";

            if ((m_strLR == "R" && m_strLine == "4") || (m_strLR == "L" && m_strLine == "3"))
            {
                m_strOE = "E";
            }
            else if ((m_strLR == "R" && m_strLine == "3") || (m_strLR == "L" && m_strLine == "4"))
            {
                m_strOE = "O";
            }

            CalculateResult();
        }

        private void CalculateResult()
        {
            string strDate = CMyTime.GetMyTimeStr("yyyy-MM-dd");
            string sql = $"SELECT sn, game_code FROM tb_child WHERE game_code LIKE 'ps_%' AND special = 25 AND kubun != 1 AND game_th = {m_nDNum} AND gameDate = '{strDate}'";
            DataRowCollection lastGame = CMySql.GetDataQuery(sql);
            foreach (DataRow info in lastGame)
            {
                int nChildSn = CGlobal.ParseInt(info["sn"]);
                int nHomeScore = 0;
                int nAwayScore = 0;

                string strGameCode = Convert.ToString(info["game_code"]);
                if (strGameCode == "ps_oe")
                {
                    //-> 홀/짝
                    if (m_strOE == "O")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_strOE == "E")
                    {
                        nHomeScore = 0;
                        nAwayScore = 1;
                    }
                }
                else if (strGameCode == "ps_lr")
                {
                    //-> 좌/우
                    if (m_strLR == "L")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_strLR == "R")
                    {
                        nHomeScore = 0;
                        nAwayScore = 1;
                    }
                }
                else if (strGameCode == "ps_34")
                {
                    //-> 3줄/4줄
                    if (m_strLine == "3")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_strLine == "4")
                    {
                        nHomeScore = 0;
                        nAwayScore = 1;
                    }
                }
                else if (strGameCode == "ps_e3o4l")
                {
                    //-> 짝좌3줄 / 홀좌4줄
                    if (m_strOE == "E" && m_strLine == "3" && m_strLR == "L")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_strOE == "O" && m_strLine == "4" && m_strLR == "L")
                    {
                        nHomeScore = 0;
                        nAwayScore = 1;
                    }
                    else
                    {
                        nHomeScore = 1;
                        nAwayScore = 1;
                    }
                }
                else if (strGameCode == "ps_o3e4r")
                {
                    //-> 홀우3줄 / 짝우4줄
                    if (m_strOE == "O" && m_strLine == "3" && m_strLR == "R")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_strOE == "E" && m_strLine == "4" && m_strLR == "R")
                    {
                        nHomeScore = 0;
                        nAwayScore = 1;
                    }
                    else
                    {
                        nHomeScore = 1;
                        nAwayScore = 1;
                    }
                }

                UpdateResult(nChildSn, nHomeScore, nAwayScore);
            }

            SavePowerLadderResult();
            CResult.CalculateMiniResult(25);
        }

        private void UpdateResult(int nChildSn, int nHomeScore, int nAwaySocre)
        {
            string strWinTeam = string.Empty;
            int nWinCode = 0;

            if (nHomeScore == nAwaySocre)
            {
                strWinTeam = "Draw";
                nWinCode = 3;
            }
            else if (nHomeScore > nAwaySocre)
            {
                strWinTeam = "Home";
                nWinCode = 1;
            }
            else if (nHomeScore < nAwaySocre)
            {
                strWinTeam = "Away";
                nWinCode = 2;
            }

            string sql = $"UPDATE tb_child SET home_score = {nHomeScore}, away_score = {nAwaySocre}, win_team = '{strWinTeam}' WHERE sn = {nChildSn}";
            CMySql.ExcuteQuery(sql);

            sql = $"UPDATE tb_subchild SET win = {nWinCode} WHERE child_sn = {nChildSn}";
            CMySql.ExcuteQuery(sql);
        }

        private void SavePowerLadderResult()
        {
            string strOE = m_strOE == "O" ? "odd" : "even";
            string strLR = m_strLR == "L" ? "left" : "right";
            string str34 = m_strLine;

            string sql = $"SELECT * FROM tb_powersadari_result WHERE  gameDate = '{m_strDate}' AND th = '{m_nDNum}'";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            if (list.Count == 0)
            {
                sql = $"INSERT INTO tb_powersadari_result(th, hj, start, line, gameDate) ";
                sql += $"VALUES({m_nDNum}, '{strOE}', '{strLR}', '{str34}', '{m_strDate}')";

                CMySql.ExcuteQuery(sql);
            }
        }
    }
}
