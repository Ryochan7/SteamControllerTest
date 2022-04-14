using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.MapperUtil
{
    public class OutputActionDataBindSettings
    {
        private const int MOUSE_X_SPEED = 50;
        private const int MOUSE_Y_SPEED = 50;

        public int wheelXTicks = 0;
        public int wheelYTicks = 0;

        public int mouseXSpeed = MOUSE_X_SPEED;
        public int mouseYSpeed = MOUSE_Y_SPEED;
    }
}
