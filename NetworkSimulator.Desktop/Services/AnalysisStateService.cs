using NetworkSimulator.Shared;

namespace NetworkSimulator.Desktop.Services
{
    // Una classe per memorizzare un punto dati del grafico (Tempo, Tasso di Consegna)
    public class TimeSeriesDataPoint
    {
        public int Time { get; set; }
        public double DeliveryRate { get; set; }
    }

    // Nuova classe per i dati del grafico del Caso 3
    public class LatencyDataPoint
    {
        public int Time { get; set; }
        public double AverageLatency { get; set; }
    }

    /// <summary>
    /// Servizio Singleton per memorizzare i risultati delle simulazioni
    /// e condividerli con la pagina di Analisi.
    /// </summary>
    public class AnalysisStateService
    {
        private readonly object _lock = new object();

        // Memorizza i risultati finali per la tabella del Caso di Studio 1
        public Dictionary<string, List<FlowStats>> NormalConditionFlowResults { get; set; } = new();

        // Memorizza i dati per il grafico del Caso di Studio 2
        public Dictionary<string, Dictionary<string, List<TimeSeriesDataPoint>>> LinkFailureFlowResults { get; set; } = new();

        // Nuova proprietà per i risultati del Caso 3
        public Dictionary<string, Dictionary<string, List<LatencyDataPoint>>> CongestionTestResults { get; set; } = new();

        public bool IsCongestionTestRunning { get; private set; } = false;
        public bool IsLinkFailureTestRunning { get; private set; } = false;

        // Memorizza tutti i risultati raccolti (es. 5 sensori->10ms, 10 sensori->15ms...)
        public List<ScalabilityTestResult> ScalabilityResults { get; private set; } = new();

        // Flag per sapere se stiamo eseguendo questo test specifico
        public bool IsScalabilityTestRunning { get; private set; } = false;

        // Evento specifico per notificare che c'è un nuovo punto nel grafico di scalabilità
        public event Action? OnScalabilityResultAdded;

        public event Action? OnChange;

        public void AddNormalConditionFlowResults(string algorithmName, List<FlowStats> stats)
        {
            NormalConditionFlowResults[algorithmName] = stats;
            NotifyStateChanged();
        }

        public void AddLiveSimulationData(string algorithmName, int time, List<LiveFlowStats> liveStats)
        {
            lock (_lock)
            {
                // Inizializza i dizionari se non esistono
                if (!LinkFailureFlowResults.ContainsKey(algorithmName))
                    LinkFailureFlowResults[algorithmName] = new Dictionary<string, List<TimeSeriesDataPoint>>();
                if (!CongestionTestResults.ContainsKey(algorithmName))
                    CongestionTestResults[algorithmName] = new Dictionary<string, List<LatencyDataPoint>>();

                // Itera su ogni flusso per aggiornare entrambi i set di dati
                foreach (var flowStat in liveStats)
                {
                    if (IsLinkFailureTestRunning)
                    {
                        // -- Dati per il Grafico 2: Tasso di Consegna --
                        if (!LinkFailureFlowResults[algorithmName].ContainsKey(flowStat.SourceNodeId))
                            LinkFailureFlowResults[algorithmName][flowStat.SourceNodeId] = new List<TimeSeriesDataPoint>();

                        double deliveryRate = (flowStat.PacketsGenerated > 0) ? ((double)flowStat.PacketsDelivered / flowStat.PacketsGenerated) * 100 : 0;
                        var deliveryDataPoints = LinkFailureFlowResults[algorithmName][flowStat.SourceNodeId];
                        if (!deliveryDataPoints.Any(p => p.Time == time))
                            deliveryDataPoints.Add(new TimeSeriesDataPoint { Time = time, DeliveryRate = deliveryRate });
                    }

                    if (IsCongestionTestRunning)
                    {
                        // -- Dati per il Grafico 3: Latenza Media --
                        if (!CongestionTestResults[algorithmName].ContainsKey(flowStat.SourceNodeId))
                            CongestionTestResults[algorithmName][flowStat.SourceNodeId] = new List<LatencyDataPoint>();

                        double averageLatency = (flowStat.PacketsDelivered > 0) ? flowStat.TotalLatencySum / flowStat.PacketsDelivered : 0;
                        var latencyDataPoints = CongestionTestResults[algorithmName][flowStat.SourceNodeId];
                        if (!latencyDataPoints.Any(p => p.Time == time))
                            latencyDataPoints.Add(new LatencyDataPoint { Time = time, AverageLatency = averageLatency });
                    }
                }
            }

            NotifyStateChanged();
        }

        public void ClearCongestionResults()
        {
            lock (_lock)
            {
                CongestionTestResults.Clear();
            }
                
            NotifyStateChanged(); // Notifica la UI per aggiornare il grafico
        }

        public void ClearLinkFailureResults()
        {
            lock (_lock)
            {
                LinkFailureFlowResults.Clear();
            }
            
            NotifyStateChanged(); // Notifica la UI per aggiornare il grafico
        }

        public void SetCongestionTestStatus(bool isRunning)
        {
            IsCongestionTestRunning = isRunning;
        }

        public void SetLinkFailureTestStatus(bool isRunning)
        {
            IsLinkFailureTestRunning = isRunning;
        }

        public void SetScalabilityTestStatus(bool isRunning)
        {
            IsScalabilityTestRunning = isRunning;
            NotifyStateChanged();
        }

        public void AddScalabilityResult(ScalabilityTestResult result)
        {
            lock (_lock)
            {
                ScalabilityResults.Add(result);
            }
            OnScalabilityResultAdded?.Invoke();
        }

        public void Clear()
        {
            lock (_lock)
            {
                NormalConditionFlowResults.Clear();
                LinkFailureFlowResults.Clear();
                CongestionTestResults.Clear();
                IsLinkFailureTestRunning = false;
                IsCongestionTestRunning = false;
                ScalabilityResults.Clear();
                IsScalabilityTestRunning = false;
            }
            
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            // Chiedi al Dispatcher dell'applicazione di eseguire l'azione sul thread UI corretto.
            Application.Current.Dispatcher.Dispatch(() =>
            {
                OnChange?.Invoke();
            });
        }
    }
}
