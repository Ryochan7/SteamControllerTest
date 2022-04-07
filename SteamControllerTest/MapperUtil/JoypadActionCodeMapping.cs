using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.MapperUtil
{
    public class JoypadActionCodeMapping
    {
        public enum ControlType : uint
        {
            Unknown,
            Axis,
            Trigger,
            Button,
        }

        public ControlType type;
        public JoypadActionCodes code;
    }
}
