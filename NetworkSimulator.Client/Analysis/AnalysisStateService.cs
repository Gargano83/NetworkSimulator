using NetworkSimulator.Shared;

namespace NetworkSimulator.Client.Analysis
{
    // Una classe per memorizzare un punto dati del grafico (Tempo, Tasso di Consegna)
    public class TimeSeriesDataPoint
    {
        public int Time { get; set; }
        public double DeliveryRate { get; set; }
    }

    /// <summary>
    /// Servizio Singleton per memorizzare i risultati delle simulazioni
    /// e condividerli con la pagina di Analisi.
    /// </summary>
    public class AnalysisStateService
    {
        // Memorizza i risultati finali per la tabella del Caso di Studio 1
        public Dictionary<string, SimulationStats> NormalConditionResults { get; set; } = new();

        // Memorizza i dati per il grafico del Caso di Studio 2
        public Dictionary<string, List<TimeSeriesDataPoint>> LinkFailureResults { get; set; } = new();

        public event Action? OnChange;

        public void AddNormalConditionResult(string algorithmName, SimulationStats stats)
        {
            NormalConditionResults[algorithmName] = stats;
            NotifyStateChanged();
        }

        public void AddLinkFailureDataPoint(string algorithmName, int time, SimulationStats stats)
        {
            if (!LinkFailureResults.ContainsKey(algorithmName))
            {
                LinkFailureResults[algorithmName] = new List<TimeSeriesDataPoint>();
            }

            double deliveryRate = (stats.PacketsGenerated > 0)
                ? ((double)stats.PacketsDelivered / stats.PacketsGenerated) * 100
                : 0;

            // Evita di aggiungere punti duplicati allo stesso tempo
            if (!LinkFailureResults[algorithmName].Any(p => p.Time == time))
            {
                LinkFailureResults[algorithmName].Add(new TimeSeriesDataPoint { Time = time, DeliveryRate = deliveryRate });
            }
            NotifyStateChanged();
        }

        public void Clear()
        {
            NormalConditionResults.Clear();
            LinkFailureResults.Clear();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
