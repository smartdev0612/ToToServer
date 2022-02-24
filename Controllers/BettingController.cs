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
    public class BettingController : ControllerBase
    {
        [HttpGet]
        public string Get(int nCmd, string strValue)   //public IEnumerable<string> Get()
        {
            try
            {
                switch (nCmd)
                {
                    case 0x01:      //베팅정보 삭제 By betid
                        RemoveBettingInfo(strValue);
                        break;
                    case 0x02:      //배팅정보 삭제 By Betting no
                        RemoveBettingInfoByBettingNo(strValue);
                        break;
                    case 0x03:      //배팅방향 수정
                        ChangeBettingSelectNo(strValue);
                        break;
                }
            }
            catch (Exception err)
            {
                CGlobal.ShowConsole(err.Message);
            }

            return "success";
        }

        public void RemoveBettingInfo(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nSn = CGlobal.ParseInt(param["sn"]);
            CBetting clsBetting = CGlobal.GetSportsApiBettingBySn(nSn);
            if (clsBetting != null)
            {
                CGlobal.RemoveSportsApiBetting(clsBetting);
            }
        }

        public void RemoveBettingInfoByBettingNo(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            string betting_no = Convert.ToString(param["betting_no"]);
            List<CBetting > lstBetting = CGlobal.GetSportsApiBettingByBettingNo(betting_no);
            if (lstBetting.Count > 0)
            {
                foreach(CBetting clsBetting in lstBetting)
                {
                    CGlobal.RemoveSportsApiBetting(clsBetting);
                }
            }
        }

        public void ChangeBettingSelectNo(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nSn = CGlobal.ParseInt(param["sn"]);
            int nSelectNo = CGlobal.ParseInt(param["select_no"]);
            CBetting clsBetting = CGlobal.GetSportsApiBettingBySn(nSn);
            if (clsBetting != null)
            {
                clsBetting.m_nSelectNo = nSelectNo;
            }
        }
    }
}
