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
            m_nPriorityFoot = CGlobal.ParseInt(info["priority_foot"]);
            m_nPriorityBasket = CGlobal.ParseInt(info["priority_basket"]);
            m_nPriorityBase = CGlobal.ParseInt(info["priority_base"]);
            m_nPriorityVolley = CGlobal.ParseInt(info["priority_volley"]);
            m_nPriorityHocky = CGlobal.ParseInt(info["priority_hocky"]);
            m_nPriorityEsports = CGlobal.ParseInt(info["priority_esports"]);
        }
    }
}
