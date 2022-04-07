using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.TouchpadActions
{
    public class TouchpadDefinition
    {
        public struct TouchAxisData
        {
            public short max;
            public short mid;
            public short min;
        }

        public TouchAxisData xAxis;
        public TouchAxisData yAxis;
        public TouchpadActionCodes touchCode;

        public TouchpadDefinition(TouchAxisData xAxis, TouchAxisData yAxis, TouchpadActionCodes touchCode)
        {
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            this.touchCode = touchCode;
        }

        public TouchpadDefinition(TouchpadDefinition other)
        {
            this.xAxis = other.xAxis;
            this.yAxis = other.yAxis;
            this.touchCode = other.touchCode;
        }
    }
}
