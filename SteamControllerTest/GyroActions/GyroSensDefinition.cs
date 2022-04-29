using System;

namespace SteamControllerTest.GyroActions
{
    public class GyroSensDefinition
    {
        public double elapsedReference;

        public double mouseCoefficient;
        public double mouseOffset;

        public int accelMinLeanX;
        public int accelMaxLeanX;
        public int accelMinLeanY;
        public int accelMaxLeanY;
        public int accelMinLeanZ;
        public int accelMaxLeanZ;
    }
}
