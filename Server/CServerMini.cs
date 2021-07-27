using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public partial class CGameServer
    {
        private void OnPowerballBetting(string strPacket, int nRetCode)
        {
            JToken packet = JObject.Parse(strPacket);

            int nUser = CGlobal.ParseInt(packet["member_sn"]);
            string btGameName = Convert.ToString(packet["btGameName"]);			            //-> 배팅게임종류
		    int btMoney = CGlobal.ParseInt(packet["btMoney"]);				                //-> 배팅금액
		    string btGameTypeList = Convert.ToString(packet["btGameType"]);	                //-> 배팅게임타입
            
            int btGameTh = 0;
            if (btGameName == "powerball")
            {
                btGameTh = CPowerball.GetGameTh();                                         //-> 배팅게임회차
            }
            else if(btGameName == "powersadari")
            {
                btGameTh = CPowerladder.GetGameTh();                                         //-> 배팅게임회차
            }

            if (string.IsNullOrEmpty(btGameName) || btGameTypeList.Length == 0) 
            {
                ReturnPacket(nRetCode, "처리에 필요한 데이터가 부족합니다.", 1);
                return;
            }

            if(btGameName != "powerball" && btGameName != "powersadari")
            {
                ReturnPacket(nRetCode, "등록되지 않은 게임입니다.", 1);
                return;
            }

            if ((btGameName == "powerball" || btGameName == "powersadari") && !CPowerball.CheckGameEnable())
            {
                ReturnPacket(nRetCode, "6시부터 배팅 가능합니다.", 1);
                return;
            }

            //-> 미니게임 리그번호
            
            string sql = string.Empty;
            int leagueSn = 0;
            int specialCode = 0;
            if (btGameName == "powerball")
            {
                specialCode = 7;
                sql = "SELECT sn FROM tb_league WHERE name = '파워볼' ORDER BY sn DESC LIMIT 1";
                DataRowCollection lstLeague = CMySql.GetDataQuery(sql);
                if (lstLeague.Count == 0)
                {
                    ReturnPacket(nRetCode, "파워볼 미니게임이 정보를 찾을수 없습니다.", 1);
                    return;
                }
                leagueSn = CGlobal.ParseInt(CMySql.GetDataQuery(sql)[0]["sn"]);
            }
            else if (btGameName == "powersadari")
            {
                specialCode = 25;
                sql = "SELECT sn FROM tb_league WHERE name = '파워사다리' ORDER BY sn DESC LIMIT 1";
                DataRowCollection lstLeague = CMySql.GetDataQuery(sql);
                if (lstLeague.Count == 0)
                {
                    ReturnPacket(nRetCode, "파워사다리 미니게임이 정보를 찾을수 없습니다.", 1);
                    return;
                }
                leagueSn = CGlobal.ParseInt(CMySql.GetDataQuery(sql)[0]["sn"]);
            }

            //-> 배팅 들어온 게임이 DB에 존재하는지 확인하고 없으면 Insert 한다.
            string gameType = btGameTypeList;
            string gameCode = GetGameCode(btGameName, gameType);
            if(gameCode == string.Empty)
            {
                ReturnPacket(nRetCode, "배팅타입정보가 틀립니다.", 1);
                return;
            }
            
            DateTime dt = CMyTime.GetMyTime().AddSeconds(0 - CPowerball.m_nGameTime + 35);
            string gameDate = dt.ToString("yyyy-MM-dd");
            string gameHour = dt.ToString("HH");
            string gameTime = dt.ToString("mm");
            int limitSec = CPowerball.m_nGameTime;


            sql = $"SELECT sn FROM tb_child WHERE gameDate = '{gameDate}' AND game_code = '{gameCode}' AND game_th = '{btGameTh}' AND special = {specialCode}";
            DataRowCollection lstChild = CMySql.GetDataQuery(sql);
            MiniGameInfo info = GetGameInfo(btGameName, gameType);

            string homeTeam = string.Empty;
            string awayTeam = string.Empty;
            homeTeam = $"{btGameTh}{info.homeTeam}";
            awayTeam = $"{btGameTh}{info.awayTeam}";
          

            if (lstChild.Count == 0)
            {
                sql = $"select * from tb_league where sn = {leagueSn}";
                DataRowCollection lstLeague = CMySql.GetDataQuery(sql);
                string strLeagueName = lstLeague.Count == 0 ? string.Empty : Convert.ToString(lstLeague[0]["name"]);
                //-> 경기등록
                sql = $"insert into tb_child(sport_name, league_sn, home_team, away_team, gameDate, gameHour, gameTime, kubun, type, special, game_code, game_th, notice) values ('기타', '{leagueSn}', '{homeTeam}', '{awayTeam}', '{gameDate}', '{gameHour}', '{gameTime}', '0', '1', '{specialCode}', '{gameCode}', '{btGameTh}', '{strLeagueName}');";
                int childSn = (int)CMySql.ExcuteQuery(sql);

                sql = $"insert into tb_subchild(child_sn, betting_type, home_rate, draw_rate, away_rate) values ('{childSn}','1','{info.homeRate}','{info.drawRate}','{info.awayRate}')";
                CMySql.ExcuteQuery(sql);
            }

            //-> 유저 정보
            sql = $"SELECT * FROM tb_member WHERE sn = {nUser}";
            DataRowCollection lstUser = CMySql.GetDataQuery(sql);
            if(lstUser.Count == 0)
            {
                ReturnPacket(nRetCode, "다시 로그인 하신후 배팅하여 주세요.", 1);
                return;
            }
            DataRow userInfo = lstUser[0];
            //-> 보유머니 체크.
            int userMoney = CGlobal.ParseInt(userInfo["g_money"]);
            if(userMoney < btMoney)
            {
                ReturnPacket(nRetCode, "보유머니가 부족합니다.", 1);
                return;
            }
            double btRateTotal = info.GetBetRate(gameType);

            //-> 배팅금액 제한체크.
            int userLevel = CGlobal.ParseInt(userInfo["mem_lev"]);
            sql = $"SELECT * FROM tb_level_config_minigame WHERE user_level = {userLevel}";
            DataRow config = CMySql.GetDataQuery(sql)[0];
            int minBetMoney = CGlobal.ParseInt(config[$"{btGameName}_min_bet"]);
            int maxBetMoney = CGlobal.ParseInt(config[$"{btGameName}_max_bet"]);
            int maxBnsMoney = CGlobal.ParseInt(config[$"{btGameName}_max_bns"]);

            if(btMoney < minBetMoney)
            {
                ReturnPacket(nRetCode, $"최소 배팅금액은 ${minBetMoney.ToString("N0")}원 입니다.", 1);
                return;
            }
            if(btMoney > maxBetMoney)
            {
                ReturnPacket(nRetCode, $"최대 배팅금액은 ${maxBetMoney.ToString("N0")}원 입니다.", 1);
                return;
            }
            int bettingBonusMoney = CGlobal.ParseInt(btMoney * btRateTotal);
            if(bettingBonusMoney > maxBnsMoney)
            {
                ReturnPacket(nRetCode, $"적중금액은 최대 ${maxBnsMoney.ToString("N0")}원을 넘을수 없습니다.", 1);
                return;
            }

            int sumBetMoney = 0;    // 미니게임 한개 회차에서 한 메뉴에 배팅한 총 금액
            sql = $"SELECT IFNULL(SUM(tb_total_betting.bet_money), 0) AS sumBetMoney FROM tb_total_betting LEFT JOIN tb_subchild ON tb_total_betting.sub_child_sn = tb_subchild.sn LEFT JOIN tb_child ON tb_subchild.child_sn = tb_child.sn WHERE tb_child.special = {specialCode} AND tb_child.game_th = '{btGameTh}' AND tb_total_betting.mini_game_code = '{gameType}' AND tb_total_betting.member_sn = '{nUser}'";
            DataRowCollection rowList = CMySql.GetDataQuery(sql);
            if (rowList.Count > 0)
            {
                sumBetMoney = CGlobal.ParseInt(rowList[0]["sumBetMoney"]);
            }

            if((btMoney + sumBetMoney) > maxBetMoney)
            {
                ReturnPacket(nRetCode, $"한 회차 한 메뉴 배팅금액은 최대 ${maxBetMoney.ToString("N0")}원을 넘을수 없습니다.", 1);
                return;
            }

            int sumResultMoney = 0;    // 미니게임 한개 회차에 한 메뉴에 당첨한 총 금액
            sql = $"SELECT IFNULL(SUM(tb_total_cart.result_money), 0) AS sumResultMoney FROM tb_total_cart LEFT JOIN tb_total_betting ON tb_total_cart.betting_no = tb_total_betting.betting_no LEFT JOIN tb_subchild ON tb_total_betting.sub_child_sn = tb_subchild.sn LEFT JOIN tb_child ON tb_subchild.child_sn = tb_child.sn WHERE tb_child.special = {specialCode}  AND tb_child.game_th = '{btGameTh}' AND tb_total_betting.mini_game_code = '{gameType}' AND tb_total_cart.member_sn = '{nUser}'";
            rowList = CMySql.GetDataQuery(sql);
            if (rowList.Count > 0)
            {
                sumResultMoney = CGlobal.ParseInt(rowList[0]["sumResultMoney"]);
            }

            if ((bettingBonusMoney + sumResultMoney) > maxBnsMoney)
            {
                ReturnPacket(nRetCode, $"한 회차 한 메뉴 적중금액은 최대 ${maxBnsMoney.ToString("N0")}원을 넘을수 없습니다.", 1);
                return;
            }

            //-> 구매코드생성
            sql = "SELECT MAX(sn) AS last_sn FROM tb_total_cart";
            DataRowCollection lstTemp = CMySql.GetDataQuery(sql);
            int lastCartIdx = 0;
            if (lstTemp.Count > 0)
                lastCartIdx = lstTemp[0]["last_sn"] == DBNull.Value ? 0 : CGlobal.ParseInt(lstTemp[0]["last_sn"]);
            double protoId = (CMyTime.ConvertToUnixTimestamp(CMyTime.GetMyTime()) - CMyTime.ConvertToUnixTimestamp(CMyTime.ConvertStrToTime("2000-01-01 00:00:00"))) + lastCartIdx;

            //-> 배팅정보 Insert
            int selectTeam = info.GetSelectTeam(gameType);

            sql = $"select a.sn as child_sn, b.sn as subchild_sn, a.kubun, a.gameDate, a.gameHour, a.gameTime, a.parent_sn, b.home_rate, b.away_rate, b.draw_rate from tb_child a, tb_subchild b where a.sn = b.child_sn and a.special = {specialCode} and a.gameDate = '{gameDate}' and a.game_code = '{gameCode}' and a.game_th = '{btGameTh}'";
            DataRow gameInfo = CMySql.GetDataQuery(sql)[0];
            int subChildSn = CGlobal.ParseInt(gameInfo["subchild_sn"]);
			double home_rate = Convert.ToDouble(gameInfo["home_rate"]);
			double away_rate = Convert.ToDouble(gameInfo["away_rate"]);
			double draw_rate = Convert.ToDouble(gameInfo["draw_rate"]);

            sql = $"INSERT INTO tb_total_betting(sub_child_sn,member_sn,betting_no,select_no,home_rate,draw_rate,away_rate, select_rate,game_type,event,result,kubun,bet_money,mini_game_code) VALUES({subChildSn}, {nUser}, '{protoId}', {selectTeam}, {home_rate}, {draw_rate}, {away_rate}, {btRateTotal}, 1, 0, 0, 'Y', {btMoney}, '{gameType}')";
            CMySql.ExcuteQuery(sql);

            int user_recommend_sn = CGlobal.ParseInt(userInfo["recommend_sn"]);
		    int user_rolling_sn = CGlobal.ParseInt(userInfo["rolling_sn"]);
            string user_status = Convert.ToString(userInfo["mem_status"]);
            int user_account_enable = user_status == "G" ? 0 : 1;
            string bettingIp = Convert.ToString(Context.UserEndPoint.Address);

            sql = $"insert into tb_total_cart(member_sn, betting_no, parent_sn, regdate, operdate, kubun, result, betting_cnt, before_money, betting_money, result_rate, result_money, partner_sn, rolling_sn, bouns_rate, user_del, bet_date, is_account, betting_ip, last_special_code, logo,s_type) values({nUser}, '{protoId}', 0, now(), now(), 'Y', 0, 1, {userMoney}, {btMoney}, {btRateTotal}, 0, {user_recommend_sn}, {user_rolling_sn}, '0', 'N', now(), {user_account_enable}, '{bettingIp}', {specialCode}, 'gadget', 0)";
            CMySql.ExcuteQuery(sql);


            string mem_status = Convert.ToString(userInfo["mem_status"]);
            int before = CGlobal.ParseInt(userInfo["g_money"]);
            int after = before + btMoney;
            sql = $"update tb_member set g_money = g_money - {btMoney} where sn = {nUser}";
            CMySql.ExcuteQuery(sql);

            if (mem_status == "N")
            {
                sql = $"insert into tb_money_log(member_sn,amount,before_money,after_money,regdate,state, status_message, log_memo) values({nUser}, {btMoney}, {before}, {after}, now(), 3, '배팅', '')";
                CMySql.ExcuteQuery(sql);

                //-> 배팅 알람 업데이트.
                if (btMoney >= 300000)
                {
                    sql = $"update tb_alarm_flag set betting_{btGameName}_big = betting_{btGameName}_big + 1 where idx = 1";
                }
                else
                {
                    sql = $"update tb_alarm_flag set betting_{btGameName} = betting_{btGameName} + 1 where idx = 1";
                }
            }

            CMySql.ExcuteQuery(sql);

            ReturnPacket(CDefine.PACKET_SPORT_BET, "배팅신청이 완료되었습니다.", 0);
        }

        private string GetGameCode(string btGameName, string gameType)
        {
            try
            {
                if(btGameName == "powerball")
                {
                    string json = "{'n-oe-o':'p_n-oe', 'n-oe-e':'p_n-oe', 'n-uo-u':'p_n-uo', 'n-uo-o':'p_n-uo', 'p-oe-o':'p_p-oe', 'p-oe-e':'p_p-oe', 'p-uo-u':'p_p-uo', 'p-uo-o':'p_p-uo', 'n-bs-h':'p_n-bs', 'n-bs-d':'p_n-bs', 'n-bs-a':'p_n-bs', 'p_0':'p_01', 'p_1':'p_01', 'p_2':'p_23', 'p_3':'p_23', 'p_4':'p_45', 'p_5':'p_45', 'p_6':'p_67', 'p_7':'p_67', 'p_8':'p_89', 'p_9':'p_89', 'p_02':'p_0279', 'p_79':'p_0279', 'p_34':'p_3456', 'p_56':'p_3456', 'p_o-un':'p_oe-unover', 'p_e-over':'p_oe-unover', 'p_e-un':'p_eo-unover', 'p_o-over':'p_eo-unover', 'n_o-un':'p_noe-unover', 'n_e-over':'p_noe-unover', 'n_e-un':'p_neo-unover', 'n_o-over':'p_neo-unover'}";
                    JToken obj = JObject.Parse(json);

                    return Convert.ToString(obj[gameType]);
                }
                else if(btGameName == "powersadari")
                {
                    string json = "{'odd':'ps_oe', 'even':'ps_oe', 'left':'ps_lr', 'right':'ps_lr', '3line':'ps_34', '4line':'ps_34', 'even3line_left':'ps_e3o4l','odd4line_left':'ps_e3o4l', 'odd3line_right':'ps_o3e4r', 'even4line_right':'ps_o3e4r'}";
                    JToken obj = JObject.Parse(json);

                    return Convert.ToString(obj[gameType]);
                }
            }
            catch
            {
                return string.Empty;
            }

            return string.Empty;
        }

        private MiniGameInfo GetGameInfo(string gameName, string gameType)
        {
            string sql = $"SELECT * FROM tb_mini_odds";
            DataRow miniodds_info = CMySql.GetDataQuery(sql)[0];

            MiniGameInfo info = new MiniGameInfo();

            if(gameName == "powerball")
            {
                if (gameType == "n-oe-o" || gameType == "n-oe-e")
                {
                    info.homeTeam = "회차 [일반볼 홀]";
                    info.awayTeam = "회차 [일반볼 짝]";
                    info.homeRate = Convert.ToDouble(miniodds_info["pb_n_oe"]);
                    info.awayRate = info.homeRate;
                    info.drawRate = 1.0;
                    info.homeCode = "n-oe-o";
                    info.awayCode = "n-oe-e";
                    info.drawCode = "";
                }
                else if (gameType == "n-uo-u" || gameType == "n-uo-o")
                {
                    info.homeTeam = "회차 [일반볼 언더]";
                    info.awayTeam = "회차 [일반볼 오버]";
                    info.homeRate = Convert.ToDouble(miniodds_info["pb_n_uo"]);
                    info.awayRate = info.homeRate;
                    info.drawRate = 1.0;
                    info.homeCode = "n-uo-u";
                    info.awayCode = "n-uo-o";
                    info.drawCode = "";
                }
                else if (gameType == "p-oe-o" || gameType == "p-oe-e")
                {
                    info.homeTeam = "회차 [파워볼 홀]";
                    info.awayTeam = "회차 [파워볼 짝]";
                    info.homeRate = Convert.ToDouble(miniodds_info["pb_p_oe"]);
                    info.awayRate = info.homeRate;
                    info.drawRate = 1.0;
                    info.homeCode = "p-oe-o";
                    info.awayCode = "p-oe-e";
                    info.drawCode = "";
                }
                else if (gameType == "p-uo-u" || gameType == "p-uo-o")
                {
                    info.homeTeam = "회차 [파워볼 언더]";
                    info.awayTeam = "회차 [파워볼 오버]";
                    info.homeRate = Convert.ToDouble(miniodds_info["pb_p_uo"]);
                    info.awayRate = info.homeRate;
                    info.drawRate = 1.0;
                    info.homeCode = "p-uo-u";
                    info.awayCode = "p-uo-o";
                    info.drawCode = "";
                }
                else if (gameType == "n-bs-h" || gameType == "n-bs-d" || gameType == "n-bs-a")
                {
                    info.homeTeam = "회차 [대 81~130]";
                    info.awayTeam = "회차 [소 15~64]";
                    info.homeRate = Convert.ToDouble(miniodds_info["pb_n_bs_h"]);
                    info.awayRate = Convert.ToDouble(miniodds_info["pb_n_bs_a"]);
                    info.drawRate = Convert.ToDouble(miniodds_info["pb_n_bs_d"]);
                    info.homeCode = "n-bs-h";
                    info.awayCode = "n-bs-a";
                    info.drawCode = "n-bs-d";
                }
                else if (gameType == "p_o-un" || gameType == "p_e-over")
                {
                    info.homeTeam = "회차 [파워볼 홀언더]";
                    info.awayTeam = "회차 [파워볼 짝오버]";
                    info.homeRate = Convert.ToDouble(miniodds_info["pb_p_o_un"]);
                    info.awayRate = Convert.ToDouble(miniodds_info["pb_p_e_ov"]);
                    info.drawRate = 1.0;
                    info.homeCode = "p_o-un";
                    info.awayCode = "p_e-over";
                    info.drawCode = "";
                }
                else if (gameType == "p_e-un" || gameType == "p_o-over")
                {
                    info.homeTeam = "회차 [파워볼 짝언더]";
                    info.awayTeam = "회차 [파워볼 홀오버]";
                    info.homeRate = Convert.ToDouble(miniodds_info["pb_p_e_un"]);
                    info.awayRate = Convert.ToDouble(miniodds_info["pb_p_o_ov"]);
                    info.drawRate = 1.0;
                    info.homeCode = "p_e-un";
                    info.awayCode = "p_o-over";
                    info.drawCode = "";
                }
                else if (gameType == "n_o-un" || gameType == "n_e-over")
                {
                    info.homeTeam = "회차 [일반볼 홀언더]";
                    info.awayTeam = "회차 [일반볼 짝오버]";
                    info.homeRate = Convert.ToDouble(miniodds_info["pb_n_o_un"]);
                    info.awayRate = Convert.ToDouble(miniodds_info["pb_n_e_ov"]);
                    info.drawRate = 1.0;
                    info.homeCode = "n_o-un";
                    info.awayCode = "n_e-over";
                    info.drawCode = "";
                }
                else if (gameType == "n_e-un" || gameType == "n_o-over")
                {
                    info.homeTeam = "회차 [일반볼 짝언더]";
                    info.awayTeam = "회차 [일반볼 홀오버]";
                    info.homeRate = Convert.ToDouble(miniodds_info["pb_n_e_un"]);
                    info.awayRate = Convert.ToDouble(miniodds_info["pb_n_o_ov"]);
                    info.drawRate = 1.0;
                    info.homeCode = "n_e-un";
                    info.awayCode = "n_o-over";
                    info.drawCode = "";
                }
            }
            else if(gameName == "powersadari")
            {
                if (gameType == "odd" || gameType == "even")
                {
                    info.homeTeam = "회차 [홀]";
                    info.awayTeam = "회차 [짝]";
                    info.homeRate = Convert.ToDouble(miniodds_info["ps_oe"]);
                    info.awayRate = info.homeRate;
                    info.drawRate = 1.0;
                    info.homeCode = "odd";
                    info.awayCode = "even";
                    info.drawCode = "";
                }
                else if (gameType == "left" || gameType == "right")
                {
                    info.homeTeam = "회차 [좌]";
                    info.awayTeam = "회차 [우]";
                    info.homeRate = Convert.ToDouble(miniodds_info["ps_lr"]);
                    info.awayRate = info.homeRate;
                    info.drawRate = 1.0;
                    info.homeCode = "left";
                    info.awayCode = "right";
                    info.drawCode = "";
                }
                else if (gameType == "3line" || gameType == "4line")
                {
                    info.homeTeam = "회차 [3줄]";
                    info.awayTeam = "회차 [4줄]";
                    info.homeRate = Convert.ToDouble(miniodds_info["ps_line"]);
                    info.awayRate = info.homeRate;
                    info.drawRate = 1.0;
                    info.homeCode = "3line";
                    info.awayCode = "4line";
                    info.drawCode = "";
                }
                else if (gameType == "even3line_left" || gameType == "odd4line_left")
                {
                    info.homeTeam = "회차 [짝3줄좌]";
                    info.awayTeam = "회차 [홀4줄좌]";
                    info.homeRate = Convert.ToDouble(miniodds_info["ps_oeline_lr"]);
                    info.awayRate = info.homeRate;
                    info.drawRate = 1.0;
                    info.homeCode = "even3line_left";
                    info.awayCode = "odd4line_left";
                    info.drawCode = "";
                }
                else if (gameType == "odd3line_right" || gameType == "even4line_right")
                {
                    info.homeTeam = "회차 [홀4줄좌]";
                    info.awayTeam = "회차 [짝4줄우]";
                    info.homeRate = Convert.ToDouble(miniodds_info["ps_oeline_lr"]);
                    info.awayRate = info.homeRate;
                    info.drawRate = 1.0;
                    info.homeCode = "odd3line_right";
                    info.awayCode = "even4line_right";
                    info.drawCode = "";
                }
            }
            
            return info;
        }
        
    }

    public class MiniGameInfo
    {
        public string homeTeam;
        public string awayTeam;
        public double homeRate;
        public double awayRate;
        public double drawRate;
        public string homeCode;
        public string awayCode;
        public string drawCode;

        public double GetBetRate(string gameType)
        {
            double rate = 1.0;

            if (homeCode == gameType)
                rate = homeRate;
            else if (awayCode == gameType)
                rate = awayRate;
            else if (drawCode == gameType)
                rate = drawRate;

            return rate;
        }

        public int GetSelectTeam(string gameType)
        {
            int select = 0;

            if (homeCode == gameType)
                select = 1;
            else if (awayCode == gameType)
                select = 2;
            else if(drawCode == gameType)
                select = 3;

            return select;
        }
    }
}
