using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CMarket : MMarket, ILSports
    {
        public MBase GetModel()
        {
            return this as MMarket;
        }

        public void LoadInfo(DataRow info)
        {
            m_nCode = CGlobal.ParseInt(info["mid"]);
            m_strEn = Convert.ToString(info["mname_en"]);
            m_strKo = Convert.ToString(info["mname_ko"]);
            m_nFamily = CGlobal.ParseInt(info["mfamily"]);
            m_nUse = CGlobal.ParseInt(info["muse"]);
            m_nPeriod = CGlobal.ParseInt(info["period"]);
            m_fRate = Convert.ToDouble(info["frate"]);
        }
    }
}
