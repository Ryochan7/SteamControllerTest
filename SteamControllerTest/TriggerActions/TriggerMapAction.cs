using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.TriggerActions
{
    public struct TriggerEventFrame
    {
        public short axisValue;
        public bool fullClick;
    }

    public abstract class TriggerMapAction : MapAction
    {
        public bool activeEvent;
        public bool active;

        protected TriggerDefinition triggerDefinition;
        public TriggerDefinition TriggerDef
        {
            get => triggerDefinition;
            set => triggerDefinition = value;
        }

        public abstract void Prepare(Mapper mapper, short axisValue, bool alterState = true);
        public virtual void SoftCopyFromParent(TriggerMapAction parentAction)
        {
            name = parentAction.name;
            mappingId = parentAction.mappingId;
            this.parentAction = parentAction;
            this.triggerDefinition = new TriggerDefinition(parentAction.triggerDefinition);
        }
    }
}
