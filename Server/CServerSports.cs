using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LSportsServer
{
    public partial class CGameServer
    {
        public CLSportsReqList m_reqParam;

        private void OnLSportsPacket(CPacket packet)
        {
            switch (packet.m_nPacketCode)
            {
                case CDefine.PACKET_SPORT_LIST:
                    OnRequestList(packet.m_strPacket);
                    break;
                case CDefine.PACKET_SPORT_BET:
                    try
                    {
                        OnSportsBetting(packet.m_strPacket);
                    }
                    catch
                    {
                        ReturnPacket(CDefine.PACKET_SPORT_BET, "배팅처리가 되지 않았습니다. 다시 시도 해주세요.", 1);
                        return;
                    }
                    
                    break;
                case CDefine.PACKET_POWERBALL_BET:
                    try
                    {
                        OnPowerballBetting(packet.m_strPacket, packet.m_nPacketCode);
                    }
                    catch
                    {
                        ReturnPacket(CDefine.PACKET_POWERBALL_BET, "배팅처리가 되지 않았습니다. 다시 시도해주세요.", 1);
                        return;
                    }

                    break;

                case CDefine.PACKET_POWERLADDER_BET:
                    try
                    {
                        OnPowerballBetting(packet.m_strPacket, packet.m_nPacketCode);
                    }
                    catch
                    {
                        ReturnPacket(CDefine.PACKET_POWERBALL_BET, "배팅처리가 되지 않았습니다. 다시 시도해주세요.", 1);
                        return;
                    }

                    break;

                case CDefine.PACKET_KENOLADDER_BET:
                    try
                    {
                        OnPowerballBetting(packet.m_strPacket, packet.m_nPacketCode);
                    }
                    catch
                    {
                        ReturnPacket(CDefine.PACKET_POWERBALL_BET, "배팅처리가 되지 않았습니다. 다시 시도해주세요.", 1);
                        return;
                    }

                    break;
            }
        }

        private void OnSportsBetting(string strPacket)
        {

            Thread.Sleep(3000);

            JToken objPacket = JObject.Parse(strPacket);

            int nUser = CGlobal.ParseInt(objPacket["user"]);
            string mode = Convert.ToString(objPacket["mode"]).Trim();
            int betting = CGlobal.ParseInt(objPacket["betMoney"]);
            string gametype = Convert.ToString(objPacket["gametype"]);
            string _gameType = Convert.ToString(objPacket["game_type"]);
            int specialType = CGlobal.ParseInt(objPacket["special_type"]);
            string strgametype = Convert.ToString(objPacket["strgametype"]);
            string betcontent = Convert.ToString(objPacket["betcontent"]);

            int betting_point = CGlobal.ParseInt(betting);
            string result = IsValidBetting(betcontent);

            if (result != "true")
		    {
                ReturnPacket(CDefine.PACKET_SPORT_BET, result, 1);
                return;
            }

            //유저정보를 얻는다.
            string sql = $"SELECT * FROM tb_member WHERE sn = {nUser}";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            if(list.Count == 0)
            {
                ReturnPacket(CDefine.PACKET_SPORT_BET, "유저정보가 틀립니다.", 1);
                return;
            }
            DataRow userInfo = list[0];
            int lev = CGlobal.ParseInt(userInfo["mem_lev"]);

            //-> DB기준 최소 배팅금 확인. 2016.08.10
            sql = $"SELECT lev_min_money FROM tb_level_config WHERE lev = {lev}";
            DataRow info = CMySql.GetDataQuery(sql)[0];
            int bettingMinMoney = CGlobal.ParseInt(info["lev_min_money"]);
            if (betting < bettingMinMoney) 
            {
                ReturnPacket(CDefine.PACKET_SPORT_BET, $"최소 배팅금은 {bettingMinMoney.ToString("N0")}원 입니다.", 1);
                return;
            }

            int dbCash = CGlobal.ParseInt(userInfo["g_money"]);
            if (dbCash < betting || betting < 0 ) 
            {
                ReturnPacket(CDefine.PACKET_SPORT_BET, "보유머니가 부족합니다.", 1);
                return;
            }

            string buy = string.Empty;
            if (mode == "betting") 
            {
			    buy = "Y";
            } 
            else if (mode == "cart") 
            {
			    buy = "N";
            } 
            else
            {
                ReturnPacket(CDefine.PACKET_SPORT_BET, "잘못된 인자입니다.", 1);
                return;
            }

            string[] data = betcontent.Split('#');
            int betting_cnt = data.Length - 1; //-> 배팅경기수
            if(betting_cnt < 0)
            {
                ReturnPacket(CDefine.PACKET_SPORT_BET, "배팅처리가 되지 않았습니다. 다시 시도하십시오.", 1);
                return;
            }

            //-> 게임번호
            sql = "SELECT MAX(sn) AS last_sn FROM tb_total_cart";
            DataRow maxCart = CMySql.GetDataQuery(sql)[0];
            int lastIdx = maxCart["last_sn"] == DBNull.Value ? 0 : CGlobal.ParseInt(maxCart["last_sn"]);

            DateTime baseTime = CMyTime.ConvertStrToTime("2000-01-01 00:00:00");
            DateTime nowTime = CMyTime.GetMyTime();
            string protoId = (CMyTime.ConvertToUnixTimestamp(nowTime) - CMyTime.ConvertToUnixTimestamp(baseTime)  + (9 * 60 * 60) + lastIdx).ToString();
            if (protoId == "") 
            {
                ReturnPacket(CDefine.PACKET_SPORT_BET, "구매번호를 확인하여 주세요.", 1);
                return;
            }
            protoId = $"{nUser}{protoId}";
            string sport_name = string.Empty;

            List<List<string>> lstParamArr = new List<List<string>>();
            //-> 선택한 경기마다 경기시작여부체크
            for (int i = 0; i < betting_cnt; i++) {
                List<string> lstParam = data[i].Split("||").ToList();

                if(lstParam[12] == "이벤트")
                {
                    lstParamArr.Add(lstParam);
                    continue;
                }

			    int childSn = CGlobal.ParseInt(lstParam[0].Split('_')[0]);
                int family = CGlobal.ParseInt(lstParam[0].Split('_')[2]);
                lstParam[0] = childSn.ToString();
                lstParam[13] = family.ToString();
                int selected = CGlobal.ParseInt(lstParam[1]);
                string betId = Convert.ToString(lstParam[14]);
                double selectedRate = Convert.ToDouble(lstParam[7]);

                //if (selected == 0) 
                //    selected = 1;
                //else if (selected == 1) 
                //    selected = 3;
                //else if (selected == 2) 
                //    selected = 2;

                CGame clsGame = CGlobal.GetGameInfoByCode(childSn);
                if(clsGame == null)
                {
                    ReturnPacket(CDefine.PACKET_SPORT_BET, "경기타입이 오류가 발생되였습니다.", 1);
                    return;
                }

                CSports clsSports = CGlobal.GetSportsInfoByCode(clsGame.m_nSports);
                sport_name = clsSports.m_strKo;

                betId = betId.Substring(0, betId.Length - clsGame.m_nFixtureID.ToString().Length);
                betId = $"{betId}{clsGame.m_nFixtureID.ToString()}";
                lstParam[14] = betId;

                CBetRate clsRate = null;
                if (_gameType == "multi")
                    clsRate = clsGame.GetPreRateInfoByBetID(betId);
                else if (_gameType == "abroad")
                    clsRate = clsGame.GetPreRateInfoByBetID(betId);
                else if (_gameType == "live")
                    clsRate = clsGame.GetLiveRateInfoByBetID(betId);

                if (clsRate == null)
                {
                    ReturnPacket(CDefine.PACKET_SPORT_BET, "배당이 변경되였습니다.", 1);
                    return;
                }
                if(clsRate.m_nStatus != 1)
                {
                    ReturnPacket(CDefine.PACKET_SPORT_BET, "배당이 변경되였습니다.", 1);
                    return;
                }

                double delta = 0.5f;
                if(selected == 0)
                {
                    delta = Math.Abs(clsRate.m_fHRate - selectedRate);
                }
                else if(selected == 1)
                {
                    delta = Math.Abs(clsRate.m_fDRate - selectedRate);
                }
                else if(selected == 2)
                {
                    delta = Math.Abs(clsRate.m_fARate - selectedRate);
                }

                if(delta > 0.1)
                {
                    ReturnPacket(CDefine.PACKET_SPORT_BET, "배당이 변경되였습니다.", 1);
                    return;
                }


                clsRate.InsertBetRateToDB();
                lstParam[9] = clsRate.m_nCode.ToString();

                lstParamArr.Add(lstParam);
            }

      //      string kind = string.Empty;
      //      if (_gameType == "multi" || _gameType == "live" || _gameType == "abroad")
		    //{
			   // kind = "cross";
      //      } 
      //      else if (_gameType == "handi") 
      //      {
			   // kind = "handi";
      //      } 
      //      else if (_gameType == "special") 
      //      {
			   // kind = "special";
      //      } 
      //      else if (_gameType == "real") 
      //      {
			   // kind = "real";
      //      } 


            double sumSelectRate = 1.0;
            for (int i = 0; i < lstParamArr.Count; i++) 
             {
                List<string> data_detail = lstParamArr[i];

                int childSn = CGlobal.ParseInt(data_detail[0]);
			    int selected = CGlobal.ParseInt(data_detail[1]);
			    double rate1 = Convert.ToDouble(data_detail[4]);
			    double rate2 = Convert.ToDouble(data_detail[5]);
			    double rate3 = Convert.ToDouble(data_detail[6]);
			    double selectedRate = Convert.ToDouble(data_detail[7]);

			
			    //-> 넘어온 선택배당율(selectedRate) 검증. 2016.08.10
			    int errorRate = 0;
                if (selected == 0 ) 
                {
                    if (rate1 != selectedRate ) 
                        errorRate = 1;
                } 
                else if (selected == 1) 
                {
                    if (rate2 != selectedRate ) 
                        errorRate = 1;
                } 
                else if (selected == 2) 
                {
                    if (rate3 != selectedRate ) 
                        errorRate = 1;
                }

                if (errorRate == 1) 
                {
                    ReturnPacket(CDefine.PACKET_SPORT_BET, "배당(기준점)에 오류가 발생되였습니다.", 1);
                    return;
                }

                //-> 총 배당율 합계 계산. 2016.08.10
			    sumSelectRate = sumSelectRate * selectedRate;
            }

            double resultRate = Convert.ToDouble(sumSelectRate.ToString("0.00"));
            if (resultRate > 100)
		    {
                ReturnPacket(CDefine.PACKET_SPORT_BET, "스포츠 배팅은 100배당 이상은 배팅이 불가능 합니다.", 1);
                return;
            }

            //-> 축벳 체크 (배팅한 게임 1개씩 가져와서 같은게임(childSn) 같은 방향(selected)에 배팅한 이력이 있는지 확인)
		    List<int> copy_bte_list = new List<int>(); //-> 중복 배팅 확인.
		    List<string> cukbet_no_list = new List<string>();
		    List<string> newCukbet_no_list = new List<string>();
            for ( int i = 0; i < lstParamArr.Count; i++ ) 
            {
                List<string> data_detail = lstParamArr[i];		
                int childSn = CGlobal.ParseInt(data_detail[0]);
                int selected = CGlobal.ParseInt(data_detail[1]);
                string homeTeamName = data_detail[2];
                string awayTeamName = data_detail[3];
                double rate1 = Convert.ToDouble(data_detail[4]);
                double rate2 = Convert.ToDouble(data_detail[5]);
                double rate3 = Convert.ToDouble(data_detail[6]);
			    double selectedRate = Convert.ToDouble(data_detail[7]);
			    string gameType = data_detail[8];
			    int subChildSn = CGlobal.ParseInt(data_detail[9]);

                if ( selected == 0 ) 
                    selected = 1;
                else if ( selected == 1 ) 
                    selected = 3;
                else if ( selected == 2 ) 
                    selected = 2;

                if (homeTeamName.Trim().Length == homeTeamName.Trim().Replace("보너스", "").Length)
                {
				    //-> 같은게임(childSn) 같은 방향(selected)에 배팅한 betting_no를 모두 저장
                    sql = $"select distinct(a.betting_no) from tb_total_betting a, tb_total_cart b where a.betting_no = b.betting_no and a.result = 0 and a.member_sn = '{nUser}' and a.sub_child_sn = '{subChildSn}' and a.select_no = '{selected}'";
                    DataRowCollection cukbet_row = CMySql.GetDataQuery(sql);
                    for (int j = 0; j < cukbet_row.Count; j++ ) 
                    {
                        string strBettingNo = Convert.ToString(cukbet_row[j]["betting_no"]);
                        if(cukbet_no_list.Exists(value=>value == strBettingNo))
                        {
                            int nIndex = cukbet_no_list.FindIndex(value => value == strBettingNo);
                            copy_bte_list[nIndex]++;
                        }
                        else
                        {
                            cukbet_no_list.Add(strBettingNo);
                            copy_bte_list.Add(1);
                        }
                    }
                }
            }

            //-> 배팅번호 게임중(다폴 등) 낙첨된 경기가 있으면 해당 배팅번호 제외.
            for (int j = 0; j < cukbet_no_list.Count; j++ ) 
            {
                sql = $"select betting_no from tb_total_betting where betting_no = '{cukbet_no_list[j]}' and result = 2";
                DataRowCollection loseGameRow = CMySql.GetDataQuery(sql);
                if (loseGameRow.Count == 0 ) 
                {
				    newCukbet_no_list.Add(cukbet_no_list[j]);
                }
            }

            //-> 축벳 체크 (배당가져와서 총 배당금 측정)
		    int total_ckbet_money = 0;
            for ( int i = 0; i < newCukbet_no_list.Count; i++ )
            {
			    double ckbet_rate = 1;
			    int ckbet_money = 0;
                sql = $"select select_rate, bet_money from tb_total_betting where member_sn = '{nUser}' and betting_no = '{newCukbet_no_list[i]}'";
                DataRowCollection betgame_row = CMySql.GetDataQuery(sql);
                for ( int j = 0; j < betgame_row.Count; j++ )
                {
				    ckbet_rate = ckbet_rate * Convert.ToDouble(betgame_row[j]["select_rate"]);
				    ckbet_money = CGlobal.ParseInt(betgame_row[j]["bet_money"]);
                }
			    //-> 축벳 체크된 총 배당금
			    total_ckbet_money = (total_ckbet_money + CGlobal.ParseInt(ckbet_money * ckbet_rate));
            }

            //-> 현재 배팅한 게임에 배당금.
		    int this_bedang_money = CGlobal.ParseInt(betting * resultRate);
            sql = $"select lev_max_money, lev_max_money_special, lev_max_money_single, lev_max_money_single_special, lev_max_bonus_cukbet, lev_max_bonus_cukbet_special from tb_level_config where lev = {lev}";
            DataRow array_cukbet = CMySql.GetDataQuery(sql)[0];
            int ckbet_max_money = 0;
            if ( specialType == 0 ) 
                ckbet_max_money = CGlobal.ParseInt(array_cukbet["lev_max_bonus_cukbet"]) + 0;
            else 
                ckbet_max_money = CGlobal.ParseInt(array_cukbet["lev_max_bonus_cukbet_special"]) + 0;

            if ( ckbet_max_money > 0 && total_ckbet_money != 0) 
            {
                //-> 현재 배당금 + 축벳 배당금이 축벳 제한 금액을 초과 할 경우.
                if (( this_bedang_money + total_ckbet_money ) > ckbet_max_money ) 
                {
                    string strMsg = $"축배팅 제한상환가는 {ckbet_max_money.ToString("N0")}원입니다 배팅금액을 조정해주세요.\\n[ 현재 배팅 배당금 : {this_bedang_money.ToString("N0")}원 ] \\n[ 보유 축뱃 배당금 : {total_ckbet_money.ToString("N0")})원 ]\\n\\n 합계 : {this_bedang_money.ToString("N0")} + {total_ckbet_money.ToString("N0")} = {(this_bedang_money + total_ckbet_money).ToString("N0")}원";
                    ReturnPacket(CDefine.PACKET_SPORT_BET, strMsg, 1);
                    return;
                }
            }
            
            cukbet_no_list = new List<string>();
		    newCukbet_no_list = new List<string>();

            //-> 배팅 제한 체크.
            List<CBetCheck> lstBetCheck = new List<CBetCheck>();

            for (int i = 0; i <lstParamArr.Count; i++)
            {
                List<string> data_detail = lstParamArr[i];
                int childSn = CGlobal.ParseInt(data_detail[0]);
                int selected = CGlobal.ParseInt(data_detail[1]);
                int familyId = CGlobal.ParseInt(data_detail[13]);
                double rate1 = Convert.ToDouble(data_detail[4]);
                double rate2 = Convert.ToDouble(data_detail[5]);
                double rate3 = Convert.ToDouble(data_detail[6]);
                double selectedRate = Convert.ToDouble(data_detail[7]);
                string gameType = data_detail[8];
                int subChildSn = CGlobal.ParseInt(data_detail[9]);

                if (selected == 0)
                    selected = 1;
                else if (selected == 1)
                    selected = 3;
                else if (selected == 2)
                    selected = 2;

                if(lstBetCheck.Exists(value=>value.nChildSn == childSn))
                {
                    lstBetCheck.Find(value => value.nChildSn == childSn).lstFamily.Add(familyId);
                }
                else
                {
                    CBetCheck cls = new CBetCheck();
                    cls.nChildSn = childSn;
                    cls.lstFamily.Add(familyId);
                    lstBetCheck.Add(cls);
                }
            }

            foreach(CBetCheck cls in lstBetCheck)
            {
                if (cls.lstFamily.Count == 1)
                    continue;

                sql = $"select * from tb_child where sn = {cls.nChildSn}";
                DataRowCollection lstChild = CMySql.GetDataQuery(sql);
                if (lstChild != null && lstChild.Count > 0)
                {
                    int nty = 0;
                    string strType = string.Empty;
                    if (_gameType == "multi")
                    {
                        nty = 1;
                        strType = "국내형";
                    }
                    else if (_gameType == "abroad")
                    {
                        nty = 2;
                        strType = "해외형";
                    }
                    else if (_gameType == "live")
                    {
                        nty = 3;
                        strType = "라이브";
                    }
                       
                    int sport_id = CGlobal.ParseInt(lstChild[0]["sport_id"]);
                    string strSportName = Convert.ToString(lstChild[0]["sport_name"]);

                    sql = $"SELECT tb_cross_limit.*, tb_sports.name AS sportsName FROM tb_cross_limit LEFT JOIN tb_sports ON tb_cross_limit.sport_id = tb_sports.sn WHERE tb_cross_limit.sport_id = {sport_id} AND tb_cross_limit.type_id = {nty}";
                    DataRowCollection lstLimit = CMySql.GetDataQuery(sql);

                    if (lstLimit == null || lstLimit.Count == 0)
                    {
                        int nFamilyCnt = cls.lstFamily.Count;
                        if (nFamilyCnt > 1)
                        {
                            ReturnPacket(CDefine.PACKET_SPORT_BET, $"{strType} {strSportName}에서 조합배팅은 제한합니다.", 1);
                            return;
                        }
                    }
                    else
                    {
                        bool bCheck = false;
                        foreach (DataRow lmtInfo in lstLimit)
                        {
                            string strScript = Convert.ToString(lmtInfo["cross_script"]);
                            string[] arrScript = strScript.Split('+');
                            List<int> lstFamily = new List<int>();

                            foreach (string str in arrScript)
                            {
                                try
                                {
                                    int nfamily = CGlobal.ParseInt(str);
                                    lstFamily.Add(nfamily);

                                }
                                catch (Exception err)
                                {
                                    Console.WriteLine(err.Message);
                                    lstFamily.Add(0);
                                }
                            }

                            int index = 0;
                            foreach (int nf in lstFamily)
                            {
                                if (cls.lstFamily.Exists(value => value == nf))
                                {
                                    index++;
                                }
                            }

                            if (index == lstFamily.Count)
                            {
                                bCheck = true;
                                break;
                            }
                               
                        }

                        if (!bCheck)
                        {
                            string strFamilyName = GetFamilyName(cls.lstFamily);
                            // ReturnPacket(CDefine.PACKET_SPORT_BET, $"{strType} {strSportName}에서 {strFamilyName} 조합배팅은 제한합니다.", 1);
                            ReturnPacket(CDefine.PACKET_SPORT_BET, $"{strType} {strSportName}에서 제한된 조합배팅입니다.", 1);
                            return;
                        }
                    }
                }
            }

            int lastChildSn = 0;
            //' tb_betting 에 1게임씩 추가
            for (int i = 0; i < lstParamArr.Count; i++)
		    {
                List<string> data_detail = lstParamArr[i];
			
			    int childSn = CGlobal.ParseInt(data_detail[0]);
			    int selected = CGlobal.ParseInt(data_detail[1]);
			    float rate1 = Convert.ToSingle(data_detail[4]);
                float rate2 = Convert.ToSingle(data_detail[5]);
                float rate3 = Convert.ToSingle(data_detail[6]);
			    double selectedRate = Convert.ToDouble(data_detail[7]);
			    int gameType = CGlobal.ParseInt(data_detail[8]);
			    int subChildSn = CGlobal.ParseInt(data_detail[9]);
			    long betid = Convert.ToInt64(data_detail[14]);

                if (selected == 0)			
                    selected = 1;
                else if(selected == 1)    
                    selected = 3;
                else if(selected == 2)    
                    selected = 2;

                if(data_detail[12] == "이벤트")
                {
                    rate1 = Convert.ToSingle(data_detail[7]);
                    rate2 = rate1;
                    rate3 = rate1;

                    sql = $"insert into tb_total_betting(sub_child_sn,member_sn,betting_no,select_no,home_rate,draw_rate,away_rate, select_rate,game_type,event,result,kubun,bet_money,s_type, betid) ";
                    sql += $"values({subChildSn}, {nUser}, '{protoId}', {selected}, {rate1}, {rate2}, {rate3}, {selectedRate}, {gameType}, 1, 1, '{buy}', {betting}, 1, 'bonus')";
                    CMySql.ExcuteQuery(sql);
                }
                else
                {
                    lastChildSn = childSn;
                    CGame clsGame = CGlobal.GetGameInfoByCode(childSn);
                    CSports clsSports = CGlobal.GetSportsInfoByCode(clsGame.m_nSports);

                    CBetRate clsRate = null;
                    if (_gameType == "multi")
                        clsRate = clsGame.GetPrematchBetRateList().Find(value => value.m_nCode == subChildSn);
                    else if (_gameType == "abroad")
                        clsRate = clsGame.GetPrematchBetRateList().Find(value => value.m_nCode == subChildSn);
                    else if (_gameType == "live")
                        clsRate = clsGame.GetLiveBetRateList().Find(value => value.m_nCode == subChildSn);

                    if (clsRate == null)
                        continue;

                    rate1 = clsRate.m_fHRate;
                    rate2 = clsRate.m_fDRate;
                    rate3 = clsRate.m_fARate;

                    string score = $"{clsGame.m_nHomeScore}-{clsGame.m_nAwayScore}";
                    int live = _gameType == "live" ? 1 : 0;

                    sql = $"insert into tb_total_betting(sub_child_sn,member_sn,betting_no,select_no,home_rate,draw_rate,away_rate, select_rate,game_type,event,result,kubun,bet_money,s_type,betid, score, live) ";
                    sql += $"values({subChildSn}, {nUser}, '{protoId}', {selected}, {rate1}, {rate2}, {rate3}, {selectedRate}, {gameType}, 0, 0, '{buy}', {betting}, 1, '{betid}', '{score}', {live})";
                    CMySql.ExcuteQuery(sql);
                }
            }

            //-> 마지막 $childSn을 가지고 special 코드를 가져온다. (정산부분 게임을 분류하기 위해)
            CGame clsLastGame = CGlobal.GetGameInfoByCode(lastChildSn);
            int lastSpecialCode = clsLastGame.m_nSpecial;

            int recommendSn = CGlobal.ParseInt(userInfo["recommend_sn"]);
            int rollingSn = CGlobal.ParseInt(userInfo["rolling_sn"]);
            string author = Convert.ToString(userInfo["nick"]);
            int accountEnable = 1;
            if (Convert.ToString(userInfo["mem_status"]) == "G")
                accountEnable = 0;
            string bettingIp = Convert.ToString(Context.UserEndPoint.Address);
            string bet_date = CMyTime.GetMyTimeStr("yyyy-MM-dd");

            sql = $"insert into tb_total_cart(member_sn, betting_no, parent_sn, regdate, operdate, kubun, result, betting_cnt, before_money, betting_money, result_rate, result_money, partner_sn, rolling_sn, bouns_rate, user_del,bet_date, is_account,betting_ip, last_special_code,logo,s_type) values({nUser}, '{protoId}', 0, now(), now(), '{buy}', 0, {betting_cnt}, {dbCash}, {betting}, {resultRate}, 0, {recommendSn}, {rollingSn}, 0, 'N', now(), {accountEnable}, '{bettingIp}', {lastSpecialCode}, 'gadget', 0)";
            CMySql.ExcuteQuery(sql);

            sql = $"select * from tb_point_config";
            DataRow bonusInfo = CMySql.GetDataQuery(sql)[0];
            string chkFolderOption = Convert.ToString(bonusInfo["chk_folder"]);
            if (chkFolderOption == "1")
            {
			    int bouns_rate_percent = CGlobal.ParseInt(bonusInfo[$"folder_bouns{betting_cnt}"]);
                sql = $"update tb_total_cart set bouns_rate = {bouns_rate_percent} where member_sn = {nUser} and betting_no = '{protoId}'";
                CMySql.ExcuteQuery(sql);
            }

            string mem_status = Convert.ToString(userInfo["mem_status"]);
            int before = CGlobal.ParseInt(userInfo["g_money"]);
            int after = before + betting;
            sql = $"update tb_member set g_money = g_money - {betting} where sn = {nUser}";
            CMySql.ExcuteQuery(sql);

            if (mem_status == "N")
            {
			    sql = $"insert into tb_money_log(member_sn,amount,before_money,after_money,regdate,state, status_message, log_memo) values({nUser}, {betting}, {before}, {after}, now(), 3, '배팅', '')";
                CMySql.ExcuteQuery(sql);
            }

            /*베팅수가 10개이상이고, 1만원이상인 경기는 자동으로 잭팟게시글을 남긴다.*/
            if (betting_cnt >= 10 && betting >= 10000)
            {
                string title = $"{author} 님의 잭팟베팅^^";
                sql = $"insert into tb_content(province, title, imgnum, pic, author, top, content, ip, time, hit, betting_no, logo) values('9', '{title}', '0', '', '{author}', '1', '잭팟이벤트', '{bettingIp}', now(), '0', '{protoId}', 'gadget')";
                CMySql.ExcuteQuery(sql);
            }

            if(specialType == 22)
            {
                //-> 배팅 알람 업데이트.
                if ( betting >= 300000 ) 
                {
                    sql = $"update tb_alarm_flag set betting_vfootball_big = betting_vfootball_big + 1 where idx = 1";
                } 
                else
                {
				    sql = $"update tb_alarm_flag set betting_vfootball = betting_vfootball + 1 where idx = 1";
                }
            }
            else
            {
                //-> 배팅 알람 업데이트.
                if (betting >= 300000)
                {
                    sql = $"update tb_alarm_flag set betting_sport_big = betting_sport_big + 1 where idx = 1";
                }
                else
                {
                    sql = $"update tb_alarm_flag set betting_sport = betting_sport + 1 where idx = 1";
                }

                CMySql.ExcuteQuery(sql);
            }

            ReturnPacket(CDefine.PACKET_SPORT_BET, "배팅신청이 완료되였습니다.", 0);
        }

        private int GetCrossLimitCount(string strGameType)
        {
            int nType = 0;
            if (strGameType == "multi")
                nType = 1;
            else if (strGameType == "abroad")
                nType = 2;
            else if (strGameType == "live")
                nType = 3;

            string sql = $"SELECT * FROM tb_cross_limit WHERE type_id = {nType}";
            DataRowCollection list = CMySql.GetDataQuery(sql);

            return list.Count;
        }

        private string IsValidBetting(string betcontent)
        {
            string[] betArray = betcontent.Split('#');
            int m_count = betArray.Length;

            foreach(string bet_detail in betArray)
            {
                if (bet_detail.IndexOf("3폴더") == -1)
                {
                    if (bet_detail.IndexOf("5폴더") == -1)
                    {
                        continue;
                    }
                    else
                    {
                        if (m_count < 7)
                            return "5폴더 이상만 베팅 가능합니다.";
                    }
                }
                else
                {
                    if (m_count < 5)
                        return "3폴더 이상만 베팅 가능합니다.";
                }
            }

            return "true";
        }

        // 승패+오버
        private bool CheckRule_wl_over(string[] data)
        {
            for (int i = 0; i < data.Length - 1; i++) 
            {
                string[] game_spec = data[i].Split("||");
			    int childSn = CGlobal.ParseInt(game_spec[0]);
                CGame i_item = CGlobal.GetGameInfoByCode(childSn);
			    int i_item_checkbox_index = CGlobal.ParseInt(game_spec[1]);

                for ( int j = (i + 1) ; j < data.Length - 1; j++ )
                {
				    game_spec = data[j].Split("||");
				    childSn = CGlobal.ParseInt(game_spec[0]);
                    CGame j_item = CGlobal.GetGameInfoByCode(childSn);
				    int j_item_checkbox_index = CGlobal.ParseInt(game_spec[1]);
                    if (i_item.m_nType == 1 && (i_item_checkbox_index == 0 || i_item_checkbox_index == 2)) 
                    {
                        if (i_item.m_nHomeTeam == j_item.m_nHomeTeam && i_item.m_nAwayTeam == j_item.m_nAwayTeam && j_item.m_nType == 4 && j_item_checkbox_index == 2) 
                        {
                            return false;
                        }
                    } 
                    else if ( i_item.m_nType == 4 && i_item_checkbox_index == 2 && j_item.m_nHomeTeam == i_item.m_nHomeTeam && j_item.m_nAwayTeam == i_item.m_nAwayTeam)
                    { 
                        if (j_item.m_nType == 1 && (j_item_checkbox_index == 0 || j_item_checkbox_index == 2)) 
                        {
                            return false;
                        }
                    }
                } 
            } 
            return true;
        }

        // 승패+언더
        private bool CheckRule_wl_under(string[] data)
        {
            for (int i = 0; i < data.Length - 1; i++) 
            {
                string[] game_spec = data[i].Split("||");
                int childSn = CGlobal.ParseInt(game_spec[0]);
                CGame i_item = CGlobal.GetGameInfoByCode(childSn);
                int i_item_checkbox_index = CGlobal.ParseInt(game_spec[1]);

                for ( int j = (i + 1) ; j < data.Length - 1; j++ ) 
                {
                    game_spec = data[j].Split("||");
                    childSn = CGlobal.ParseInt(game_spec[0]);
                    CGame j_item = CGlobal.GetGameInfoByCode(childSn);
                    int j_item_checkbox_index = CGlobal.ParseInt(game_spec[1]);

                    if ( i_item.m_nType == 1 && (i_item_checkbox_index == 0 || i_item_checkbox_index == 2)) 
                    {
                        if (i_item.m_nHomeTeam == j_item.m_nHomeTeam && i_item.m_nAwayTeam == j_item.m_nAwayTeam && j_item.m_nType == 4 && j_item_checkbox_index == 0 ) 
                        {
                            return false;
                        }
                    } 
                    else if ( i_item.m_nType == 4 && i_item_checkbox_index == 0 && j_item.m_nHomeTeam == i_item.m_nHomeTeam && j_item.m_nAwayTeam == i_item.m_nAwayTeam) 
                    {
                        if (j_item.m_nType == 1 && (j_item_checkbox_index == 0 || j_item_checkbox_index == 2)) 
                        {
                            return false;
                        }
                    }
                } //-> end FOR J
            } //-> end FOR I
            return true;
        }

        // 무 + 오버
        private bool CheckRule_d_over(string[] data)
        {
            for (int i = 0; i < data.Length - 1; i++)
            {
                string[] game_spec = data[i].Split("||");
                int childSn = CGlobal.ParseInt(game_spec[0]);
                CGame i_item = CGlobal.GetGameInfoByCode(childSn);
                int i_item_checkbox_index = CGlobal.ParseInt(game_spec[1]);

                for (int j = (i + 1); j < data.Length - 1; j++)
                {
                    game_spec = data[j].Split("||");
                    childSn = CGlobal.ParseInt(game_spec[0]);
                    CGame j_item = CGlobal.GetGameInfoByCode(childSn);
                    int j_item_checkbox_index = CGlobal.ParseInt(game_spec[1]);

                    if ( i_item.m_nType == 1 && i_item_checkbox_index == 1) 
                    {
                        if (i_item.m_nHomeTeam == j_item.m_nHomeTeam && i_item.m_nAwayTeam == j_item.m_nAwayTeam && j_item.m_nType == 4 && j_item_checkbox_index == 2 ) 
                        {
                            return false;
                        }
                    } 
                    else if ( i_item.m_nType == 4 && i_item_checkbox_index == 2) 
                    {
                        if (i_item.m_nHomeTeam == j_item.m_nHomeTeam && i_item.m_nAwayTeam == j_item.m_nAwayTeam && j_item.m_nType == 1 &&  j_item_checkbox_index == 1) 
                        {
                            return false;
                        }
                    }
                } //-> end FOR J
            } //-> end FOR I
            return true;
        }

        // 무+언더
        private bool CheckRule_d_under(string[] data)
        {
            for (int i = 0; i < data.Length - 1; i++)
            {
                string[] game_spec = data[i].Split("||");
                int childSn = CGlobal.ParseInt(game_spec[0]);
                CGame i_item = CGlobal.GetGameInfoByCode(childSn);
                int i_item_checkbox_index = CGlobal.ParseInt(game_spec[1]);

                for (int j = (i + 1); j < data.Length - 1; j++)
                {
                    game_spec = data[j].Split("||");
                    childSn = CGlobal.ParseInt(game_spec[0]);
                    CGame j_item = CGlobal.GetGameInfoByCode(childSn);
                    int j_item_checkbox_index = CGlobal.ParseInt(game_spec[1]);

                    if (i_item.m_nType == 1 && i_item_checkbox_index == 1)
                    {
                        if (i_item.m_nHomeTeam == j_item.m_nHomeTeam && i_item.m_nAwayTeam == j_item.m_nAwayTeam && j_item.m_nType == 4 && j_item_checkbox_index == 0 ) 
                        {
                            return false;
                        }
                    }
                    else if (i_item.m_nType == 4 && i_item_checkbox_index == 0)
                    {
                        if (i_item.m_nHomeTeam == j_item.m_nHomeTeam && i_item.m_nAwayTeam == j_item.m_nAwayTeam && j_item.m_nType == 1 &&  j_item_checkbox_index == 1) 
                        {
                            return false;
                        }
                    }
                } //-> end FOR J
            } //-> end FOR I
            return true;
        }

        // 핸디+언더/오버
        private bool CheckRule_handi_unov(string[] data)
        {
            for (int i = 0; i < data.Length - 1; i++)
            {
                string[] game_spec = data[i].Split("||");
                int childSn = CGlobal.ParseInt(game_spec[0]);
                CGame i_item = CGlobal.GetGameInfoByCode(childSn);

                for (int j = (i + 1); j < data.Length - 1; j++)
                {
                    game_spec = data[j].Split("||");
                    childSn = CGlobal.ParseInt(game_spec[0]);
                    CGame j_item = CGlobal.GetGameInfoByCode(childSn);

                    if ( i_item.m_nType == 2) 
                    {
                        if (i_item.m_nHomeTeam == j_item.m_nHomeTeam && i_item.m_nAwayTeam == j_item.m_nAwayTeam && j_item.m_nType == 4) 
                        {
                            return false;
                        }
                    } 
                    else if ( i_item.m_nType == 4) 
                    {
                        if (i_item.m_nHomeTeam == j_item.m_nHomeTeam && i_item.m_nAwayTeam == j_item.m_nAwayTeam && j_item.m_nType == 2) 
                        {
                            return false;
                        }
                    }
                } //-> end FOR J
            } //-> end FOR I
            return true;
        }

        private void OnRequestList(string strPacket)
        {
            m_reqParam = JsonConvert.DeserializeObject<CLSportsReqList>(strPacket);
            SendGameListPacket(CDefine.PACKET_SPORT_LIST);
        }

        public void SendGameListPacket(int nPacketCode)
        {
            List<CGame> lstGame = null;

            try
            {
                lstGame = CGlobal.GetGameList().FindAll(value => value.CheckGame() && value.m_nCountry > 0 && value.m_nBlock == 0).OrderBy(value => value.m_strGameTime).ThenBy(value => value.m_nLeague).ToList();
            }
            catch(Exception err)
            {
                CGlobal.ShowConsole(err.Message);
                return;
            }
            
            lstGame = lstGame.FindAll(value => value.GetGameDateTime() < CMyTime.GetMyTime().AddDays(3) && value.IsFinishGame() == false);
            
            int nSports = 0;
            if (m_reqParam == null)
                return;

            if (m_reqParam.m_strSports == "soccer")
                nSports = 6046;
            else if (m_reqParam.m_strSports == "basketball")
                nSports = 48242;
            else if (m_reqParam.m_strSports == "volleyball")
                nSports = 154830;
            else if (m_reqParam.m_strSports == "baseball")
                nSports = 154914;
            else if (m_reqParam.m_strSports == "hockey")
                nSports = 35232;

            if (nSports > 0)
                lstGame = lstGame.FindAll(value => value.m_nSports == nSports);
            if (m_reqParam.m_nLeague > 0)
                lstGame = lstGame.FindAll(value => value.m_nLeague == m_reqParam.m_nLeague);
            
            if (m_reqParam.m_nLive == 2)
            {
                lstGame = lstGame.FindAll(value => value.CheckLive());
            }
            else
            {
                lstGame = lstGame.FindAll(value => value.CheckLive() == false || value.m_nStatus == 9);
                lstGame = lstGame.FindAll(value => value.GetGameDateTime() > CMyTime.GetMyTime().AddSeconds(-5)
                                                && value.GetPrematchBetRateList().Exists(val => val.CheckWinDrawLose())
                                                && value.GetPrematchBetRateList().Find(val => val.CheckWinDrawLose()).m_nStatus < 2);
            }


            List<CLSportsPacket> lstSendPacket = new List<CLSportsPacket>();
            int nTotalCnt = lstGame.Count;

            int nIndex = m_reqParam.m_nPageIndex * m_reqParam.m_nPageSize;
            if (lstGame.Count <= nIndex)
            {
                ReturnPacket(nPacketCode, JsonConvert.SerializeObject(lstSendPacket), 0);
                return;
            }


            if (m_reqParam.m_nLive == 0)
            {
                int[] lstFilter1 = { 1, 2, 3, 28, 52, 226, 342, 866 };
                lstGame = lstGame.FindAll(info=> info.GetPrematchBetRateList().Exists(value => (value.m_nStatus == 1 || value.CheckWinDrawLose()) && lstFilter1.ToList().Exists(val => val == value.m_nMarket)));
            }
            else if (m_reqParam.m_nLive == 1)
            {
                lstGame = lstGame.FindAll(info => info.GetPrematchBetRateList().Exists(value => (value.m_nStatus == 1 || value.CheckWinDrawLose())));
            }
            else if (m_reqParam.m_nLive == 2)
            {
                lstGame = lstGame.FindAll(info => info.GetLiveBetRateList().Exists(value => (value.m_nStatus == 1 || value.CheckWinDrawLose())));
            }

            int nGroup = 1;

            for (int i = nIndex; i < nIndex + m_reqParam.m_nPageSize; i++)
            {
                if (i >= lstGame.Count)
                    break;

                CGame clsGame = lstGame[i];
                CLSportsPacket sendPacket = new CLSportsPacket();

                sendPacket.m_nGame = clsGame.m_nCode;
                sendPacket.m_nFixtureID = clsGame.m_nFixtureID;
                sendPacket.m_nSports = clsGame.m_nSports;
                sendPacket.m_strSportName = CGlobal.GetSportsInfoByCode(clsGame.m_nSports).m_strKo;
                sendPacket.m_nLeague = clsGame.m_nLeague;
                sendPacket.m_strLeagueName = CGlobal.GetLeagueInfoByCode(clsGame.m_nLeague).m_strKo;
                sendPacket.m_strLeagueImg = CGlobal.GetLeagueInfoByCode(clsGame.m_nLeague).m_strImg;
                sendPacket.m_strHomeTeam = CGlobal.GetTeamInfoByCode(clsGame.m_nHomeTeam).m_strKo;
                sendPacket.m_strAwayTeam = CGlobal.GetTeamInfoByCode(clsGame.m_nAwayTeam).m_strKo;
                sendPacket.m_strDate = clsGame.m_strDate;
                sendPacket.m_strHour = clsGame.m_strHour;
                sendPacket.m_strMin = clsGame.m_strMin;
                sendPacket.m_nStatus = clsGame.m_nStatus;
                CPeriod period = CGlobal.GetPeriodInfoByCode(clsGame.m_nSports, clsGame.m_nPeriod);
                if (period == null)
                    sendPacket.m_strPeriod = "경기전";
                else
                    sendPacket.m_strPeriod = period.m_strKo;

                sendPacket.m_nHomeScore = clsGame.m_nHomeScore;
                sendPacket.m_nAwayScore = clsGame.m_nAwayScore;

                sendPacket.m_lstDetail = new List<CLSportsDPacket>();

                List<CBetRate> lstBetRate = null;
                if (m_reqParam.m_nLive == 0)
                {
                    int[] lstFilter1 = { 1, 2, 3, 28, 52, 226, 342, 866 };
                    lstBetRate = clsGame.GetPrematchBetRateList().FindAll(value => (value.m_nStatus == 1 || value.CheckWinDrawLose()) && lstFilter1.ToList().Exists(val => val == value.m_nMarket));
                }
                else if(m_reqParam.m_nLive == 1)
                {
                    lstBetRate = clsGame.GetPrematchBetRateList().FindAll(value => (value.m_nStatus == 1 || value.CheckWinDrawLose()));
                }
                else if(m_reqParam.m_nLive == 2)
                {
                    lstBetRate = clsGame.GetLiveBetRateList().FindAll(value => (value.m_nStatus == 1 || value.CheckWinDrawLose()));
                }

                lstBetRate = lstBetRate.OrderBy(value => value.m_nMarket).ThenBy(value => value.m_dOrder).ThenBy(value => value.m_strHLine).ThenBy(value => value.m_strHName).ToList();

                foreach (CBetRate info in lstBetRate)
                {
                    CLSportsDPacket packet = new CLSportsDPacket();
                    packet.m_nMarket = info.m_nMarket;
                    packet.m_strMarket = CGlobal.GetMarketInfoByCode(info.m_nMarket).m_strKo;
                    packet.m_nHBetCode = info.m_strHBetCode;
                    packet.m_nDBetCode = info.m_strDBetCode;
                    packet.m_nABetCode = info.m_strABetCode;
                    packet.m_fHRate = info.m_fHRate;
                    packet.m_fDRate = info.m_fDRate;
                    packet.m_fARate = info.m_fARate;
                    packet.m_fHBase = info.m_fHBase;
                    packet.m_fDBase = info.m_fDBase;
                    packet.m_fABase = info.m_fABase;
                    packet.m_strHLine = info.m_strHLine;
                    packet.m_strDLine = info.m_strDLine;
                    packet.m_strALine = info.m_strALine;
                    packet.m_strBLine = info.m_strBLine;
                    packet.m_strHName = info.m_strHName;
                    packet.m_strDName = info.m_strDName;
                    packet.m_strAName = info.m_strAName;
                    packet.m_nStatus = info.m_nStatus;
                    packet.m_nFamily = info.m_nFamily;

                    sendPacket.m_lstDetail.Add(packet);
                }

                if (lstSendPacket.Count == 0)
                {
                    sendPacket.m_nGroup = nGroup;
                    sendPacket.m_lstSportsCnt = new List<CLSportsSportsCnt>();
                    CalcGameCount(lstGame, sendPacket);
                    sendPacket.m_nTotalCnt = nTotalCnt;
                }
                else
                {
                    CLSportsPacket back = lstSendPacket[lstSendPacket.Count - 1];
                    if (back.m_strDate == sendPacket.m_strDate && back.m_strHour == sendPacket.m_strHour && back.m_strMin == sendPacket.m_strMin && back.m_strLeagueName.Trim() == sendPacket.m_strLeagueName.Trim())
                    {
                        sendPacket.m_nGroup = nGroup;
                    }
                    else
                    {
                        nGroup++;
                        sendPacket.m_nGroup = nGroup;
                    }
                }

                lstSendPacket.Add(sendPacket);
            }

            ReturnPacket(nPacketCode, JsonConvert.SerializeObject(lstSendPacket), 0);
        }
        
        private void CalcGameCount(List<CGame> lstGame, CLSportsPacket packet)
        {
            packet.m_lstSportsCnt = new List<CLSportsSportsCnt>();

            List<CSports> lstSports = CGlobal.GetSportsList().FindAll(value => value.m_nUse == 1);
            foreach(CSports info in lstSports)
            {
                CLSportsSportsCnt countInfo = new CLSportsSportsCnt();
                countInfo.m_nSports = info.m_nCode;
                countInfo.m_strName = info.m_strKo;

                List<CGame> lstSportsCnt = lstGame.FindAll(value => value.m_nSports == info.m_nCode);
                countInfo.m_nCount = lstSportsCnt.Count;

                countInfo.m_lstCountryCnt = new List<CLSportsContryCnt>();
                foreach (CGame clsGame in lstSportsCnt)
                {
                    CLSportsContryCnt clsCountryCnt = countInfo.m_lstCountryCnt.Find(value => value.m_nCountry == clsGame.m_nCountry);
                    if (clsCountryCnt == null)
                    {
                        clsCountryCnt = new CLSportsContryCnt();
                        clsCountryCnt.m_nCountry = clsGame.m_nCountry;
                        clsCountryCnt.m_strName = CGlobal.GetCountryInfoByCode(clsGame.m_nCountry).m_strKo;
                        clsCountryCnt.m_strImg = CGlobal.GetCountryInfoByCode(clsGame.m_nCountry).m_strImg;

                        clsCountryCnt.m_lstLeagueCnt = new List<CLSportsLeagueCnt>();
                        countInfo.m_lstCountryCnt.Add(clsCountryCnt);
                    }

                    clsCountryCnt.m_nCount++;

                    CLSportsLeagueCnt clsLeagueCnt = clsCountryCnt.m_lstLeagueCnt.Find(value => value.m_nLeague == clsGame.m_nLeague);
                    if(clsLeagueCnt == null)
                    {
                        clsLeagueCnt = new CLSportsLeagueCnt();
                        clsLeagueCnt.m_nLeague = clsGame.m_nLeague;
                        clsLeagueCnt.m_strName = CGlobal.GetLeagueInfoByCode(clsGame.m_nLeague).m_strKo;
                        clsLeagueCnt.m_strImg = CGlobal.GetLeagueInfoByCode(clsGame.m_nLeague).m_strImg;

                        clsCountryCnt.m_lstLeagueCnt.Add(clsLeagueCnt);
                    }
                    clsLeagueCnt.m_nCount++;
                }

                packet.m_lstSportsCnt.Add(countInfo);
            }
        }

        private string GetFamilyName(List<int> lstFamily)
        {
            string strFimily = string.Empty;
            for(int i=0; i<lstFamily.Count; i++)
            {
                string sql = $"SELECT * FROM tb_market_family WHERE family_id = {lstFamily[i]}";
                DataRow row = CMySql.GetDataQuery(sql)[0];
                strFimily += Convert.ToString(row["family_name"]);
                if (i < lstFamily.Count - 1)
                    strFimily += "+";
            }

            return strFimily;
        }

        private static void OnAjaxRequestList(CGameServer clsServer)
        {
            while(clsServer.m_bThread)
            {
                if(clsServer.m_reqParam != null) 
                {
                    clsServer.SendGameListPacket(CDefine.PACKET_SPORT_AJAX);
                }

                Thread.Sleep(1000);
            }
        }
    }

    public class CBetCheck
    {
        public int nChildSn;
        public List<int> lstFamily = new List<int>();
    }
}
