namespace NetworkSimulator.Shared
{
    /// <summary>
    /// Contiene le statistiche aggregate di una sessione di simulazione.
    /// </summary>
    public class SimulationStats
    {
        public int PacketsGenerated { get; set; }
        public int PacketsDelivered { get; set; }
        public double AverageLatency { get; set; } // in ms
        public double Throughput { get; set; } // in KB/s
    }
}
