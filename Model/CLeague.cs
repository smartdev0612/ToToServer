using Newtonsoft.Json.Linq;
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
            m_nSn = CGlobal.ParseInt(info["sn"]);
            m_strEn = Convert.ToString(info["name_en"]);
            m_strKo = Convert.ToString(info["name"]);
            m_strImg = Convert.ToString(info["lg_img"]);
            m_nCountry = CGlobal.ParseInt(info["nation_sn"]);
            m_nSports = CGlobal.ParseInt(info["sport_sn"]);
            m_nUse = CGlobal.ParseInt(info["is_use"]);
        }

        public void SaveLeagueInfo(int nSports)
        {
            string strUrl = $"{CDefine.API_URL}/OddService/GetLeagues?Username={CDefine.API_USERNAME}&Password={CDefine.API_PASSWORD}&Guid={CDefine.API_GUID}&sports={nSports}";
            string str = CHttp.GetResponseString(strUrl);

            JToken objPacket = JObject.Parse(str);
            if (objPacket["Body"] != null)
            {
                List<JToken> list = objPacket["Body"].ToList();
                foreach (JToken obj in list)
                {
                    long nLeagueId = Convert.ToInt64(obj["Id"]);
                    string strName = Convert.ToString(obj["Name"]);
                    long nLocationId = Convert.ToInt64(obj["LocationId"]);
                    long nSportId = Convert.ToInt64(obj["SportId"]);
                    CEntry.SaveLeagueToDB(nLeagueId, strName, nLocationId, nSportId);
                }
            }
            
        }
    }
}
