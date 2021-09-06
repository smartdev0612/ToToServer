using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LSportsServer
{
    public static class CEngine
    {
        public static void StartRealProcess()
        {
            new Thread(() => StartCalcThread()).Start();
        }

        private static void StartCalcThread()
        {
            //CalculatePartner();
            bool bFlag = false;
            DateTime dateTime = CMyTime.GetMyTime();
            while (true)
            {
                dateTime = CMyTime.GetMyTime();
                if (dateTime.Hour == 23 && dateTime.Minute == 59 && dateTime.Second > 50)
                {
                    bFlag = true;
                }
                if (dateTime.Hour == 0 && dateTime.Minute == 0 && bFlag)
                {
                    bFlag = false;
                    CalculatePartner();
                    ClearDBThread();
                }

                Thread.Sleep(1000);
            }
        }
        public static void CalculatePartner()
        {
            string strDate = CMyTime.GetMyTime().AddDays(-1).ToString("yyyy-MM-dd");
            string strFromTime = strDate + " 00:00:00";
            string strToTime = strDate + " 23:59:59";

            string sql = $"SELECT * FROM tb_recommend WHERE rec_lev = 1 AND status != 2 ORDER BY idx ASC";
            DataRowCollection recData = CMySql.GetDataQuery(sql);
            foreach (DataRow row in recData)
            {
                int recommendSn = CGlobal.ParseInt(row["Idx"]);
                string recommendId = Convert.ToString(row["rec_id"]);
                string tex_type = Convert.ToString(row["rec_tex_type"]);                                    //-> 총판 정산타입코드
                double rate_sport = Math.Round(Convert.ToDouble(row["rec_rate_sport"]), 1);                                //-> 총판 스포츠 정산 비율%
                double rate_minigame = Math.Round(Convert.ToDouble(row["rec_rate_minigame"]), 1);                          //-> 총판 미니게임 정산 비율%
                int one_folder_flag = CGlobal.ParseInt(row["rec_one_folder_flag"]);                          //-> 총판 단폴더 정산 포함 여부
                string recommendId_top = Convert.ToString(row["rec_parent_id"]);                            //-> 총판 부본사ID


                //-> 만약 총판이 부본사에 소속되어 있지 않다면 총판만 정산.
                int recommendSn_top = 0;
                string tex_type_top = "";
                double rate_sport_top = 0;
                double rate_minigame_top = 0;
                int one_folder_flag_top = 0;

                if (recommendId_top.Trim() != string.Empty)
                {
                    //-> 부본사 정산정보를 가져온다.
                    sql = $"SELECT * FROM tb_recommend WHERE rec_lev = 9 AND rec_id = '{recommendId_top}'";
                    DataRowCollection topRecData = CMySql.GetDataQuery(sql);
                    if (topRecData.Count == 0)
                        continue;

                    recommendSn_top = CGlobal.ParseInt(topRecData[0]["Idx"]);
                    recommendId_top = Convert.ToString(topRecData[0]["rec_id"]);
                    tex_type_top = Convert.ToString(topRecData[0]["rec_tex_type"]);                                             //-> 부본사 정산타입코드
                    rate_sport_top = Math.Round(Convert.ToDouble(topRecData[0]["rec_rate_sport"]), 1);                         //-> 부본사 스포츠 정산 비율%
                    rate_minigame_top = Math.Round(Convert.ToDouble(topRecData[0]["rec_rate_minigame"]), 1);                   //-> 부본사 미니게임 정산 비율%
                    one_folder_flag_top = CGlobal.ParseInt(topRecData[0]["rec_one_folder_flag"]);                               //-> 부본사 단폴더 정산 포함 여부
                }
                //-> 단폴 포함 여부.
                string add_where = string.Empty;
                if (one_folder_flag == 0)
                {
                    add_where = " AND betting_cnt > 1";    //-> 미포함
                }
                else
                {
                    add_where = "";    //-> 포함
                }

                //$result = array();
                //-> 결과대기중 배팅합계
                sql = $"SELECT IFNULL(SUM(betting_money),0) AS total_betting_ready FROM tb_total_cart WHERE partner_sn = '{recommendSn}' AND kubun = 'Y' and is_account = 1 AND result = 0 AND regdate between '{strFromTime}' AND '{strToTime}'{add_where}";
                DataRow rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_betting_ready = Convert.ToInt64(rowInfo["total_betting_ready"]);

                //-> 스포츠 당첨된 배팅합계 + 당첨된 금액(배당)
                sql = $"SELECT IFNULL(SUM(betting_money),0) AS total_betting_win, IFNULL(SUM(result_money),0) AS total_result_win FROM tb_total_cart WHERE partner_sn = '{recommendSn}' AND kubun = 'Y' AND is_account = 1 AND result = 1 AND last_special_code< 3 ";
                sql += $"AND regdate BETWEEN '{strFromTime}' AND '{strToTime}'{add_where}";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_betting_win = Convert.ToInt64(rowInfo["total_betting_win"]);
                long total_result_win = Convert.ToInt64(rowInfo["total_result_win"]);

                //-> 스포츠 낙첨된 배팅합계
                sql = $"SELECT IFNULL(SUM(betting_money),0) AS total_betting_lose FROM tb_total_cart WHERE partner_sn = '{recommendSn}' AND kubun = 'Y' AND is_account = 1 AND result = 2 AND last_special_code < 3 AND regdate between '{strFromTime}' AND '{strToTime}'{add_where}";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_betting_lose = CGlobal.ParseInt(rowInfo["total_betting_lose"]);

                //-> 미니게임 당첨된 배팅합계 + 당첨된 금액(배당)
                sql = $"select ifnull(sum(betting_money),0) as total_betting_win, ifnull(sum(result_money),0) as total_result_win from tb_total_cart where partner_sn = '{recommendSn}' and kubun = 'Y' and is_account = 1 and result = 1 and last_special_code >= 3 and regdate between '{strFromTime}' and '{strToTime}'";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_betting_win_mgame = Convert.ToInt64(rowInfo["total_betting_win"]);
                long total_result_win_mgame = Convert.ToInt64(rowInfo["total_result_win"]);

                //-> 미니게임 낙첨된 배팅합계
                sql = $"select ifnull(sum(betting_money),0) as total_betting_lose from tb_total_cart where partner_sn = '{recommendSn}' and kubun = 'Y' and is_account = 1 and result = 2 and last_special_code >= 3 and regdate between '{strFromTime}' AND '{strToTime}'";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_betting_lose_mgame = Convert.ToInt64(rowInfo["total_betting_lose"]);

                //-> 입금 합계
                sql = $"select ifnull(sum(agree_amount),0) as total_charge from tb_charge_log where state = 1 and member_sn in(select sn from tb_member where recommend_sn = '{recommendSn}' and mem_status != 'G') and regdate between '{strFromTime}' AND '{strToTime}'";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_charge = Convert.ToInt64(rowInfo["total_charge"]);

                //-> 출금 합계
                sql = $"select ifnull(sum(agree_amount),0) as total_exchange from tb_exchange_log where state = 1 and member_sn in(select sn from tb_member where recommend_sn = '{recommendSn}' and mem_status != 'G') and regdate between '{strFromTime}' AND '{strToTime}'";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_exchange = Convert.ToInt64(rowInfo["total_exchange"]);

                //-> 충전(첫충) 포인트 합계
                sql = $"select ifnull(sum(amount),0) as total_mileage_charge from tb_mileage_log where state = 1 and amount > 0 and regdate between '{strFromTime}' AND '{strToTime}' and member_sn in(select sn from tb_member where mem_status != 'G') and member_sn in(select sn from tb_member where recommend_sn = '{recommendSn}')";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_mileage_charge = Convert.ToInt64(rowInfo["total_mileage_charge"]);

                //-> 추천인 낙첨 포인트 합계
                sql = $"select ifnull(sum(amount),0) as total_mileage_recommend_lose from tb_mileage_log where state = 12 and amount > 0 and regdate between '{strFromTime}' AND '{strToTime}' and member_sn in(select sn from tb_member where mem_status != 'G') and member_sn in(select sn from tb_member where recommend_sn = '{recommendSn}')";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_mileage_recommend_lose = Convert.ToInt64(rowInfo["total_mileage_recommend_lose"]);

                //-> 다폴더 포인트 합계
                sql = $"select ifnull(sum(amount),0) as total_mileage_multi_folder from tb_mileage_log where state = 3 and amount > 0 and regdate between '{strFromTime}' and '{strToTime}' and member_sn in(select sn from tb_member where mem_status != 'G') and member_sn in(select sn from tb_member where recommend_sn = '{recommendSn}')";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_mileage_multi_folder = Convert.ToInt64(rowInfo["total_mileage_multi_folder"]);

                //-> 다폴더 낙첨 포인트 합계
                sql = $"select ifnull(sum(a.amount),0) as total_mileage_multi_folder_lose from tb_mileage_log a, tb_total_cart b where a.state = 4 and a.amount > 0 and a.betting_no = b.betting_no and b.betting_cnt > 1 and ";
                sql += $"a.regdate between '{strFromTime}' AND '{strToTime}' and a.member_sn in(select sn from tb_member where mem_status != 'G') and a.member_sn in(select sn from tb_member where recommend_sn = '{recommendSn}')";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_mileage_multi_folder_lose = Convert.ToInt64(rowInfo["total_mileage_multi_folder_lose"]);

                //-> 단폴더 낙첨 포인트 합계
                sql = $"select ifnull(sum(a.amount),0) as total_mileage_one_folder_lose from tb_mileage_log a, tb_total_cart b where a.state = 4 and a.amount > 0 and a.betting_no = b.betting_no and b.betting_cnt = 1 and ";
                sql += $"a.regdate between '{strFromTime}' AND '{strToTime}' and a.member_sn in(select sn from tb_member where mem_status != 'G') and a.member_sn in(select sn from tb_member where recommend_sn = '{recommendSn}')";
                rowInfo = CMySql.GetDataQuery(sql)[0];
                long total_mileage_one_folder_lose = Convert.ToInt64(rowInfo["total_mileage_one_folder_lose"]);


                //-> 정산방식 + 수익율 + 정산금 계산 ----------
                // 배팅 = (배팅금 * 수익율) / 100
                // 낙첨 = ((미당첨배팅금 - 당첨배당금) * 수익율) / 100
                // 입출 = ((입금 - 출금) * 수익율) / 100
                string tex_type_name = string.Empty;
                long tex_money = 0;
                long tex_money_top = 0;
                long tex_money_m = 0;
                long tex_money_top_m = 0;
                long tex_money_s = 0;
                long tex_money_top_s = 0;

                if (tex_type_top == "in" || tex_type == "in")
                {
                    //-> 기본정산비율인 $rate_sport의 비율로 정산.
                    tex_type_name = "입금";
                    tex_money = CGlobal.ParseInt((total_charge * rate_sport) * 0.01);                    //-> 총판
                    tex_money_top = CGlobal.ParseInt((total_charge * rate_sport_top) * 0.01);            //-> 부본사
                }
                else if (tex_type_top == "inout" || tex_type == "inout")
                {
                    //-> 기본정산비율인 $rate_sport의 비율로 정산.
                    tex_type_name = "입금-출금";

                    tex_money = CGlobal.ParseInt(((total_charge - total_exchange) * rate_sport) * 0.01);             //-> 총판
                    tex_money_top = CGlobal.ParseInt(((total_charge - total_exchange) * rate_sport_top) * 0.01);     //-> 부본사
                }
                else if (tex_type_top == "inout_Mbet" || tex_type == "inout_Mbet")
                {
                    //-> 기본정산비율인 rate_sport의 비율로 정산.
                    tex_type_name = "입금-출금+미니롤링";

                    tex_money = CGlobal.ParseInt(((total_charge - total_exchange) * rate_sport) * 0.01);                 //-> 총판
                    tex_money_top = CGlobal.ParseInt(((total_charge - total_exchange) * rate_sport_top) * 0.01);         //-> 부본사

                    //-> 미니게임롤링 - 총배팅금 * 비율%
                    tex_money_m = CGlobal.ParseInt(((total_betting_win_mgame + total_betting_lose_mgame) * rate_minigame) * 0.01);
                    tex_money_top_m = CGlobal.ParseInt(((total_betting_win_mgame + total_betting_lose_mgame) * rate_minigame_top) * 0.01);

                    //-> 합산
                    tex_money += tex_money_m;
                    tex_money_top += tex_money_top_m;
                }
                else if (tex_type_top == "betting" || tex_type == "betting")
                {
                    //-> 기본정산비율인 $rate_sport의 비율로 정산.
                    tex_type_name = "배팅금(미니제외)";
                    tex_money = CGlobal.ParseInt(((total_betting_win + total_betting_lose) * rate_sport) * 0.01);                    //-> 총판
                    tex_money_top = CGlobal.ParseInt(((total_betting_win + total_betting_lose) * rate_sport_top) * 0.01);            //-> 부본사
                }
                else if (tex_type_top == "betting_m" || tex_type == "betting_m")
                {
                    //-> 기본정산비율인 rate_sport의 비율로 정산.
                    tex_type_name = "배팅금(미니포함)";

                    //-> 스포츠 : 총배팅금 * 비율%
                    tex_money_s = CGlobal.ParseInt(((total_betting_win + total_betting_lose) * rate_sport) * 0.01);
                    tex_money_top_s = CGlobal.ParseInt(((total_betting_win + total_betting_lose) * rate_sport_top) * 0.01);

                    //-> 미니게임 : 총배팅금 * 비율%
                    tex_money_m = CGlobal.ParseInt(((total_betting_win_mgame + total_betting_lose_mgame) * rate_minigame) * 0.01);
                    tex_money_top_m = CGlobal.ParseInt(((total_betting_win_mgame + total_betting_lose_mgame) * rate_minigame_top) * 0.01);

                    //-> 합산
                    tex_money = tex_money_s + tex_money_m;
                    tex_money_top = tex_money_top_s + tex_money_top_m;

                }
                else if (tex_type_top == "fail" || tex_type == "fail")
                {
                    //-> 기본정산비율인 rate_sport의 비율로 정산.
                    tex_type_name = "낙첨금(미니제외)";

                    long sum_betting_money = total_betting_win + total_betting_lose;
                    tex_money = Convert.ToInt64(((sum_betting_money - total_result_win) * rate_sport) * 0.01);                  //-> 총판
                    tex_money_top = Convert.ToInt64(((sum_betting_money - total_result_win) * rate_sport_top) * 0.01);          //-> 부본사

                }
                else if (tex_type_top == "fail_m" || tex_type == "fail_m")
                {
                    //-> 기본정산비율인 rate_sport의 비율로 정산.
                    tex_type_name = "낙첨금(미니포함)";

                    //-> 스포츠 : 낙첨금 * 비율%
                    tex_money_s = CGlobal.ParseInt(((total_betting_win + total_betting_lose - total_result_win) * rate_sport) * 0.01);
                    tex_money_top_s = CGlobal.ParseInt(((total_betting_win + total_betting_lose - total_result_win) * rate_sport_top) * 0.01);

                    //-> 미니게임 : 낙첨금 * 비율%
                    tex_money_m = CGlobal.ParseInt(((total_betting_win_mgame + total_betting_lose_mgame - total_result_win_mgame) * rate_minigame) * 0.01);
                    tex_money_top_m = CGlobal.ParseInt(((total_betting_win_mgame + total_betting_lose_mgame - total_result_win_mgame) * rate_minigame_top) * 0.01);

                    //-> 합산
                    tex_money = tex_money_s + tex_money_m;
                    tex_money_top = tex_money_top_s + tex_money_top_m;

                }
                else if (tex_type_top == "Swin_Mbet" || tex_type == "Swin_Mbet")
                {
                    //-> 스포츠낙첨은 rate_sport로 미니게임롤링은 rate_minigame로 정산.
                    tex_type_name = "스낙+미롤";

                    //-> 스포츠 : 총배팅금 - 당첨금 * 비율%
                    tex_money_s = CGlobal.ParseInt((((total_betting_win + total_betting_lose) - total_result_win) * rate_sport) * 0.01);
                    tex_money_top_s = CGlobal.ParseInt((((total_betting_win + total_betting_lose) - total_result_win) * rate_sport_top) * 0.01);

                    //-> 미니게임롤링 : 총배팅금 * 비율%
                    tex_money_m = CGlobal.ParseInt(((total_betting_win_mgame + total_betting_lose_mgame) * rate_minigame) * 0.01);
                    tex_money_top_m = CGlobal.ParseInt(((total_betting_win_mgame + total_betting_lose_mgame) * rate_minigame_top) * 0.01);

                    //-> 합산
                    tex_money = tex_money_s + tex_money_m;
                    tex_money_top = tex_money_top_s + tex_money_top_m;

                }
                else if (tex_type_top == "Sbet_Mlose" || tex_type == "Sbet_Mlose")
                {
                    //-> 스포츠는 rate_sport로 미니게임은 rate_minigame로 정산.
                    tex_type_name = "S배팅+M낙첨";

                    //-> 스포츠 : 총배팅금 * 비율%
                    tex_money_s = CGlobal.ParseInt(((total_betting_win + total_betting_lose) * rate_sport) * 0.01);
                    tex_money_top_s = CGlobal.ParseInt(((total_betting_win + total_betting_lose) * rate_sport_top) * 0.01);

                    //-> 미니게임 : 총배팅금 - 당첨배당금 * 비율%
                    tex_money_m = CGlobal.ParseInt(((total_betting_win_mgame + total_betting_lose_mgame - total_result_win_mgame) * rate_minigame) * 0.01);
                    tex_money_top_m = CGlobal.ParseInt(((total_betting_win_mgame + total_betting_lose_mgame - total_result_win_mgame) * rate_minigame_top) * 0.01);

                    //-> 합산
                    tex_money = tex_money_s + tex_money_m;
                    tex_money_top = tex_money_top_s + tex_money_top_m;

                }
                else
                {
                    tex_type_name = "미정산";
                    tex_money = 0;
                    tex_money_top = 0;
                }

                //-> 부본사정산금 - 총판정산금 = 부본사최종정산금
                if (recommendId_top.Trim() == string.Empty)
                {
                    tex_money_top = 0;
                }
                else
                {
                    tex_money_top = tex_money_top - tex_money;
                }

                string add_tex_rate = $"{rate_sport}|{rate_minigame}";
                string add_tex_rate_top = $"{rate_sport_top}|{rate_minigame_top}";

                //-> 이미 Insert 되었는지 확인, 있다면 Update
                long tex_log_idx = 0;
                long get_tex_money = 0;
                long get_tex_money_top = 0;

                sql = $"select idx, get_tex_money, get_tex_money_top from tb_recommend_tex where rec_sn = '{recommendSn}' and regdate between '{strFromTime}' and '{strToTime}'";
                DataRowCollection lstLog = CMySql.GetDataQuery(sql);
                if (lstLog.Count > 0)
                {
                    DataRow res = lstLog[0];
                    tex_log_idx = CGlobal.ParseInt(res["idx"]);
                    get_tex_money = CGlobal.ParseInt(res["get_tex_money"]);
                    get_tex_money_top = CGlobal.ParseInt(res["get_tex_money_top"]);
                }

                bool update_res = false;
                int procCnt = 0;

                if (tex_log_idx > 0)
                {
                    //-> 정산 정보 Update
                    sql = $"UPDATE tb_recommend_tex SET rec_sn_top = '{recommendSn_top}', rec_id_top = '{recommendId_top}',save_rate_type = '{tex_type_name}', save_rate_top = '{add_tex_rate_top}', save_rate = '{add_tex_rate}', save_one_folder_flag = '{one_folder_flag}', ";
                    sql += $"money_to_charge = '{total_charge}', money_to_exchange = '{total_exchange}', betting_to_ready = '{total_betting_ready}', betting_to_win = '{total_betting_win}', betting_to_win_mgame = '{total_betting_win_mgame}', ";
                    sql += $"betting_to_lose = '{total_betting_lose}', betting_to_lose_mgame = '{total_betting_lose_mgame}', result_to_win = '{total_result_win}', result_to_win_mgame = '{total_result_win_mgame}', mileage_to_charge = '{total_mileage_charge}', ";
                    sql += $"mileage_to_recomm_lose = '{total_mileage_recommend_lose}', mileage_to_multi_folder = '{total_mileage_multi_folder}', mileage_to_multi_folder_lose = '{total_mileage_multi_folder_lose}', mileage_to_one_folder_lose = '{total_mileage_one_folder_lose}', ";
                    sql += $"tex_money_top = '{tex_money_top}', tex_money = '{tex_money}', updatedate = '{strFromTime}' WHERE idx = {tex_log_idx}";

                    CMySql.ExcuteQuery(sql);
                    update_res = true;
                    procCnt++;
                }
                else
                {
                    //-> 정산 정보 Insert
                    sql = $"INSERT INTO tb_recommend_tex(rec_sn_top, rec_sn, rec_id_top, rec_id, save_rate_type, save_rate_top, save_rate, save_one_folder_flag, money_to_charge, money_to_exchange, betting_to_ready, betting_to_win, betting_to_win_mgame, betting_to_lose, betting_to_lose_mgame, ";
                    sql += $"result_to_win, result_to_win_mgame, mileage_to_charge, mileage_to_recomm_lose, mileage_to_multi_folder, mileage_to_multi_folder_lose, mileage_to_one_folder_lose, tex_money_top, tex_money, regdate) VALUES('{recommendSn_top}', '{recommendSn}', ";
                    sql += $"'{recommendId_top}', '{recommendId}', '{tex_type_name}', '{add_tex_rate_top}', '{add_tex_rate}', '{one_folder_flag}', '{total_charge}', '{total_exchange}', '{total_betting_ready}', '{total_betting_win}', '{total_betting_win_mgame}', '{total_betting_lose}', ";
                    sql += $"'{total_betting_lose_mgame}', '{total_result_win}', '{total_result_win_mgame}', '{total_mileage_charge}', '{total_mileage_recommend_lose}', '{total_mileage_multi_folder}', '{total_mileage_multi_folder_lose}', '{total_mileage_one_folder_lose}', ";
                    sql += $"'{tex_money_top}', '{tex_money}', '{strFromTime}')";

                    CMySql.ExcuteQuery(sql);
                    sql = $"SELECT  idx FROM tb_recommend_tex ORDER BY idx DESC  LIMIT 1";
                    tex_log_idx = CGlobal.ParseInt(CMySql.GetDataQuery(sql)[0]["idx"]);
                    procCnt++;
                }

                //-> 정산타입이 [입금-출금] 이면 결과대기 배팅금이 없어도 정산을 처리 한다.
                if (tex_type_name == "입금-출금" || tex_type_name == "입금-출금+미니롤링")
                {
                    total_betting_ready = 0;
                }

                //-> 총판 현재까지의 예상정산금
                long sum_tex_money = 0;
                if (recommendSn > 0)
                {
                    sql = $"SELECT SUM(tex_money) AS sum_tex_money FROM tb_recommend_tex WHERE is_checked = 0 AND rec_sn = '{recommendSn}' ";
                    DataRowCollection texList = CMySql.GetDataQuery(sql);
                    if (texList.Count > 0)
                    {
                        DataRow res = texList[0];
                        sum_tex_money = Convert.ToInt64(res["sum_tex_money"]);
                    }
                }

                //-> 부본사 현재까지의 예상정산금
                long sum_tex_money_top = 0;
                if (recommendSn_top > 0) { 
                    sql = $"SELECT SUM(tex_money_top) AS sum_tex_money_top FROM tb_recommend_tex WHERE is_checked_top = 0 AND rec_sn_top = '{recommendSn_top}'";
                    DataRowCollection texTopList = CMySql.GetDataQuery(sql);
                    if (texTopList.Count > 0)
                    {
                        DataRow res = texTopList[0];
                        sum_tex_money_top = Convert.ToInt64(res["sum_tex_money_top"]);
                    }
                }

                //-> 결과대기 배팅금이 없으면 부본사와 총판에 정산금을 내린다.
                if ((update_res == true || tex_log_idx > 0) && total_betting_ready == 0 && get_tex_money == 0 && get_tex_money_top == 0 && (sum_tex_money != 0 || sum_tex_money_top != 0))
                {
                    //-> 현재 부본사 머니
                    sql = $"select rec_money from tb_recommend where Idx = '{recommendSn_top}'";
                    DataRowCollection list = CMySql.GetDataQuery(sql);
                    if(list.Count > 0)
                    {
                        DataRow info = list[0];
                       
                        long before_money_top = Convert.ToInt64(info["rec_money"]);
                        long after_money_top = Convert.ToInt64(info["rec_money"]) + sum_tex_money_top;

                        //-> 부본사 정산로그 [get_tex_money, texdate] Update
                        sql = $"update tb_recommend_tex set get_tex_money_top = '{sum_tex_money_top}', texdate = '{strFromTime}', is_checked_top = 1, confirm_date_top = '{strToTime}' where idx = {tex_log_idx}";
                        CMySql.ExcuteQuery(sql);

                        //-> 부본사 미정산 내역들을 모두 정산됨으로 Update
                        sql = $"update tb_recommend_tex set is_checked_top = 1, confirm_date_top = '{strToTime}' where is_checked_top = 0 AND rec_sn_top = '{recommendSn_top}'";
                        CMySql.ExcuteQuery(sql);

                        //-> 부본사 머니 업데이트
                        sql = $"update tb_recommend set rec_money = {after_money_top} where Idx = {recommendSn_top}";
                        CMySql.ExcuteQuery(sql);

                        //-> 부본사 머니 로그 Insert
                        sql = $"INSERT INTO tb_recommend_money_log(rec_sn, amount, before_money, after_money, state, status_message, proc_flag, is_read, procdate, regdate) ";
                        sql += $"VALUES('{recommendSn_top}', '{sum_tex_money_top}', '{before_money_top}', '{after_money_top}', '9', '부본사 정산금 입금', '1', '1', '{strFromTime}', '{strFromTime}')";
                        CMySql.ExcuteQuery(sql);

                        //-> 총판 정산로그 [get_tex_money, texdate] Update
                        sql = $"update tb_recommend_tex set get_tex_money = '{sum_tex_money}', texdate = '{strFromTime}', is_checked = 1, confirm_date = '{strToTime}' where idx = {tex_log_idx}";
                        CMySql.ExcuteQuery(sql);

                        //-> 총판 미정산 내역들을 모두 정산됨으로 Update
                        sql = $"update tb_recommend_tex set is_checked = 1, confirm_date = '{strToTime}' where is_checked = 0 AND rec_sn = '{recommendSn}'";
                        CMySql.ExcuteQuery(sql);

                        //-> 현재 총판 머니
                        sql = $"select rec_money from tb_recommend where Idx = '{recommendSn}'";
                        info = CMySql.GetDataQuery(sql)[0];
                        long before_money = Convert.ToInt64(info["rec_money"]);
                        long after_money = before_money + sum_tex_money;

                        //-> 총판 머니 업데이트
                        sql = $"update tb_recommend set rec_money = '{after_money}' where Idx = {recommendSn}";
                        CMySql.ExcuteQuery(sql);

                        //-> 총판 머니 로그 Insert
                        sql = $"INSERT INTO tb_recommend_money_log(rec_sn, amount, before_money, after_money, state, status_message, proc_flag, is_read, procdate, regdate) VALUES('{recommendSn}', '{sum_tex_money}', ";
                        sql += $"'{before_money}', '{after_money}', 1, '총판 정산금 입금', 1, 1, '{strFromTime}', '{strFromTime}')";
                        CMySql.ExcuteQuery(sql);
                    }
                    else // 총판에 상위부본사가 없는 경우 총판만 정산.
                    {

                        //-> 정산로그 [get_tex_money, texdate] Update
                        sql = $"update tb_recommend_tex set get_tex_money = '{sum_tex_money}', texdate = '{strFromTime}', is_checked = 1, confirm_date = '{strToTime}' where idx = {tex_log_idx}";
                        CMySql.ExcuteQuery(sql);

                        //-> 총판 미정산 내역들을 모두 정산됨으로 Update
                        sql = $"update tb_recommend_tex set is_checked = 1, confirm_date = '{strToTime}' where is_checked = 0 AND rec_sn = '{recommendSn}'";
                        CMySql.ExcuteQuery(sql);

                        //-> 현재 총판 머니
                        sql = $"select rec_money from tb_recommend where Idx = '{recommendSn}'";
                        DataRow info = CMySql.GetDataQuery(sql)[0];
                        long before_money = Convert.ToInt64(info["rec_money"]);
                        long after_money = before_money + sum_tex_money;

                        //-> 총판 머니 업데이트
                        sql = $"update tb_recommend set rec_money = '{after_money}' where Idx = {recommendSn}";
                        CMySql.ExcuteQuery(sql);

                        //-> 총판 머니 로그 Insert
                        sql = $"INSERT INTO tb_recommend_money_log(rec_sn, amount, before_money, after_money, state, status_message, proc_flag, is_read, procdate, regdate) VALUES('{recommendSn}', '{sum_tex_money}', ";
                        sql += $"'{before_money}', '{after_money}', 1, '총판 정산금 입금', 1, 1, '{strFromTime}', '{strFromTime}')";
                        CMySql.ExcuteQuery(sql);
                    }
                }
            }
        }

        public static void ClearDBThread()
        {
            DateTime nowTime = CMyTime.GetMyTime();
            DateTime preTime = nowTime.AddDays(-7);
            DateTime preBettingTime = nowTime.AddDays(-40);
            string strPreDate = preTime.ToString("yyyy-MM-dd");
            string strPreBettingDate = preBettingTime.ToString("yyyy-MM-dd");

            DateTime yesterdayTime = nowTime.AddDays(-1);
            string strYesterday = yesterdayTime.ToString("yyyy-MM-dd");

            string strNowTime = nowTime.AddHours(-3).ToString("yyyy-MM-dd HH:mm");

            string sql = $"SELECT tb_child.sn, tb_child.game_sn FROM tb_child LEFT JOIN tb_subchild ON tb_child.sn = tb_subchild.child_sn WHERE tb_child.sport_id > 0 AND CONCAT(tb_child.gameDate, ' ', tb_child.gameHour, ':', tb_child.gameTime) <= '{strNowTime} 00:00' AND tb_subchild.sn IS NULL UNION SELECT sn, game_sn FROM tb_child WHERE STATUS > 2 AND gameDate< '{strPreDate}'";

            DataRowCollection list = CMySql.GetDataQuery(sql);
            Console.WriteLine(list.Count);

            List<string> lstSql = new List<string>();
            
            if (list.Count > 0)
            {
                string strDeleteChild = $"DELETE FROM tb_child WHERE sn = 0";
                string strDeleteSubChild = $"DELETE FROM tb_subchild WHERE child_sn = 0";
                string strDeleteScore = $"DELETE FROM tb_score WHERE game_sn = 0";
                
                foreach (DataRow info in list)
                {
                    int nChildSn = CGlobal.ParseInt(info["sn"]);
                    strDeleteChild += $" OR sn = {nChildSn}";
                    strDeleteSubChild += $" OR child_sn = {nChildSn}";

                    long nFixtureID = Convert.ToInt64(info["game_sn"]);
                    strDeleteScore += $" OR game_sn = {nFixtureID}";

                    CGlobal.RemoveGameAtFixtureID(nFixtureID);
                }

                lstSql.Add(strDeleteChild);
                lstSql.Add(strDeleteSubChild);
                lstSql.Add(strDeleteScore);

                CMySql.ExcuteQueryList(lstSql);
                lstSql.Clear();
            }

            sql = $"DELETE FROM tb_kenosadari_result WHERE gameDate < '{strPreDate}'";
            lstSql.Add(sql);

            sql = $"DELETE FROM tb_powerball_result WHERE game_date < '{strPreDate}'";
            lstSql.Add(sql);

            sql = $"DELETE FROM tb_powersadari_result WHERE gameDate < '{strPreDate}'";
            lstSql.Add(sql);

            sql = $"DELETE FROM tb_total_betting WHERE tb_total_betting.betting_no IN (SELECT tb_total_cart.betting_no FROM tb_total_cart WHERE tb_total_cart.bet_date < '{strPreBettingDate} 00:00:00')";
            lstSql.Add(sql);

            sql = $"DELETE FROM tb_total_betting_cancel WHERE tb_total_betting_cancel.betting_no IN (SELECT tb_total_cart_cancel.betting_no FROM tb_total_cart_cancel WHERE tb_total_cart_cancel.bet_date < '{strPreBettingDate} 00:00:00')";
            lstSql.Add(sql);

            sql = $"DELETE FROM tb_total_cart WHERE bet_date < '{strPreBettingDate} 00:00:00'";
            lstSql.Add(sql);

            sql = $"DELETE FROM tb_total_cart_cancel WHERE bet_date < '{strPreBettingDate} 00:00:00'";
            lstSql.Add(sql);

            sql = $"DELETE FROM tb_score WHERE strTime < '{ strYesterday } 00:00:00'";
            lstSql.Add(sql);

            CMySql.ExcuteQueryList(lstSql);
            lstSql.Clear();
        }

        
    }
}
