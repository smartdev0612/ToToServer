using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CBetting : MBetting, ILSports
    {
        public MBase GetModel()
        {
            return this as MBetting;
        }

        public void LoadInfo(DataRow info)
        {
            m_nCode = CGlobal.ParseInt(info["sn"]);
            m_nSubChildSn = CGlobal.ParseInt(info["sub_child_sn"]);
            m_nMemberSn = CGlobal.ParseInt(info["member_sn"]);
            m_strBettingNo = Convert.ToString(info["betting_no"]);
            m_nSelectNo = CGlobal.ParseInt(info["select_no"]);
            m_fHomeRate = Convert.ToDouble(info["home_rate"]);
            m_fAwayRate = Convert.ToDouble(info["away_rate"]);
            m_fDrawRate = Convert.ToDouble(info["draw_rate"]);
            m_fSelectRate = Convert.ToDouble(info["select_rate"]);
            m_strBetID = Convert.ToString(info["betid"]);
            m_nGameType = CGlobal.ParseInt(info["game_type"]);
            m_nResult = CGlobal.ParseInt(info["result"]);
            m_nBetMoney = CGlobal.ParseInt(info["bet_money"]);
            m_nStype = CGlobal.ParseInt(info["s_type"]);
            m_nPass = CGlobal.ParseInt(info["pass"]);
            m_strScore = Convert.ToString(info["score"]);
            m_nLive = CGlobal.ParseInt(info["live"]);
        }
    }
}
