using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public static class CEntry
    {
        public static DataRowCollection SelectSports()
        {
            string sql = "SELECT * FROM tb_sports";
            DataRowCollection list = CMySql.GetDataQuery(sql);

            return list;
        }

        public static DataRowCollection SelectCountry()
        {
            string sql = "SELECT * FROM tb_nation";
            DataRowCollection list = CMySql.GetDataQuery(sql);

            return list;
        }

        public static DataRowCollection SelectLeague()
        {
            string sql = "SELECT * FROM  tb_league WHERE sport_sn IS NOT NULL";
            DataRowCollection list = CMySql.GetDataQuery(sql);

            return list;
        }

        public static DataRowCollection SelectTeam()
        {
            string sql = "SELECT * FROM tb_team";
            DataRowCollection list = CMySql.GetDataQuery(sql);

            return list;
        }

        public static DataRowCollection SelectMarket()
        {
            string sql = "SELECT * FROM tb_markets";
            DataRowCollection list = CMySql.GetDataQuery(sql);

            return list;
        }

        public static DataRowCollection SelectPeriod()
        {
            string sql = "SELECT * FROM tb_periods";
            DataRowCollection list = CMySql.GetDataQuery(sql);

            return list;
        }

        public static DataRowCollection SelectGame()
        {
            string sql = "SELECT * FROM tb_child WHERE status < 3 AND sport_id > 0 AND sport_id IS NOT NULL";
            DataRowCollection list = CMySql.GetDataQuery(sql);

            return list;
        }
        public static DataRowCollection SelectBetRate(int nGame)
        {
            string sql = $"SELECT * FROM tb_subchild WHERE child_sn = {nGame}";
            DataRowCollection list = CMySql.GetDataQuery(sql);

            return list;
        }

        public static int InsertGameToDB(CGame model)
        {
            int nKubun = model.IsFinishGame() ? 1 : 0;

            CSports clsSports = CGlobal.GetSportsInfoByCode(model.m_nSports);
            CLeague clsLeague = CGlobal.GetLeagueInfoByCode(model.m_nLeague);
            CTeam clsHomeTeam = CGlobal.GetTeamInfoByCode(model.m_nHomeTeam);
            CTeam clsAwayTeam = CGlobal.GetTeamInfoByCode(model.m_nAwayTeam);

            string sql = $"SELECT sn FROM tb_child WHERE game_sn = {model.m_nFixtureID}";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            if(list.Count > 0)
            {
                model.m_nCode = CGlobal.ParseInt(list[0]["sn"]);
                SaveGameInfoToDB(model);
            }
            else
            {
                sql = $"INSERT INTO tb_child(game_sn, sport_id, sport_name_en, sport_name, league_sn, notice_en, notice, home_team_id, home_team_en, home_team, away_team_id, away_team_en, away_team, gameDate, gameHour, gameTime, status, kubun, strTime, special, league_img, home_score, away_score, win_team, game_period, is_specified_special, tb_child.type) VALUES({model.m_nFixtureID}, {model.m_nSports}, '{clsSports.m_strEn}', '{clsSports.m_strKo}', {model.m_nLeague}, '{clsLeague.m_strEn}', '{clsLeague.m_strKo}', {model.m_nHomeTeam}, '{clsHomeTeam.m_strEn}', '{clsHomeTeam.m_strKo}', {model.m_nAwayTeam}, '{clsAwayTeam.m_strEn}', '{clsAwayTeam.m_strKo}', '{model.m_strDate}', '{model.m_strHour}', '{model.m_strMin}', {model.m_nStatus}, {nKubun}, '{CMyTime.GetMyTimeStr()}', {model.m_nSpecial}, '{clsLeague.m_strImg}', {model.m_nHomeScore}, {model.m_nAwayScore}, '{model.m_strWinTeam}', {model.m_nPeriod}, {model.m_nSpecified}, {model.m_nType})";

                CMySql.ExcuteQuery(sql);

                sql = $"SELECT sn FROM tb_child WHERE game_sn = '{model.m_nFixtureID}'";
                list = CMySql.GetDataQuery(sql);
            }

            int nChildSn = CGlobal.ParseInt(list[0]["sn"]);

            return nChildSn;
        }

        public static void SaveGameInfoToDB(CGame model)
        {
            int nKubun = model.IsFinishGame() ? 1 : 0;
            CSports clsSports = CGlobal.GetSportsInfoByCode(model.m_nSports);
            CLeague clsLeague = CGlobal.GetLeagueInfoByCode(model.m_nLeague);
            CTeam clsHomeTeam = CGlobal.GetTeamInfoByCode(model.m_nHomeTeam);
            CTeam clsAwayTeam = CGlobal.GetTeamInfoByCode(model.m_nAwayTeam);

            string sql = $"UPDATE tb_child SET sport_id = {model.m_nSports}, sport_name_en = '{clsSports.m_strEn}', sport_name = '{clsSports.m_strKo}', league_sn = {model.m_nLeague}, notice_en = '{clsLeague.m_strEn}', notice = '{clsLeague.m_strKo}', home_team_id = {model.m_nHomeTeam}, home_team_en = '{clsHomeTeam.m_strEn}', home_team = '{clsHomeTeam.m_strKo}', away_team_id = {model.m_nAwayTeam}, away_team_en = '{clsAwayTeam.m_strEn}', away_team = '{clsAwayTeam.m_strKo}', gameDate = '{model.m_strDate}', gameHour = '{model.m_strHour}', gameTime = '{model.m_strMin}', status = {model.m_nStatus}, kubun = {nKubun}, strTime = '{CMyTime.GetMyTimeStr()}',  special = {model.m_nSpecial}, league_img = '{clsLeague.m_strImg}', home_score = {model.m_nHomeScore}, away_score = {model.m_nAwayScore}, win_team = '{model.m_strWinTeam}', game_period = {model.m_nPeriod}, is_specified_special = {model.m_nSpecified}, tb_child.type = {model.m_nType}, live = {model.m_nLive} WHERE sn = '{model.m_nCode}'";

            CMySql.ExcuteQuery(sql);

            sql = $"DELETE FROM tb_score WHERE game_sn = {model.m_nFixtureID}";
            CMySql.ExcuteQuery(sql);
        }

        public static int InsertBetRateInfoToDB(MBetRate model)
        {
            string sql = $"SELECT sn FROM tb_subchild WHERE (home_betid > 0 AND home_betid = '{model.m_strHBetCode}') OR (draw_betid = '{model.m_strDBetCode}' AND draw_betid > 0) OR (away_betid > 0 AND away_betid = '{model.m_strABetCode}')";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            if(list.Count > 0)
            {
                model.m_nCode = CGlobal.ParseInt(list[0]["sn"]);
                SaveBetRateInfoToDB(model);
            }
            else
            {
                sql = $"INSERT INTO tb_subchild(child_sn, betting_type, home_rate, draw_rate, away_rate, win, result, new_home_rate, new_draw_rate, new_away_rate, home_betid, draw_betid, away_betid, home_line, draw_line, away_line, home_name, draw_name, away_name, status, base_line, strTime, apiName, live) VALUES({model.m_nGame}, {model.m_nMarket}, {model.m_fHBase}, {model.m_fDBase}, {model.m_fABase}, {model.m_nWin}, {model.m_nResult}, {model.m_fHRate}, {model.m_fDRate}, {model.m_fARate}, '{model.m_strHBetCode}', '{model.m_strDBetCode}', '{model.m_strABetCode}', '{model.m_strHLine}', '{model.m_strDLine}', '{model.m_strALine}', '{model.m_strHName}', '{model.m_strDName}', '{model.m_strAName}', {model.m_nStatus}, '{model.m_strBLine}', '{CMyTime.GetMyTimeStr()}', '{model.m_strApi}', {model.m_nLive})";

                CMySql.ExcuteQuery(sql);

                sql = "SELECT sn FROM tb_subchild ORDER BY sn DESC LIMIT 1";
                list = CMySql.GetDataQuery(sql);
            }

            int nCode = CGlobal.ParseInt(list[0]["sn"]);

            return nCode;
        }

        public static void SaveBetRateInfoToDB(MBetRate model)
        {
            string sql = $"UPDATE tb_subchild SET home_rate = {model.m_fHBase}, draw_rate = {model.m_fDBase}, away_rate = {model.m_fABase}, win = {model.m_nWin}, result = {model.m_nResult}, new_home_rate = {model.m_fHRate}, new_draw_rate = {model.m_fDRate}, new_away_rate = {model.m_fARate}, home_betid = '{model.m_strHBetCode}', draw_betid = '{model.m_strDBetCode}', away_betid = '{model.m_strABetCode}', home_line = '{model.m_strHLine}', draw_line = '{model.m_strDLine}', away_line = '{model.m_strALine}', home_name = '{model.m_strHName}', draw_name = '{model.m_strDName}', away_name = '{model.m_strAName}', status = {model.m_nStatus}, base_line = '{model.m_strBLine}', apiName = '{model.m_strApi}', live = {model.m_nLive}, strTime = '{CMyTime.GetMyTimeStr()}' WHERE sn = {model.m_nCode}";

            CMySql.ExcuteQuery(sql);
        }

        public static void SaveScoreInfoToDB(MScore model)
        {
            string sql = $"INSERT INTO tb_score(game_sn, period, home_score, away_score, isFinished, isConfirmed, strTime) VALUES({model.m_nFixtureID}, {model.m_nPeriod}, {model.m_nHomeScore}, {model.m_nAwayScore}, {model.m_nIsFinished}, {model.m_nIsConfirmed}, '{CMyTime.GetMyTimeStr()}')";
            CMySql.ExcuteQuery(sql);
        }

        public static void SetGameSchedule(string strWhere)
        {
            string sql = $"UPDATE tb_child SET live = 1 WHERE live != -1 AND {strWhere}";
            CMySql.ExcuteQuery(sql);
        }

        public static void SaveScoreToDB(MGame model)
        {
            string sql = $"UPDATE tb_child SET home_score = {model.m_nHomeScore}, away_score = {model.m_nAwayScore}, game_period = {model.m_nPeriod} WHERE sn = {model.m_nCode}";
            CMySql.ExcuteQuery(sql);
        }

        public static void SaveLeagueToDB(long nLeagueId, string strName, long nLocationId, long nSportId)
        {
            string sql = $"SELECT sn FROM tb_league WHERE lsports_league_sn = '{nLeagueId}'";
            DataRowCollection list = CMySql.GetDataQuery(sql);
            if (list.Count == 0)
            {
                sql = $"SELECT name FROM tb_sports WHERE sn = '{nSportId}'";
                DataRowCollection sports = CMySql.GetDataQuery(sql);
                string kind = "";
                if (sports.Count > 0)
                    kind = Convert.ToString(sports[0]["name"]);

                string strLeagueImg = $"/upload/league/{nLocationId}.png";
                strName = strName.Replace("'", " ");

                sql = $"INSERT INTO tb_league(sn, lsports_league_sn, nation_sn, sport_sn, kind, name, name_en, alias_name, lg_img) VALUES ({nLeagueId}, {nLeagueId}, {nLocationId}, {nSportId}, '{kind}', '{strName}', '{strName}', '{strName}', '{strLeagueImg}')";

                CMySql.ExcuteQuery(sql);
            }
        }
    }
}
