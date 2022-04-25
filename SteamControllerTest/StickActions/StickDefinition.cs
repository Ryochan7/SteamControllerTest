using SteamControllerTest.MapperUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.StickActions
{
    public class StickDefinition
    {
        public struct StickAxisData
        {
            // Used limits by mapper. Might differ from real limits of hardware
            public short max;
            public short mid;
            public short min;

            // Real hardware limits
            public int hard_max;
            public int hard_min;
        };

        /*public int axisMin;
        public int axisMax;
        public int axisMid;
        */
        public StickAxisData xAxis;
        public StickAxisData yAxis;
        public StickActionCodes stickCode;

        /*public StickDefinition(int axisMin, int axisMax, int axisMid,
            StickActionCodes stickCode)
        {
            this.axisMin = axisMin;
            this.axisMax = axisMax;
            this.axisMid = axisMid;
            this.stickCode = stickCode;
        }
        */
        public StickDefinition(StickAxisData xAxis, StickAxisData yAxis,
            StickActionCodes stickCode)
        {
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            this.stickCode = stickCode;
        }

        public StickDefinition(StickDefinition other)
        {
            this.xAxis = other.xAxis;
            this.yAxis = other.yAxis;
            this.stickCode = other.stickCode;
        }
    }
}
