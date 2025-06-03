
using System;
using System.Collections.Generic;

namespace Rock.ViewModels.Blocks.Communication.CommunicationFlowPerformance
{
    public class CommunicationFlowPerformanceFlowInstanceBag
    {
        public DateTime StartDate { get; set; }

        public List<CommunicationFlowPerformanceConversionHistoryBag> ConversionHistories { get; set; }
    }
}
