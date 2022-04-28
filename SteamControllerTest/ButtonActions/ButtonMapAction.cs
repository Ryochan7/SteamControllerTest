using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamControllerTest.ActionUtil;
using SteamControllerTest.MapperUtil;

namespace SteamControllerTest.ButtonActions
{
    public abstract class ButtonMapAction : MapAction
    {
        public bool activeEvent;
        public bool active;
        public bool analog;
        public bool useNotches;
        //public bool preprocess;
        public bool processAction;

        public abstract double ButtonDistance { get; }

        public abstract double AxisUnit { get; }

        public abstract void Prepare(Mapper mapper, bool status, bool alterState = true);
        public abstract void PrepareAnalog(Mapper mapper, double axisValue, double axisUnit,
            bool alterState = true);

        public abstract ButtonMapAction DuplicateAction();
        public virtual void SoftCopyFromParent(ButtonMapAction parentAction)
        {
        }

        public virtual void ProcessAction(Mapper mapper, bool outputActive,
            OutputActionData action)
        {
        }
    }
}
