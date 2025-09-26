namespace NetworkSimulator.Shared
{
    public class FinalSimulationResults
    {
        public SimulationStats AggregateStats { get; set; }
        public List<FlowStats> PerFlowStats { get; set; }
    }
}
