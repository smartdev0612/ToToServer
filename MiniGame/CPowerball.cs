using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using WebSocketSharp;

namespace LSportsServer
{
    public static class CPowerball
    {
        private static WebSocket ws;
        private static CPowerballInfo m_pbInfo;
        private static bool m_bFlag;
        public static int m_nGameTime;

        public static void StartPowerball()
        {
            new Thread(() => ConnectPowerball()).Start();
        }

        private static void ConnectPowerball()
        {
            if (ws == null)
            {
                ws = new WebSocket($"ws://{CDefine.POWER_SERVER}");
                ws.OnOpen += Ws_OnOpen;
                ws.OnError += Ws_OnError;
                ws.OnClose += Ws_OnClose;
                ws.OnMessage += Ws_OnMessage;

                ws.Connect();
            }
        }
        private static void Ws_OnOpen(object sender, EventArgs e)
        {
            Console.WriteLine("Connect powerball server");
        }
        private static void Ws_OnError(object sender, ErrorEventArgs e)
        {
            (sender as WebSocket).Connect();
        }
        private static void Ws_OnClose(object sender, CloseEventArgs e)
        {
            (sender as WebSocket).Connect();
        }
        private static void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            string strPacket = e.Data;
            DateTime dtNow = CMyTime.GetMyTime();
            string[] arrPacket = strPacket.Split('|');

            List<string> packet = new List<string>();
            packet.Add(arrPacket[0]);

            //당일회차
            string strBaseTime = dtNow.ToString("yyyy-MM-dd") + " 00:00:00";
            DateTime dtBaseTime = CMyTime.ConvertStrToTime(strBaseTime);
            TimeSpan spTime = dtNow - dtBaseTime;
            int nDNum = CGlobal.ParseInt(Math.Floor(spTime.TotalSeconds + 30) / 300);
            if (nDNum > 288)
                nDNum = 288;
            packet.Add(nDNum.ToString());

            int nRT = 300 - (CGlobal.ParseInt((spTime.TotalSeconds + 30) % 300));
            packet.Add(nRT.ToString());

            int nBall0 = CGlobal.ParseInt(arrPacket[1]); 
            int nBall1 = CGlobal.ParseInt(arrPacket[2]); 
            int nBall2 = CGlobal.ParseInt(arrPacket[3]); 
            int nBall3 = CGlobal.ParseInt(arrPacket[4]); 
            int nBall4 = CGlobal.ParseInt(arrPacket[5]); 
            int nPow = CGlobal.ParseInt(arrPacket[6]);
            int nSum = nBall0 + nBall1 + nBall2 + nBall3 + nBall4;

            packet.Add(nSum.ToString());
            packet.Add(nPow.ToString());
            packet.Add(nBall0.ToString());
            packet.Add(nBall1.ToString());
            packet.Add(nBall2.ToString());
            packet.Add(nBall3.ToString());
            packet.Add(nBall4.ToString());

            if (CDefine.USE_POWERLADDER == "yes")
            {
                new Thread(() => CPowerladder.OnRecvMessage(packet)).Start();
            }

            if (CDefine.USE_POWERBALL != "yes")
                return;


            int nGnum = CGlobal.ParseInt(packet[0]) + 1;
            m_nGameTime = CGlobal.ParseInt(packet[2]);

            if (m_pbInfo == null)
            {
                m_pbInfo = new CPowerballInfo(nGnum);
                m_bFlag = true;
            }
            else if (m_bFlag && m_pbInfo.m_nGNum != nGnum)
            {
                m_bFlag = false;
                m_pbInfo.RecvResult(packet);
                m_pbInfo = new CPowerballInfo(nGnum);
                m_bFlag = true;
            }

            int nDum = (m_pbInfo.m_nDNum + 1) % 288;
            CGlobal.BroadCastPowerTime(nGnum + 1, nDum, packet[2]);
        }

        public static int GetGameTh()
        {
            return m_pbInfo.m_nGNum;
        }

        

        public static bool CheckGameEnable()
        {
            return m_pbInfo.m_nDNum > 0;
        }
    }

    public class CPowerballInfo
    {
        public int m_nGNum;
        public int m_nDNum;
        public int m_nPow;
        public int m_nSum;
        public int m_nBall0;
        public int m_nBall1;
        public int m_nBall2;
        public int m_nBall3;
        public int m_nBall4;
        public string m_strDate;

        private string m_strPOE;
        private string m_strPUV;
        private string m_strOE;
        private string m_strUV;


        public CPowerballInfo(int nGNum)
        {
            m_nGNum = nGNum;
            DateTime dtNow = CMyTime.GetMyTime();
            m_strDate = dtNow.AddMinutes(5).ToString("yyyy-MM-dd");
            dtNow = dtNow.AddSeconds(50);
            m_nDNum = CGlobal.ParseInt(Math.Floor((double)(dtNow.Hour * 60 + dtNow.Minute) / 5)) + 1;
            if (m_nDNum > 288)
                m_nDNum = m_nDNum - 288;
        }

        public void RecvResult(List<string> packet)
        {
            m_nBall0 = CGlobal.ParseInt(packet[5]);
            m_nBall1 = CGlobal.ParseInt(packet[6]);
            m_nBall2 = CGlobal.ParseInt(packet[7]);
            m_nBall3 = CGlobal.ParseInt(packet[8]);
            m_nBall4 = CGlobal.ParseInt(packet[9]);
            m_nPow = CGlobal.ParseInt(packet[4]);
            m_nSum = m_nBall0 + m_nBall1 + m_nBall2 + m_nBall3 + m_nBall4;

            m_strPOE = m_nPow % 2 == 0 ? "even" : "odd";
            m_strPUV = m_nPow < 5 ? "under" : "over";
            m_strOE = m_nSum % 2 == 0 ? "even" : "odd";
            m_strUV = m_nSum < 73 ? "under" : "over";

            CalculateResult();
        }

        private void CalculateResult()
        {
            string sql = $"SELECT sn, notice, game_code FROM tb_child WHERE game_code LIKE 'p_%' AND special = 7 AND game_th = {m_nGNum}";
            DataRowCollection lastGame = CMySql.GetDataQuery(sql);
            foreach (DataRow info in lastGame)
            {
                int nChildSn = CGlobal.ParseInt(info["sn"]);
                int nHomeScore = 0;
                int nAwayScore = 0;
                string strGameCode = Convert.ToString(info["game_code"]);

                if (strGameCode == "p_n-bs")
                {
                    //-> 대,중,소
                    if (m_nSum >= 15 && m_nSum <= 64)
                    {
                        nHomeScore = 0;
                        nAwayScore = 1;
                    }
                    else if (m_nSum >= 65 && m_nSum <= 80)
                    {
                        nHomeScore = 1;
                        nAwayScore = 1;
                    }
                    else
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                }
                else if (strGameCode == "p_n-oe")
                {
                    //-> 일반볼 홀/짝
                    if (m_strOE == "odd")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else
                    {
                        nHomeScore = 0;
                        nAwayScore = 1;
                    }
                }
                else if (strGameCode == "p_n-uo")
                {
                    //-> 일반볼 언더/오버
                    if (m_strUV == "under")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else
                    {
                        nHomeScore = 0;
                        nAwayScore = 1;
                    }
                }
                else if (strGameCode == "p_p-oe")
                {
                    //-> 파워볼 홀/짝
                    if (m_strPOE == "odd")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else
                    {
                        nHomeScore = 0;
                        nAwayScore = 1;
                    }
                }
                else if (strGameCode == "p_p-uo")
                {
                    //-> 파워볼 언더/오버
                    if (m_strPUV == "under")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else
                    {
                        nHomeScore = 0;
                        nAwayScore = 1;
                    }
                }
                else if (strGameCode == "p_noe-unover")
                {
                    //일반볼조합 (홀언더 / 짝오버)
                    if (m_strOE == "odd" && m_strUV == "under")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_strOE == "even" && m_strUV == "over")
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
                else if (strGameCode == "p_neo-unover")
                {
                    //일반볼조합 (짝언더 / 홀오버)
                    if (m_strOE == "even" && m_strUV == "under")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_strOE == "odd" && m_strUV == "over")
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
                else if (strGameCode == "p_oe-unover")
                {
                    //파워볼조합 (홀언더 / 짝오버)
                    if (m_strPOE == "odd" && m_strPUV == "under")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_strPOE == "even" && m_strPUV == "over")
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
                else if (strGameCode == "p_eo-unover")
                {
                    //파워볼조합 (짝언더 / 홀오버)
                    if (m_strPOE == "even" && m_strPUV == "under")
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_strPOE == "odd" && m_strPUV == "over")
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
                else if (strGameCode == "p_01")
                {
                    //-> 파워볼 (단일 0/1)
                    if (m_nPow == 0)
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_nPow == 1)
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
                else if (strGameCode == "p_23")
                {
                    //-> 파워볼 (단일 2/3)
                    if (m_nPow == 2)
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_nPow == 3)
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
                else if (strGameCode == "p_45")
                {
                    //-> 파워볼 (단일 4/5)
                    if (m_nPow == 4)
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_nPow == 5)
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
                else if (strGameCode == "p_67")
                {
                    //-> 파워볼 (단일 6/7)
                    if (m_nPow == 6)
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_nPow == 7)
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
                else if (strGameCode == "p_89")
                {
                    //-> 파워볼 (단일 8/9)
                    if (m_nPow == 8)
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_nPow == 9)
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
                else if (strGameCode == "p_0279")
                {
                    //-> 파워볼 (구간 0~2/7~9)
                    if (m_nPow >= 0 && m_nPow <= 2)
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_nPow >= 7 && m_nPow <= 9)
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
                else if (strGameCode == "p_3456")
                {
                    //-> 파워볼 (구간 3~4/5~6)
                    if (m_nPow >= 3 && m_nPow <= 4)
                    {
                        nHomeScore = 1;
                        nAwayScore = 0;
                    }
                    else if (m_nPow >= 5 && m_nPow <= 6)
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

            SavePowerBallResult();
            CResult.CalculateMiniResult(7, m_nGNum);
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

            List<string> lstSql = new List<string>();

            string sql = $"UPDATE tb_child SET home_score = {nHomeScore},   away_score = {nAwaySocre}, win_team = '{strWinTeam}' WHERE sn = {nChildSn}";
            lstSql.Add(sql);

            sql = $"UPDATE tb_subchild SET win = {nWinCode} WHERE child_sn = {nChildSn}";
            lstSql.Add(sql);

            CMySql.ExcuteQueryList(lstSql);
        }

        private void SavePowerBallResult()
        {
            string strBall = $"{m_nBall0},{m_nBall1},{m_nBall2},{m_nBall3},{m_nBall4}";
            string strLMS = string.Empty;
            if (m_nSum < 65)
            {
                strLMS = "소(15~64)";
            }
            else if (m_nSum > 80)
            {
                strLMS = "대(81~130)";
            }
            else
            {
                strLMS = "중(65~80)";
            }

            string strPLMS = string.Empty;
            if (m_nPow < 3)
            {
                strPLMS = "A (0~2)";
            }
            else if (m_nPow < 5)
            {
                strPLMS = "B (3~4)";
            }
            else if (m_nPow < 7)
            {
                strPLMS = "C (5~6)";
            }
            else
            {
                strPLMS = "D (7~9)";
            }

            string sql = $"SELECT * FROM tb_powerball_result WHERE th = {m_nGNum}";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            if (list.Count == 0)
            {
                sql = $"INSERT INTO tb_powerball_result(th, nb_list, nb_list_sum, nb_list_area, nb_odd_even, nb_under_over, pb, pb_area,  pb_odd_even, pb_under_over, game_date) ";
                sql += $"VALUES({m_nGNum}, '{strBall}', {m_nSum}, '{strLMS}', '{m_strOE}', '{m_strUV}', {m_nPow}, '{strPLMS}', '{m_strPOE}', '{m_strPUV}', '{m_strDate}')";

                CMySql.ExcuteQuery(sql);
            }
        }
    }
}
