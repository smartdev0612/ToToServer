using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CPeriod : MPeriod, ILSports
    {
        public MBase GetModel()
        {
            return this as MBase;
        }

        public void LoadInfo(DataRow info)
        {
            m_nCode = CGlobal.ParseInt(info["sn"]);
            m_nSports = CGlobal.ParseInt(info["sport_sn"]);
            m_nPeriod = CGlobal.ParseInt(info["period_sn"]);
            m_strEn = Convert.ToString(info["period_desc_en"]);
            m_strKo = Convert.ToString(info["period_desc_ko"]);
        }
    }
}
