using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CPacket
    {
        public int m_nPacketCode;
        public int m_nRetCode;
        public string m_strPacket;

        public CPacket()
        {

        }

        public CPacket(int nCode)
        {
            m_nPacketCode = nCode;
        }
    }

    public class CLSportsPacket
    {
        public int m_nGame;
        public long m_nFixtureID;       // MGame.m_nFixtureID
        public int m_nSports;
        public string m_strSportName;   // MSports.m_strKo
        public int m_nLeague;           // MLeague.m_nCode
        public string m_strLeagueName;  // MLeague.m_strKo
        public string m_strLeagueImg;   // MLeague.m_strImg
        public string m_strHomeTeam;    // MTeam.m_strKo
        public string m_strAwayTeam;    // MTeam.m_strKo
        public string m_strDate;        // MGame.m_strDate
        public string m_strHour;        // MGame.m_strHour
        public string m_strMin;         // MGame.m_strMin
        public int m_nStatus;           // MGame.m_nStatus
        public string m_strPeriod;      // MPeriod.m_strKo
        public int m_nHomeScore;        // MGame.m_nHomeScore
        public int m_nAwayScore;        // MGame.m_nAwayScore
        public int m_nGroup;

        public List<CLSportsDPacket> m_lstDetail;
        public List<CLSportsSportsCnt> m_lstSportsCnt;

        public int m_nTotalCnt;
    }

    public class CLSportsSportsCnt
    {
        public int m_nSports;
        public string m_strName;
        public int m_nCount;

        public List<CLSportsContryCnt> m_lstCountryCnt;
    }

    public class CLSportsContryCnt
    {
        public int m_nCountry;
        public string m_strName;
        public string m_strImg;
        public int m_nCount;
        public int m_nPriorityFoot;
        public int m_nPriorityBasket;
        public int m_nPriorityBase;
        public int m_nPriorityVolley;
        public int m_nPriorityHocky;
        public int m_nPriorityEsports;

        public List<CLSportsLeagueCnt> m_lstLeagueCnt;
    }

    public class CLSportsLeagueCnt
    {
        public int m_nLeague;
        public string m_strName;
        public string m_strImg;
        public int m_nCount;
    }

    public class CLSportsDPacket
    {
        public int m_nMarket;           // MBetRate.m_nMarket
        public string m_strMarket;      // MMarket.m_strKo
        public string m_nHBetCode;        // MBetRate.m_nHBetCode
        public string m_nDBetCode;        // MBetRate.m_nDBetCode
        public string m_nABetCode;        // MBetRate.m_nABetCode
        public float m_fHRate;         // MBetRate.m_fHRate
        public float m_fDRate;         // MBetRate.m_fDRate
        public float m_fARate;         // MBetRate.m_fARate
        public float m_fHBase;         // MBetRate.m_fHBase
        public float m_fDBase;         // MBetRate.m_fDBase
        public float m_fABase;         // MBetRate.m_fABase
        public string m_strHLine;       // MBetRate.m_strHLine
        public string m_strDLine;       // MBetRate.m_strDLine
        public string m_strALine;       // MBetRate.m_strALine
        public string m_strBLine;       // MBetRate.m_strBLine
        public string m_strHName;       // MBetRate.m_strHName
        public string m_strDName;       // MBetRate.m_strDName
        public string m_strAName;       // MBetRate.m_strAName
        public int m_nStatus;           // MBetRate.m_nStatus
        public int m_nFamily;
    }

    public class CLSportsReqList
    {
        public string m_strSports;
        public int m_nLeague;
        public int m_nPageIndex;
        public int m_nPageSize;
        public int m_nLive;
    }
}
