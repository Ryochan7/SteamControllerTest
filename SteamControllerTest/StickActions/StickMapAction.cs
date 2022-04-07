using SteamControllerTest.ActionUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.StickActions
{
    public abstract class StickMapAction : MapAction
    {
        public bool activeEvent;
        public bool active;
        protected StickDefinition stickDefinition;
        public StickDefinition StickDefinition { get => stickDefinition; set => stickDefinition = value; }

        public abstract void Prepare(Mapper mapper, int axisXVal, int axisYVal, bool alterState = true);

        public abstract StickMapAction DuplicateAction();
        public virtual void SoftCopyFromParent(StickMapAction parentAction)
        {
        }
    }
}
