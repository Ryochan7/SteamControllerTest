using SteamControllerTest.ActionUtil;
using SteamControllerTest.MapperUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamControllerTest.ButtonActions
{
    public class AxisDirButtonNoAction : AxisDirButton
    {
        public AxisDirButtonNoAction()
        {
        }

        public AxisDirButtonNoAction(ActionFunc actionFunc) : base(actionFunc)
        {
        }

        public AxisDirButtonNoAction(OutputActionData outputAction) : base(outputAction)
        {
        }

        public AxisDirButtonNoAction(IEnumerable<OutputActionData> outputActions) : base(outputActions)
        {
        }

        public override void Event(Mapper mapper)
        {
        }
    }
}
