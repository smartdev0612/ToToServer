using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        [HttpGet]
        public string Get(int nCmd, string strValue)
        {
            try
            {
                switch(nCmd)
                {
                    case 0x01: // 경기 수동 업로드
                        AddGameInfo(strValue);
                        break;
                    case 0x02: // 배당수정
                        UpdateRate(strValue);
                        break;
                    case 0x03:  // 경기삭제
                        break;
                    case 0x04:  // 경기마감
                        finishGame(strValue);
                        break;
                    case 0x05:  // 마감
                        deadlineGame(strValue);
                        break;
                    case 0x06:  // 마켓 숨기기
                        hideMarket(strValue);
                        break;
                    case 0x07:  // 마켓 삭제
                        deleteMarket(strValue);
                        break;
                }

            } 
            catch(Exception err)
            {
                CGlobal.ShowConsole(err.Message);
            }

            return "success";
        }

        private void AddGameInfo(string strValue)
        {
            CGlobal.LoadRealtimeGameFromDB();
        }

        private void UpdateRate(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nSubchildSn = CGlobal.ParseInt(param["nSn"]);
            int nChildSn = CGlobal.ParseInt(param["nChildSn"]);
            string strGameDate = Convert.ToString(param["strGameDate"]);
            string strGameHour = Convert.ToString(param["strGameHour"]);
            string strGameTime = Convert.ToString(param["strGameTime"]);
            int nGameType = CGlobal.ParseInt(param["nGameType"]);
            int nFamilyID = CGlobal.ParseInt(param["nFamilyID"]);
            float fHomeRate = Convert.ToSingle(param["fHomeRate"]);
            float fAwayRate = 0.0f;
            float fDrawRate = Convert.ToSingle(param["fDrawRate"]);
            string strHomeLine = "";
            string strHomeName = "";
            switch(nFamilyID)
            {
                case 1:     // 승무패
                case 12:    // 더블찬스
                    fAwayRate = Convert.ToSingle(param["fAwayRate"]);
                    break;
                case 7:     // 언더오버
                case 8:     // 아시안핸디캡
                case 9:     // E스포츠 핸디캡
                    strHomeLine = Convert.ToString(param["fDrawRate"]);
                    break;
                case 11:    // 정확한 스코어
                    strHomeName = Convert.ToString(param["fDrawRate"]);
                    break;
                case 47:    // 승무패 + 언더오버
                    strHomeLine = Convert.ToString(param["fDrawRate"]);
                    break;
            }
            
        }

        private void finishGame(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int childSn = CGlobal.ParseInt(param["sn"]);
            CGame clsGame = CGlobal.GetGameInfoByCode(childSn);
            if (clsGame != null)
            {
                CGlobal.RemoveGame(clsGame);
            }
        }

        private void deadlineGame(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nChildSn = CGlobal.ParseInt(param["sn"]);
            string strGameDate = Convert.ToString(param["gameDate"]);
            string strGameHour = Convert.ToString(param["gameHour"]);
            string strGameTime = Convert.ToString(param["gameTime"]);
            CGame clsGame = CGlobal.GetGameInfoByCode(nChildSn);
            if (clsGame != null)
            {
                clsGame.m_strDate = strGameDate;
                clsGame.m_strHour = strGameHour;
                clsGame.m_strMin = strGameTime;
            }
        }

        private void hideMarket(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nChildSn = CGlobal.ParseInt(param["child_sn"]);
            int nSubChildSn = CGlobal.ParseInt(param["subchild_sn"]);
            CGame clsGame = CGlobal.GetGameInfoByCode(nChildSn);
            if (clsGame != null)
            {

            }
        }

        private void deleteMarket(string strValue)
        {
            JToken param = JObject.Parse(strValue);
            int nChildSn = CGlobal.ParseInt(param["child_sn"]);
            int nSubChildSn = CGlobal.ParseInt(param["subchild_sn"]);
            CGame clsGame = CGlobal.GetGameInfoByCode(nChildSn);
            if (clsGame != null)
            {

            }
        }
    }
}
