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
        public Dictionary<string, List<FlowStats>> NormalConditionFlowResults { get; set; } = new();

        // Memorizza i dati per il grafico del Caso di Studio 2
        public Dictionary<string, Dictionary<string, List<TimeSeriesDataPoint>>> LinkFailureFlowResults { get; set; } = new();

        public event Action? OnChange;

        public void AddNormalConditionFlowResults(string algorithmName, List<FlowStats> stats)
        {
            NormalConditionFlowResults[algorithmName] = stats;
            NotifyStateChanged();
        }

        public void AddLinkFailureFlowDataPoints(string algorithmName, int time, List<LiveFlowStats> liveStats)
        {
            if (!LinkFailureFlowResults.ContainsKey(algorithmName))
            {
                LinkFailureFlowResults[algorithmName] = new Dictionary<string, List<TimeSeriesDataPoint>>();
            }

            foreach (var flowStat in liveStats)
            {
                if (!LinkFailureFlowResults[algorithmName].ContainsKey(flowStat.SourceNodeId))
                {
                    LinkFailureFlowResults[algorithmName][flowStat.SourceNodeId] = new List<TimeSeriesDataPoint>();
                }

                double deliveryRate = (flowStat.PacketsGenerated > 0)
                    ? ((double)flowStat.PacketsDelivered / flowStat.PacketsGenerated) * 100
                    : 0;

                var dataPoints = LinkFailureFlowResults[algorithmName][flowStat.SourceNodeId];

                if (!dataPoints.Any(p => p.Time == time))
                {
                    dataPoints.Add(new TimeSeriesDataPoint { Time = time, DeliveryRate = deliveryRate });
                }
            }
            NotifyStateChanged();
        }

        public void Clear()
        {
            NormalConditionFlowResults.Clear();
            LinkFailureFlowResults.Clear();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
