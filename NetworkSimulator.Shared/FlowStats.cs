namespace NetworkSimulator.Shared
{
    public class FlowStats
    {
        // Dati base del flusso
        public string SourceNodeId { get; set; }
        public string DestinationNodeId { get; set; }

        // Dati statistici
        public int PacketsGenerated { get; set; }
        public int PacketsDelivered { get; set; }
        public double TotalLatencySum { get; set; } // Lo usiamo per calcolare la media alla fine
        public double TotalDataDelivered { get; set; }

        // Risultato finale
        public List<PathSegment> FinalPath { get; set; } = new List<PathSegment>();

        // Proprietà calcolate per comodità di visualizzazione
        public double AverageLatency => (PacketsDelivered > 0) ? TotalLatencySum / PacketsDelivered : 0;
        public double DeliveryRate => (PacketsGenerated > 0) ? ((double)PacketsDelivered / PacketsGenerated) * 100 : 0;
    }
}
