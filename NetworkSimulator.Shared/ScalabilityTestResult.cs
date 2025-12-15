namespace NetworkSimulator.Shared
{
    public class ScalabilityTestResult
    {
        /// <summary>
        /// L'algoritmo usato per questo test (es. "Dijkstra" o "AI").
        /// </summary>
        public string Algorithm { get; set; } = string.Empty;
        /// <summary>
        /// Il numero di sensori attivi in questa iterazione. (Asse X del grafico)
        /// </summary>
        public int SensorCount { get; set; }

        /// <summary>
        /// Il data rate usato per i sensori. Serve per distinguere le diverse "curve" sul grafico.
        /// </summary>
        public double DataRate { get; set; }

        /// <summary>
        /// Risultato: Latenza media registrata. (Asse Y del grafico 1)
        /// </summary>
        public double AverageLatency { get; set; }

        /// <summary>
        /// Risultato: Tasso di consegna registrato. (Asse Y del grafico 2)
        /// </summary>
        public double DeliveryRate { get; set; }
    }
}
