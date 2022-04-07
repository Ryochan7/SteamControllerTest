using SteamControllerTest.ActionUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.GyroActions
{
    public struct GyroEventFrame
    {
        public short GyroYaw;
        public short GyroPitch;
        public short GyroRoll;
        public double AngGyroYaw;
        public double AngGyroPitch;
        public double AngGyroRoll;
        public short AccelX;
        public short AccelY;
        public short AccelZ;
        public double AccelXG, AccelYG, AccelZG;
        public double timeElapsed;

        public double elapsedReference;
    }

    public abstract class GyroMapAction : MapAction
    {
        public bool active;
        public bool activeEvent;

        public abstract void Prepare(Mapper mapper, ref GyroEventFrame gyroFrame, bool alterState = true);

        public abstract void BlankEvent(Mapper mapper);

        public abstract GyroMapAction DuplicateAction();

        public virtual void SoftCopyFromParent(GyroMapAction parentAction)
        {
        }
    }
}
