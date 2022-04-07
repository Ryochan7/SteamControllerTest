using SteamControllerTest.ActionUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.ButtonActions
{
    public class ButtonNoAction : ButtonMapAction
    {
        public override double ButtonDistance => throw new NotImplementedException();

        public ButtonNoAction()
        {
        }

        protected ButtonNoAction(ButtonNoAction parentAction)
        {
            if (parentAction != null)
            {
                this.parentAction = parentAction;
                parentAction.hasLayeredAction = true;
                mappingId = parentAction.mappingId;
            }
        }

        public override void Prepare(Mapper mapper, bool status, bool alterState = true)
        {
        }

        public override void Event(Mapper mapper)
        {
        }

        public override void Release(Mapper mapper, bool resetState = true)
        {
        }

        public override void PrepareAnalog(Mapper mapper, double axisValue, bool alterState = true)
        {
            throw new NotImplementedException();
        }

        public override ButtonMapAction DuplicateAction()
        {
            return new ButtonNoAction(this);
        }

        public override void SoftCopyFromParent(ButtonMapAction parentAction)
        {
            if (parentAction is ButtonNoAction tempNoAction)
            {
                base.SoftCopyFromParent(parentAction);

                this.parentAction = parentAction;
                tempNoAction.hasLayeredAction = true;
                mappingId = tempNoAction.mappingId;
            }
        }
    }
}
