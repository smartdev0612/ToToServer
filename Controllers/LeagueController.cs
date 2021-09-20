using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeagueController : ControllerBase
    {
        [HttpGet]
        public string Get(int nCmd, string strValue)   //public IEnumerable<string> Get()
        {
            try
            {
                switch (nCmd)
                {
                    case 0x01:      //리그정보수정
                        UpdateLeague(strValue);
                        break;
                    case 0x02:      //리그삭제
                        DeleteLeague(strValue);
                        break;
                    case 0x03:      //선택된 리그들 삭제
                        DeleteLeagues(strValue);
                        break;
                }
            }
            catch (Exception err)
            {
                CGlobal.ShowConsole(err.Message);
            }

            return "success";
        }

        private void UpdateLeague(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nSn = CGlobal.ParseInt(param["sn"]);
            int nLSportsLeagueSn = CGlobal.ParseInt(param["lsports_league_sn"]);
            int nCountry = CGlobal.ParseInt(param["nation_sn"]);
            int nSports = CGlobal.ParseInt(param["sport_sn"]);
            string strName = Convert.ToString(param["name"]);
            string strNameEn = Convert.ToString(param["name_en"]);
            string strLeagueImg = Convert.ToString(param["league_img"]);
            int nUse = CGlobal.ParseInt(param["is_use"]);

            CLeague clsLeague = CGlobal.GetLeagueInfoBySn(nSn);
            if (clsLeague == null)
            {
                CLeague clsLeagueInfo = new CLeague();
                clsLeagueInfo.m_nSn = nSn;
                clsLeagueInfo.m_nCode = nLSportsLeagueSn;
                clsLeagueInfo.m_nCountry = nCountry;
                clsLeagueInfo.m_nSports = nSports;
                clsLeagueInfo.m_strKo = strName;
                clsLeagueInfo.m_strEn = strNameEn;
                clsLeagueInfo.m_strImg = strLeagueImg;
                clsLeagueInfo.m_nUse = nUse;
                CGlobal.AddLeagueInfo(clsLeagueInfo);
            }
            else
            {
                /*List<CGame> lstGame = CGlobal.GetGameList();
                lstGame.FindAll(value => value.m_nLeague == clsLeague.m_nCode);
                if (lstGame != null)
                {
                    foreach (CGame clsGame in lstGame)
                    {
                        clsGame.m_nLeague = nLSportsLeagueSn;
                    }
                }*/

                clsLeague.m_nSn = nSn;
                clsLeague.m_nCode = nLSportsLeagueSn;
                clsLeague.m_nCountry = nCountry;
                clsLeague.m_nSports = nSports;
                clsLeague.m_strKo = strName;
                clsLeague.m_strEn = strNameEn;
                clsLeague.m_strImg = strLeagueImg;
                clsLeague.m_nUse = nUse;
            }
        }

        private void DeleteLeague(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nSn = CGlobal.ParseInt(param["sn"]);
            CLeague clsLeague = CGlobal.GetLeagueInfoBySn(nSn);
            if (clsLeague != null)
            {
                CGlobal.RemoveLeague(clsLeague);
            }
        }

        private void DeleteLeagues(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            string strSn = Convert.ToString(param["sn"]);
            string[] lstSn = strSn.Split(",");
            foreach(string sn in lstSn)
            {
                CLeague clsLeague = CGlobal.GetLeagueInfoBySn(CGlobal.ParseInt(sn));
                if (clsLeague != null)
                {
                    CGlobal.RemoveLeague(clsLeague);
                }
            }
        }
    }
}
