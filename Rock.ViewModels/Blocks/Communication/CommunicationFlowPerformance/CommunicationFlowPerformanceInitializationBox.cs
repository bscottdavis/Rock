using System.Collections.Generic;

using Rock.ViewModels.Reporting;

namespace Rock.ViewModels.Blocks.Communication.CommunicationFlowPerformance
{
    public class CommunicationFlowPerformanceInitializationBox : BlockBox
    {
        public List<ChartNumericDataPointBag> TotalFlowPerformanceOverTimeDataPoints { get; set; }

        public List<ChartNumericDataPointBag> InstanceVsFlowPerformanceOverTimeDataPoints { get; set; }

        public CommunicationFlowPerformanceFlowBag Flow { get; set; }
    }
}
