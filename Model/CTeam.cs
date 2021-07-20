using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CTeam : MTeam, ILSports
    {
        public MBase GetModel()
        {
            return this as MTeam;
        }

        public void LoadInfo(DataRow info)
        {
            m_nCode = CGlobal.ParseInt(info["Team_Id"]);
            m_strKo = Convert.ToString(info["Team_Name_Kor"]).Replace("'", " ").Trim();
            m_strEn = Convert.ToString(info["Team_Name"]).Replace("'", " ").Trim();
            m_nSports = CGlobal.ParseInt(info["Sport_Id"]);
            m_nLeague = CGlobal.ParseInt(info["League_Id"]);
            m_nCountry = CGlobal.ParseInt(info["Location_Id"]);
        }
    }
}
