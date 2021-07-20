using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CSports : MSports, ILSports
    {
        public MBase GetModel()
        {
            return this as MSports;
        }

        public void LoadInfo(DataRow info)
        {
            m_nCode = CGlobal.ParseInt(info["sn"]);
            m_strKo = Convert.ToString(info["name"]).Replace("'", " ").Trim();
            m_strEn = Convert.ToString(info["name_en"]).Replace("'", " ").Trim();
            m_strImg = Convert.ToString(info["img"]);
            m_nUse = CGlobal.ParseInt(info["use"]);
        }
    }
}
