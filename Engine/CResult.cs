using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public static class CResult
    {
        public static void CalculateSportResult(int nSn)
        {
            //베팅테이블에서 해당 베팅자료를 얻어온다.
            //string sql = $"SELECT tb_total_betting.* FROM tb_total_betting LEFT JOIN tb_subchild ON tb_total_betting.sub_child_sn = tb_subchild.sn WHERE tb_subchild.child_sn = {clsGame.m_nCode}";
            string sql = $"SELECT tb_total_betting.* FROM tb_total_betting WHERE sn = {nSn}";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            foreach (DataRow betInfo in list)
            {
                int nTotalCnt = 0;
                int nInitCnt = 0;
                int nWinCnt = 0;
                int nLoseCnt = 0;
                int nCancelCnt = 0;
                double fWinRate = 1.0;

                string betting_no = Convert.ToString(betInfo["betting_no"]);
                sql = $"SELECT * FROM tb_total_betting WHERE betting_no = '{betting_no}' AND pass = 0";
                DataRowCollection lstBet = CMySql.GetDataQuery(sql);
                if (lstBet == null || lstBet.Count == 0)
                {
                    continue;
                }
                nTotalCnt = lstBet.Count;

                foreach (DataRow bet in lstBet)
                {
                    int result = CGlobal.ParseInt(bet["result"]);
                    if (result == 0)
                    {
                        nInitCnt++;
                    }
                    else if (result == 1)
                    {
                        nWinCnt++;
                        fWinRate *= Convert.ToDouble(bet["select_rate"]);
                    }
                    else if (result == 2)
                    {
                        nLoseCnt++;
                    }
                    else if (result == 4)
                    {
                        nCancelCnt++;
                        fWinRate *= 1;
                    }
                }

                if (nInitCnt > 0)
                {
                    continue;
                }

                sql = $"UPDATE tb_total_betting SET pass = 1 WHERE betting_no = '{betting_no}'";
                CMySql.ExcuteQuery(sql);

                int member_sn = CGlobal.ParseInt(betInfo["member_sn"]);
                sql = $"SELECT * FROM tb_member WHERE sn = {member_sn}";
                DataRowCollection lstMember = CMySql.GetDataQuery(sql);
                if (lstMember.Count == 0)
                {
                    continue;
                }
                DataRow memberInfo = lstMember[0];


                sql = $"SELECT * FROM tb_total_cart WHERE betting_no = '{betting_no}'";
                DataRowCollection lstTotalCart = CMySql.GetDataQuery(sql);
                if (lstTotalCart.Count == 0)
                {
                    continue;
                }
                int nLastPecialCode = CGlobal.ParseInt(lstTotalCart[0]["last_special_code"]);


                string logo = Convert.ToString(memberInfo["logo"]);
                int betMoney = CGlobal.ParseInt(betInfo["bet_money"]);
                int nWinCash = 0;
                //모두 취소된 게임
                if (nTotalCnt == nCancelCnt)
                {
                    nWinCash = CGlobal.ParseInt(betMoney * fWinRate);
                    sql = $"UPDATE tb_total_cart SET result = 4, operdate = now(), result_money = {nWinCash} WHERE logo = '{logo}' AND betting_no = '{betting_no}'";
                    CMySql.ExcuteQuery(sql);
                    modifyMoneyProcess(member_sn, nWinCash, betting_no, 5);
                }
                //낙첨
                else if (nLoseCnt > 0)
                {
                    sql = $"UPDATE tb_total_cart SET result = 2, operdate = now() WHERE betting_no = '{betting_no}'";
                    CMySql.ExcuteQuery(sql);

                    //-> 배팅자 낙첨 마일리지는 미니게임은 제외, 스포츠 1폴더(이기거나 진거 합 2이상) 이상부터 지급
                    if (nLastPecialCode < 3)
                    {
                        if ((nWinCnt + nLoseCnt) > 1)
                        {
                            modifyMileageProcess(member_sn, betMoney, 4, betting_no);
                        }
                    }
                }
                //당첨
                else if (nWinCnt + nCancelCnt >= nTotalCnt)
                {
                    nWinCash = CGlobal.ParseInt(betMoney * fWinRate);
                    sql = $"UPDATE tb_total_cart SET result = 1, operdate = now(), result_money = {nWinCash} WHERE logo = '{logo}' AND betting_no = '{betting_no}'";
                    CMySql.ExcuteQuery(sql);
                    modifyMoneyProcess(member_sn, nWinCash, betting_no, 4);
                    //-> 배팅자 다폴더 마일리지 보너스.
                    if (nWinCnt > 2)
                    {
                        sql = $"SELECT folder_bouns{nWinCnt} AS bonus FROM tb_point_config";
                        DataRowCollection pointConfigInfo = CMySql.GetDataQuery(sql);
                        if (pointConfigInfo.Count == 0)
                        {
                            continue;
                        }
                        int folder_bouns = CGlobal.ParseInt(pointConfigInfo[0]["bonus"]);
                        modifyMileageProcess(member_sn, betMoney, 3, betting_no, folder_bouns, nWinCnt);
                    }
                }

                //-> 추천인 마일리지는 미니게임 제외, 스포츠 1폴더(이기거나 진거 합 2이상) 이상부터 지급
                if (nLastPecialCode < 3)
                {
                    if ((nWinCnt + nLoseCnt) > 1)
                    {
                        //-> 추천인 낙첨 마일리지
                        recommendFailedGameMileage(member_sn, betMoney, betting_no, nLoseCnt);
                    }
                }
            }
        }

        public static void CalculateMiniResult(int nSpecial)
        {
            string sql = $"SELECT b.sn as childSn, a.sub_child_sn as subChildSn, b.home_score, b.away_score, b.win_team, b.special, b.game_code FROM tb_total_betting a, tb_child b, tb_subchild c ";
            sql += $"WHERE a.sub_child_sn = c.sn and c.child_sn = b.sn AND (b.win_team is not null or b.handi_winner is not null) AND a.result = 0 AND a.betid = 0 AND b.special = {nSpecial} GROUP BY a.sub_child_sn ORDER BY c.child_sn ASC";

            DataRowCollection listData = CMySql.GetDataQuery(sql);

            foreach (DataRow row in listData)
            {
                int nSpecialCode = CGlobal.ParseInt(row["special"]);
                if (nSpecialCode < 5 || nSpecialCode == 50 && nSpecialCode == 22)
                {
                    continue;
                }

                int nChildSn = CGlobal.ParseInt(row["childSn"]);
                int nSubChildSn = CGlobal.ParseInt(row["subChildSn"]);
                string strWinTeam = Convert.ToString(row["win_team"]);
                int nHomeScore = CGlobal.ParseInt(row["home_score"]);
                int nAwayScore = CGlobal.ParseInt(row["away_score"]);
                string strGameCode = Convert.ToString(row["game_code"]);

                sql = $"SELECT sn, member_sn, betting_no, select_no, game_type, home_rate, away_rate, draw_rate FROM tb_total_betting WHERE sub_child_sn = {nSubChildSn}";
                DataRowCollection list = CMySql.GetDataQuery(sql);
                foreach (DataRow res in list)
                {
                    int nBetSn = CGlobal.ParseInt(res["sn"]);
                    int nMemberSn = CGlobal.ParseInt(res["member_sn"]);
                    string strBettingNo = Convert.ToString(res["betting_no"]);
                    int nSelect = CGlobal.ParseInt(res["select_no"]);
                    int nGameType = CGlobal.ParseInt(res["game_type"]);
                    double fHomeRate = Convert.ToDouble(res["home_rate"]);
                    double fAwayRate = Convert.ToDouble(res["away_rate"]);
                    double fDrawRate = Convert.ToDouble(res["draw_rate"]);

                    int nWinCode = 0;
                    //-> 배당 모두 1.00 이면 적특 (관리자 설정).
                    if (fHomeRate == 1.00 && fAwayRate == 1.00 && fDrawRate == 1.00)
                    {
                        nWinCode = 4;
                    }
                    else
                    {
                        if (nGameType == 1)
                        {
                            if (nHomeScore == nAwayScore)
                            {
                                nWinCode = 3;
                            }
                            else if (nHomeScore > nAwayScore)
                            {
                                nWinCode = 1;
                            }
                            else if (nHomeScore < nAwayScore)
                            {
                                nWinCode = 2;
                            }
                        }
                        else if (nGameType == 2)
                        {
                            if ((nHomeScore + fDrawRate) > nAwayScore)
                            {
                                nWinCode = 1;
                            }
                            else if ((nHomeScore + fDrawRate) < nAwayScore)
                            {
                                nWinCode = 2;
                            }
                            else if ((nHomeScore + fDrawRate) == nAwayScore)
                            {
                                nWinCode = 4;
                            }
                        }
                        else if (nGameType == 4)
                        {
                            if ((nHomeScore + nAwayScore) == fDrawRate)
                            {
                                nWinCode = 4;
                            }
                            else if ((nHomeScore + nAwayScore) < fDrawRate)
                            {
                                nWinCode = 1;
                            }
                            else if ((nHomeScore + nAwayScore) > fDrawRate)
                            {
                                nWinCode = 2;
                            }
                        }
                    }

                    int nResult = 0;
                    if (nWinCode == 4)
                    {
                        nResult = 4;
                    }
                    else if (nWinCode == nSelect)
                    {
                        nResult = 1;
                    }
                    else
                    {
                        nResult = 2;
                    }

                    //-> 경기취소된 게임이면 적특.
                    if (strWinTeam == "Cancel")
                    {
                        nResult = 4;
                    }

                    //-> 배팅내역에 결과 입력
                    sql = $"UPDATE tb_total_betting SET result= {nResult} WHERE sn = {nBetSn}";
                    CMySql.ExcuteQuery(sql);
                }

                //-> 당첨여부확인 및 정산금 지급.
                AccountMoneyProcess(nChildSn);
            }
        }

        private static void AccountMoneyProcess(int nChildSn)
        {
            string sql = $"SELECT home_score, away_score, league_sn, kubun, special FROM tb_child WHERE sn = {nChildSn}";
            DataRow childInfo = CMySql.GetDataQuery(sql)[0];
            int nKubun = CGlobal.ParseInt(childInfo["kubun"]);
            //int nHomeScore = CGlobal.ParseInt(childInfo["home_score"]);
            //int nAwayScore = CGlobal.ParseInt(childInfo["away_score"]);
            //int nLeagueSn = CGlobal.ParseInt(childInfo["league_sn"]);

            if (nKubun == 1)
                return;

            sql = $"SELECT sn FROM tb_subchild WHERE child_sn = {nChildSn}";
            DataRow subChildInfo = CMySql.GetDataQuery(sql)[0];
            int nSubChild = CGlobal.ParseInt(subChildInfo["sn"]);
            sql = $"UPDATE tb_subchild SET result = 1 WHERE  sn = {nSubChild}";
            CMySql.ExcuteQuery(sql);

            sql = $"UPDATE tb_child SET kubun = 1 WHERE sn = {nChildSn}";
            CMySql.ExcuteQuery(sql);

            sql = $"SELECT b.betting_no, b.member_sn, d.last_special_code FROM tb_subchild a, tb_total_betting b, tb_child c, tb_total_cart d WHERE a.sn = b.sub_child_sn AND a.child_sn = c.sn AND b.betting_no = d.betting_no AND d.result = 0 and a.child_sn = {nChildSn}";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            foreach (DataRow info in list)
            {
                string strBettingNo = Convert.ToString(info["betting_no"]);
                int nMemberSn = CGlobal.ParseInt(info["member_sn"]);
                int nSpecialCode = CGlobal.ParseInt(info["last_special_code"]);

                sql = $"SELECT a.*, c.home_team FROM tb_total_betting a, tb_subchild b,tb_child c WHERE a.betting_no = '{strBettingNo}' AND a.sub_child_sn = b.sn AND b.child_sn = c.sn ORDER BY a.sn";
                DataRowCollection lstBet = CMySql.GetDataQuery(sql);
                int nWinCount = 0, nLoseCount = 0, nCancelCount = 0, nIngCount = 0;
                double fWinRate = 1.00;
                int nTotalCount = lstBet.Count;
                int nBetMoney = 0;

                foreach (DataRow bet in lstBet)
                {
                    int nBetSn = CGlobal.ParseInt(bet["sn"]);
                    double fSelectRate = Convert.ToDouble(bet["select_rate"]);
                    nBetMoney = CGlobal.ParseInt(bet["bet_money"]);
                    string strHomeTeam = Convert.ToString(bet["home_team"]);
                    if (strHomeTeam.Trim().Length != strHomeTeam.Replace("보너스", "").Trim().Length)
                    {
                        if (nLoseCount > 0)
                        {
                            sql = $"UPDATE tb_total_betting SET result = 2 WHERE sn = {nBetSn}";
                            CMySql.ExcuteQuery(sql);
                        }
                        else if (nIngCount == 0)
                        {
                            nWinCount++;
                            fWinRate *= fSelectRate;
                            sql = $"UPDATE tb_total_betting SET result = 1 WHERE sn = {nBetSn}";
                            CMySql.ExcuteQuery(sql);
                        }
                    }
                    else
                    {
                        int nResult = CGlobal.ParseInt(bet["result"]);
                        if (nResult == 0)
                        {
                            nIngCount++;
                        }
                        else if (nResult == 1)
                        {
                            nWinCount++;
                            fWinRate *= fSelectRate;
                        }
                        else if (nResult == 2)
                        {
                            nLoseCount++;
                        }
                        else if (nResult == 4)
                        {
                            nCancelCount++;
                            fWinRate *= 1;
                        }
                    }
                }

                if (nIngCount > 0)
                {
                    continue;
                }

                //-> 모든게임종료 (정산)
                int nWinMoney = 0;
                sql = $"SELECT * FROM tb_member WHERE sn = {nMemberSn}";
                DataRow memberInfo = CMySql.GetDataQuery(sql)[0];
                string strLogo = Convert.ToString(memberInfo["logo"]);

                if (nTotalCount == nCancelCount)
                {
                    //-> 모두 취소된 게임
                    nWinMoney = CGlobal.ParseInt(nBetMoney * fWinRate);
                    sql = $"UPDATE tb_total_cart SET result = 4, operdate = now(), result_money = {nWinMoney} WHERE logo = '{strLogo}' AND betting_no = '{strBettingNo}'";
                    CMySql.ExcuteQuery(sql);
                    CResult.modifyMoneyProcess(nMemberSn, nWinMoney, strBettingNo, 5);
                }
                else if (nLoseCount > 0)
                {
                    //낙첨
                    sql = $"UPDATE tb_total_cart SET result = 2, operdate = now() WHERE betting_no = '{strBettingNo}'";
                    CMySql.ExcuteQuery(sql);
                    //-> 배팅자 낙첨 마일리지는 미니게임은 제외, 스포츠 1폴더(이기거나 진거 합 2이상) 이상부터 지급
                    if (nSpecialCode < 3)
                    {
                        if (nWinCount + nLoseCount > 1)
                        {
                            CResult.modifyMileageProcess(nMemberSn, nBetMoney, 4, strBettingNo);
                        }
                    }
                }
                else if (nWinCount + nCancelCount >= nTotalCount)
                {
                    //당첨
                    nWinMoney = CGlobal.ParseInt(nBetMoney * fWinRate);
                    sql = $"UPDATE tb_total_cart SET result = 1, operdate = now(), result_money = {nWinMoney} WHERE logo = '{strLogo}' AND betting_no = '{strBettingNo}'";
                    CMySql.ExcuteQuery(sql);
                    CResult.modifyMoneyProcess(nMemberSn, nWinMoney, strBettingNo, 4);

                    //-> 배팅자 다폴더 마일리지 보너스.
                    if (nWinCount > 2)
                    {
                        sql = $"SELECT folder_bouns{nWinCount} FROM tb_point_config";
                        DataRowCollection lstPointConfigInfo = CMySql.GetDataQuery(sql);
                        if (lstPointConfigInfo.Count > 0)
                        {
                            int nBonusRate = CGlobal.ParseInt(lstPointConfigInfo[0]["folder_bouns"]);
                            CResult.modifyMileageProcess(nMemberSn, nBetMoney, 3, strBettingNo, nBonusRate, nWinCount);
                        }

                    }
                }

                //-> 추천인 마일리지는 미니게임 제외, 스포츠 1폴더(이기거나 진거 합 2이상) 이상부터 지급
                if (nSpecialCode < 3)
                {
                    if ((nWinCount + nLoseCount) > 1)
                    {
                        //-> 추천인 낙첨 마일리지
                        CResult.recommendFailedGameMileage(nMemberSn, nBetMoney, strBettingNo, nLoseCount);
                    }
                }

            }
        }

        public static void modifyMoneyProcess(int nSn, int nAmount, string strBetingNo, int nState)
        {
            string sql = $"SELECT g_money, mem_status FROM tb_member WHERE sn = {nSn}";
            DataRowCollection lstMemberGmoney = CMySql.GetDataQuery(sql);
            if (lstMemberGmoney.Count == 0)
            {
                return;
            }
            DataRow memberGmoneyInfo = lstMemberGmoney[0];
            int nBefore = CGlobal.ParseInt(memberGmoneyInfo["g_money"]);
            int nAfter = nBefore + nAmount;

            sql = $"UPDATE tb_member SET g_money = {nAfter} WHERE sn = {nSn}";
            CMySql.ExcuteQuery(sql);


            if (Convert.ToString(memberGmoneyInfo["mem_status"]) == "N")
            {
                string statusMessage = "";
                if (nState == 4)
                {
                    statusMessage = $"당첨배당금[배팅번호:{strBetingNo}]";
                }
                else if (nState == 5)
                {
                    statusMessage = "취소";
                }

                sql = $"INSERT INTO tb_money_log(member_sn, amount, before_money, after_money, regdate, state, status_message, betting_no) VALUES({nSn}, {nAmount}, {nBefore}, {nAfter}, now(), 4, '{statusMessage}', '{strBetingNo}')";
                CMySql.ExcuteQuery(sql);
            }
        }

        public static void modifyMileageProcess(int sn, int amount, int type, string bettingNo, double rate = 0, int winCount = 0, string addMsg = "")
        {
            string sql = $"SELECT point, mem_lev, mem_status FROM tb_member WHERE sn = {sn}";
            DataRowCollection lstMember = CMySql.GetDataQuery(sql);
            if (lstMember.Count == 0)
            {
                return;
            }
            DataRow memberInfo = lstMember[0];
            int level = CGlobal.ParseInt(memberInfo["mem_lev"]);

            string statusMessage = "";
            if (type == 3)
            {
                statusMessage = $"{winCount} 게임 | 다폴더[배팅번호:{bettingNo}]";
            }
            if (type == 4)
            {
                sql = $"SELECT lev_bet_failed_mileage_rate FROM tb_level_config WHERE lev = {level}";
                DataRowCollection lstConfig = CMySql.GetDataQuery(sql);
                if (lstConfig.Count > 0)
                {
                    DataRow configInfo = lstConfig[0];
                    rate = Convert.ToDouble(configInfo["lev_bet_failed_mileage_rate"]);
                }

                statusMessage = $"낙첨 | 게임번호[{bettingNo}]";
            }
            if (type == 12)
            {
                statusMessage = $"추천인 {addMsg} 마일리지 | 게임번호[{bettingNo}]";
            }

            if (rate <= 0)
                return;

            amount = CGlobal.ParseInt(amount * rate / 100);

            int before = CGlobal.ParseInt(memberInfo["point"]);
            int after = before + amount;
            sql = $"UPDATE tb_member SET point = {after} WHERE sn = {sn}";
            CMySql.ExcuteQuery(sql);

            if (Convert.ToString(memberInfo["mem_status"]) == "N")
            {
                sql = $"INSERT INTO tb_mileage_log(member_sn, amount, before_mileage, after_mileage, regdate, state, status_message, rate, betting_no) VALUES({sn}, {amount}, {before}, {after}, now(), 3, '{statusMessage}', {rate}, '{bettingNo}')";
                CMySql.ExcuteQuery(sql);
            }
        }

        //▶ 추천인 낙첨 마일리지
        public static void recommendFailedGameMileage(int sn, int amount, string bettingNo, int loseCount)
        {
            //-> 1대, 2대 회원sn을 가져옴.
            string sql = $"SELECT recommend_sn, recommend2_sn FROM tb_join_recommend WHERE member_sn = {sn}";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            if (list == null || list.Count == 0)
            {
                return;
            }

            DataRow recommendInfo = list[0];
            int recommend_sn = CGlobal.ParseInt(recommendInfo["recommend_sn"]);
            int recommend2_sn = CGlobal.ParseInt(recommendInfo["recommend2_sn"]);

            //-> 1대 추천인 처리.
            if (recommend_sn > 0)
            {
                sql = $"SELECT mem_lev FROM tb_member WHERE mem_status = 'N' AND sn = {recommend_sn}";
                DataRowCollection lstMember = CMySql.GetDataQuery(sql);
                if (lstMember != null && lstMember.Count > 0)
                {
                    DataRow memberInfo = lstMember[0];
                    int recommend_lev = CGlobal.ParseInt(memberInfo["mem_lev"]);

                    //-> 1대 레벨에 맞는 마일리지 지급타입과 지급율(%)을 가져온다.
                    sql = $"SELECT lev_join_recommend_mileage_rate_type, lev_join_recommend_mileage_rate FROM tb_level_config WHERE lev = {recommend_lev}";
                    DataRowCollection lstConfig = CMySql.GetDataQuery(sql);
                    if (lstConfig != null && lstConfig.Count > 0)
                    {
                        DataRow configInfo = lstConfig[0];
                        string rateType = Convert.ToString(configInfo["lev_join_recommend_mileage_rate_type"]);
                        string[] rateInfo = Convert.ToString(configInfo["lev_join_recommend_mileage_rate"]).Split(':');
                        double rate = Convert.ToDouble(rateInfo[0]);

                        if (rate > 0)
                        {
                            //-> 지급타입이 lose(낙첨)이면 $loseCount가 1이상 되어야 지급.
                            if (rateType == "lose" && loseCount > 0)
                            {
                                modifyMileageProcess(recommend_sn, amount, 12, bettingNo, rate, 0, "낙첨");
                            }
                            else if (rateType == "betting")
                            {
                                //-> 지급타입이 betting(배팅)이면 $loseCount와 상관없이 무작정 지급.
                                modifyMileageProcess(recommend_sn, amount, 12, bettingNo, rate, 0, "배팅");
                            }
                        }
                    }

                }
            }

            //-> 2대 추천인 처리.
            if (recommend2_sn > 0)
            {
                sql = $"SELECT mem_lev FROM tb_member WHERE mem_status = 'N' AND sn = {recommend2_sn}";
                DataRowCollection lstmemberInfo2 = CMySql.GetDataQuery(sql);
                if (lstmemberInfo2 != null && lstmemberInfo2.Count > 0)
                {
                    DataRow memberInfo2 = lstmemberInfo2[0];
                    int recommend2_lev = CGlobal.ParseInt(memberInfo2["mem_lev"]);

                    //-> 2대 레벨에 맞는 마일리지 지급타입과 지급율(%)을 가져온다.
                    sql = $"SELECT lev_join_recommend_mileage_rate_type, lev_join_recommend_mileage_rate FROM tb_level_config WHERE lev = {recommend2_lev}";
                    DataRowCollection lstConfig = CMySql.GetDataQuery(sql);
                    if (lstConfig.Count > 0)
                    {
                        DataRow configInfo = lstConfig[0];
                        string rateType = Convert.ToString(configInfo["lev_join_recommend_mileage_rate_type"]);
                        string[] rateInfo = Convert.ToString(configInfo["lev_join_recommend_mileage_rate"]).Split(':');
                        double rate = Convert.ToDouble(rateInfo[1]);
                        if (rate > 0)
                        {
                            //-> 지급타입이 lose(낙첨)이면 $loseCount가 1이상 되어야 지급.
                            if (rateType == "lose" && loseCount > 0)
                            {
                                modifyMileageProcess(recommend2_sn, amount, 12, bettingNo, rate, 0, "낙첨");
                            }
                            else if (rateType == "betting")
                            {
                                //-> 지급타입이 betting(배팅)이면 $loseCount와 상관없이 무작정 지급.
                                modifyMileageProcess(recommend2_sn, amount, 12, bettingNo, rate, 0, "배팅");
                            }
                        }
                    }
                }
            }
        }
    }
}
