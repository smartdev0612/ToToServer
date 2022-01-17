using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LSportsServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        // GET: api/<UserController>
        [HttpGet]
        public string Get(int nCmd, string strValue)   //public IEnumerable<string> Get()
        {
            try
            {
                switch (nCmd)
                {
                    case 0x01:      //마켓배당변경
                        ChangeRate(strValue);
                        break;
                    case 0x02:      //경기차단
                        BlockGame(strValue);
                        break;
                    case 0x03:      //경기차단해제
                        CancelBlockGame(strValue);
                        break;
                }
            }
            catch (Exception err)
            {
                CGlobal.ShowConsole(err.Message);
            }

            return "success";
        }

        private void ChangeRate(string strValue)
        {
            Console.WriteLine(strValue);
            List<AdminMarketPacket> list = JsonConvert.DeserializeObject<List<AdminMarketPacket>>(strValue);

            foreach(AdminMarketPacket packet in list)
            {
                CMarket clsMarket = CGlobal.GetMarketInfoByCode(packet.nMarket);
                if (clsMarket == null)
                    continue;


                double fRate = packet.fRate / clsMarket.m_fRate;
                clsMarket.m_fRate = packet.fRate;

                List<CGame> lstGame = CGlobal.GetGameList();
                lstGame = lstGame.FindAll(value => value != null && value.GetPrematchBetRateList().Exists(val => val.m_nMarket == packet.nMarket) || value.GetLiveBetRateList().Exists(val => val.m_nMarket == packet.nMarket));

                int nIndex = 0;
                while(nIndex < lstGame.Count)
                {
                    CGame clsGame = lstGame[nIndex];
                    if (clsGame == null)
                        break;
                    clsGame.GetPrematchBetRateList().FindAll(value => value.m_nMarket == packet.nMarket).ForEach(value => value.ChangeAdminRate(1, fRate, true));
                    clsGame.GetLiveBetRateList().FindAll(value => value.m_nMarket == packet.nMarket).ForEach(value => value.ChangeAdminRate(2, fRate, true));
                    nIndex++;
                }
            }
        }

        private void BlockGame(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nChildSn = CGlobal.ParseInt(param["nChildSn"]);

            CGame clsGame = CGlobal.GetGameInfoByCode(nChildSn);
            if (clsGame == null)
                return;

            clsGame.m_nBlock = 1;
        }

        private void CancelBlockGame(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nChildSn = CGlobal.ParseInt(param["nChildSn"]);

            CGame clsGame = CGlobal.GetGameInfoByCode(nChildSn);
            if (clsGame == null)
                return;

            clsGame.m_nBlock = 0;
        }
    }

    public class AdminMarketPacket
    {
        public int nMarket;
        public float fRate;
    }
}
