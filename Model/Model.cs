using System.Data;

namespace LSportsServer
{
    public class MBase
    {
        public int m_nCode;
    }

    public class MSports : MBase
    {
        public string m_strEn;
        public string m_strKo;
        public string m_strImg;
        public int m_nUse;
    }

    public class MCountry : MBase
    {
        public string m_strEn;
        public string m_strKo;
        public string m_strImg;
        public int m_nUse;
        public int m_nPriorityFoot;
        public int m_nPriorityBasket;
        public int m_nPriorityBase;
        public int m_nPriorityVolley;
        public int m_nPriorityHocky;
        public int m_nPriorityEsports;
    }

    public class MLeague : MBase
    {
        public int m_nSn;
        public string m_strEn;
        public string m_strKo;
        public string m_strImg;
        public int m_nUse;
        public int m_nSports;
        public int m_nCountry;
    }

    public class MTeam : MBase
    {
        public int m_nSn;
        public string m_strEn;
        public string m_strKo;
        public string m_strImg;
        public int m_nSports;
        public int m_nCountry;
        public int m_nLeague;
    }

    public class MMarket : MBase    
    {
        public string m_strEn;
        public string m_strKo;
        public int m_nFamily;
        public int m_nUse;
        public int m_nPeriod;
        public double m_fRate;
    }

    public class MPeriod : MBase
    {
        public int m_nPeriod;
        public int m_nSports;
        public string m_strEn;
        public string m_strKo;
    }

    public class MScore : MBase
    {
        public long m_nFixtureID;
        public int m_nPeriod;
        public int m_nHomeScore;
        public int m_nAwayScore;
        public int m_nIsFinished;
        public int m_nIsConfirmed;
    }

    public class MGame : MBase
    {
        public long m_nFixtureID;
        public int m_nSports;
        public int m_nCountry;
        public int m_nLeague;
        public int m_nHomeTeam;
        public int m_nAwayTeam;
        public string m_strDate;
        public string m_strHour;
        public string m_strMin;
        public int m_nStatus;
        public int m_nPeriod;
        public int m_nHomeScore;
        public int m_nAwayScore;
        public string m_strWinTeam;
        public int m_nSpecial;
        public int m_nSpecified;
        public int m_nType;
        public int m_nLive;
        public int m_nBlock;
    }

    public class MBetRate : MBase
    {
        //public int m_nGame;
        public int m_nMarket;
        public string m_strHBetCode;
        public string m_strDBetCode;
        public string m_strABetCode;
        public double m_fHRate;
        public double m_fDRate;
        public double m_fARate;
        public double m_fHBase;
        public double m_fDBase;
        public double m_fABase;
        public string m_strHLine;
        public string m_strDLine;
        public string m_strALine;
        public string m_strBLine;
        public string m_strHName;
        public string m_strDName;
        public string m_strAName;
        public int m_nStatus;
        public string m_strApi;
        public int m_nLive;
        public int m_nWin;
        public int m_nResult;
        public int m_nViewFlag;
        //public double m_nTimeTick;
    }

    public class MBetting : MBase
    {
        public int m_nSubChildSn;
        public int m_nMemberSn;
        public string m_strBettingNo;
        public int m_nSelectNo;
        public double m_fHomeRate;
        public double m_fAwayRate;
        public double m_fDrawRate;
        public double m_fSelectRate;
        public string m_strBetID;
        public int m_nGameType;
        public int m_nResult;
        public int m_nBetMoney;
        public int m_nStype;
        public int m_nPass;
        public string m_strScore;
        public int m_nLive;
    }

    public class CDataPacket
    {
        public string Fixture;
        public string Markets;
    }

    interface ILSports
    {
        public MBase GetModel();
        public void LoadInfo(DataRow info);
    }
}
