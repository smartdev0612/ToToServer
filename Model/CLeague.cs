using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CLeague : MLeague, ILSports
    {
        public MBase GetModel()
        {
            return this as MLeague;
        }

        public void LoadInfo(DataRow info)
        {
            m_nCode = CGlobal.ParseInt(info["lsports_league_sn"]);
            m_strEn = Convert.ToString(info["name_en"]);
            m_strKo = Convert.ToString(info["name"]);
            m_strImg = Convert.ToString(info["lg_img"]);
            m_nCountry = CGlobal.ParseInt(info["nation_sn"]);
            m_nSports = CGlobal.ParseInt(info["sport_sn"]);
            m_nUse = CGlobal.ParseInt(info["is_use"]);
        }
    }
}
