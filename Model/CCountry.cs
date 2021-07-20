using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CCountry : MCountry, ILSports
    {
        public MBase GetModel()
        {
            return this as MCountry;
        }

        public void LoadInfo(DataRow info)
        {
            m_nCode = CGlobal.ParseInt(info["sn"]);
            m_strEn = Convert.ToString(info["name_en"]);
            m_strKo = Convert.ToString(info["name"]);
            m_strImg = Convert.ToString(info["img"]);
            m_nUse = CGlobal.ParseInt(info["inactive"]);
        }
    }
}
