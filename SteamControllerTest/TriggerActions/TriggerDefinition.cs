using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.TriggerActions
{
    public class TriggerDefinition
    {
        public struct TriggerAxisData
        {
            public short max;
            public short min;
        }

        public TriggerAxisData trigAxis;
        public TriggerActionCodes trigCode;

        public TriggerDefinition(TriggerAxisData axisData, TriggerActionCodes trigCode)
        {
            this.trigAxis = axisData;
            this.trigCode = trigCode;
        }
    }
}
