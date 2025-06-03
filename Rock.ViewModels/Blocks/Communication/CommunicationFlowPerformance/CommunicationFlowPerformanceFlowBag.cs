
using System.Collections.Generic;

using Rock.Enums.Communication;

namespace Rock.ViewModels.Blocks.Communication.CommunicationFlowPerformance
{
    public class CommunicationFlowPerformanceFlowBag
    {
        public CommunicationFlowPerformanceConversionGoalSettingsBag ConversionGoalSettings { get; set; }

        public List<CommunicationFlowPerformanceFlowInstanceBag> Instances { get; set; }
        public ConversionGoalType? ConversionGoalType { get; internal set; }
    }
}
