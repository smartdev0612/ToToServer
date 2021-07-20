using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LSportsServer
{
    public class CScore : MScore
    {
        public CScore(long nFixtureID)
        {
            m_nFixtureID = nFixtureID;
        }
    }
}
