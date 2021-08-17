using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CBetInfo
    {
        private string m_strFixtureID;
        public string m_strBetID;
        public string m_strName;
        public double m_fStartPrice;
        public double m_fPrice;
        public int m_nSettlement;
        public string m_strLine;
        public string m_strBaseLine;
        public int m_nStatus;

        public CBetInfo(long nFixtureID)
        {
            m_strFixtureID = nFixtureID.ToString();
            m_strLine = string.Empty;
            m_strBaseLine = string.Empty;
        }

        public void UpdateInfo(JObject obj)
        {
            string strBetID = Convert.ToString(obj["Id"]);
            strBetID = strBetID.Substring(0, strBetID.Length - m_strFixtureID.Length);
            m_strBetID = $"{strBetID}{m_strFixtureID}";

            m_strBetID = Convert.ToString(obj["Id"]);

            foreach (JProperty prop in obj.Properties())
            {
                if (prop.Name == "Name")
                {
                    m_strName = Convert.ToString(prop.Value);
                }
                else if (prop.Name == "StartPrice")
                {
                    m_fStartPrice = Convert.ToDouble(prop.Value);
                }
                else if (prop.Name == "Price")
                {
                    m_fPrice = Convert.ToDouble(prop.Value);
                }
                else if (prop.Name == "Status")
                {
                    m_nStatus = CGlobal.ParseInt(prop.Value);
                    /*int nStatus = CGlobal.ParseInt(prop.Value);
                    if (nStatus == 9)
                        nStatus = 1;
                    if (nStatus > m_nStatus)
                        m_nStatus = nStatus;*/
                }
                else if (prop.Name == "Settlement")
                {
                    m_nSettlement = CGlobal.ParseInt(prop.Value);
                }
                else if (prop.Name == "Line")
                {
                    m_strLine = Convert.ToString(prop.Value);
                }
                else if (prop.Name == "BaseLine")
                {
                    m_strBaseLine = Convert.ToString(prop.Value);
                }
            }
        }
    }
}
