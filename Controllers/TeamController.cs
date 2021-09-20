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
    public class TeamController : ControllerBase
    {
        [HttpGet]
        public string Get(int nCmd, string strValue)
        {
            try
            {
                switch (nCmd)
                {
                    case 0x01:      //팀정보수정
                        UpdateTeam(strValue);
                        break;
                    case 0x02:      //팀삭제
                        DeleteTeam(strValue);
                        break;
                    case 0x03:      //선택된 팀들 삭제
                        DeleteTeams(strValue);
                        break;
                }

            }
            catch (Exception err)
            {
                CGlobal.ShowConsole(err.Message);
            }

            return "success";
        }

        private void UpdateTeam(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nSn = CGlobal.ParseInt(param["sn"]);
            int nCode = CGlobal.ParseInt(param["api_sn"]);
            string strName = Convert.ToString(param["name"]);
            string strNameEn = Convert.ToString(param["name_en"]);
            string strImg = Convert.ToString(param["team_img"]);
            int nSportSn = CGlobal.ParseInt(param["sport_sn"]);
            int nNationSn = CGlobal.ParseInt(param["nation_sn"]);

            CTeam clsTeam= CGlobal.GetTeamInfoBySn(nSn);
            if (clsTeam == null)
            {
                CTeam clsTeamInfo = new CTeam();
                clsTeamInfo.m_nSn = nSn;
                clsTeamInfo.m_nCode = nCode;
                clsTeamInfo.m_strKo = strName;
                clsTeamInfo.m_strEn = strNameEn;
                clsTeamInfo.m_strImg = strImg;
                clsTeamInfo.m_nSports = nSportSn;
                clsTeamInfo.m_nCountry = nNationSn;
                CGlobal.AddTeamInfo(clsTeamInfo);
            }
            else
            {
                clsTeam.m_nSn = nSn;
                clsTeam.m_nCode = nCode;
                clsTeam.m_strKo = strName;
                clsTeam.m_strEn = strNameEn;
                clsTeam.m_strImg = strImg;
                clsTeam.m_nSports = nSportSn;
                clsTeam.m_nCountry = nNationSn;
            }
        }

        private void DeleteTeam(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nSn = CGlobal.ParseInt(param["sn"]);
            CTeam clsTeam = CGlobal.GetTeamInfoBySn(nSn);
            if (clsTeam != null)
            {
                CGlobal.RemoveTeam(clsTeam);
            }
        }

        private void DeleteTeams(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            string strSn = Convert.ToString(param["sn"]);
            string[] lstSn = strSn.Split(",");
            foreach (string sn in lstSn)
            {
                int nSn = CGlobal.ParseInt(sn);
                CTeam clsTeam = CGlobal.GetTeamInfoBySn(nSn);
                if (clsTeam != null)
                {
                    CGlobal.RemoveTeam(clsTeam);
                }
            }
        }
    }
}
