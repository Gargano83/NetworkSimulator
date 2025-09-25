namespace NetworkSimulator.Shared
{
    public class LiveFlowStats
    {
        public string SourceNodeId { get; set; }
        public int PacketsGenerated { get; set; }
        public int PacketsDelivered { get; set; }
    }
}
