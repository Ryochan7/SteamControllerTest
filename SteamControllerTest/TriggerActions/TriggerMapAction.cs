using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.TriggerActions
{
    public abstract class TriggerMapAction : MapAction
    {
        public bool activeEvent;
        public bool active;

        public abstract void Prepare(Mapper mapper, double axisValue, bool alterState = true);
        public virtual void SoftCopyFromParent(TriggerMapAction parentAction)
        {
        }
    }
}
