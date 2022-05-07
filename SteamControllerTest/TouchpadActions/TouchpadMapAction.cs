using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.TouchpadActions
{
    public enum TouchesMode : ushort
    {
        OneTouch,
        TwoTouch,
    }

    public enum OuterRingUseRange
    {
        OnlyActive,
        FullRange,
    }

    public struct TouchEventFrame
    {
        public short X;
        public short X2;
        public short Y;
        public short Y2;
        public bool Touch;
        public bool Click;
        public uint numTouches;
        //public TouchesMode supportedTouches;
        public double timeElapsed;
    }

    public abstract class TouchpadMapAction : MapAction
    {
        public bool activeEvent;
        public bool active;

        protected TouchpadDefinition touchpadDefinition;
        public TouchpadDefinition TouchDefinition
        {
            get => touchpadDefinition; set => touchpadDefinition = value;
        }

        public abstract void Prepare(Mapper mapper, ref TouchEventFrame touchFrame, bool alterState = true);
        public virtual void SoftCopyFromParent(TouchpadMapAction parentAction)
        {
            name = parentAction.name;
            mappingId = parentAction.mappingId;
            this.parentAction = parentAction;
            this.touchpadDefinition = new TouchpadDefinition(parentAction.touchpadDefinition);
        }

        public virtual void PrepareActions()
        {
        }
    }
}
