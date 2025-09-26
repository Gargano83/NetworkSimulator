using NetworkSimulator.Shared;
using System.Collections.Concurrent;

namespace NetworkSimulator.Desktop.Services
{
    /// <summary>
    /// Gestisce la logica principale della simulazione di rete in tempo reale.
    /// Mantiene lo stato della rete e dei pacchetti, e scandisce il tempo della simulazione.
    /// </summary>
    public class SimulationService : IDisposable
    {
        // Eventi per notificare la UI
        public event Action<string> OnLogEvent;
        public event Action<int, List<LiveFlowStats>> OnUpdateLiveStats;
        public event Action<List<DataPacket>, List<Link>> OnUpdateSimulationState;
        public event Action<List<FlowResult>, string, string> OnUpdateAllPaths;
        public event Action<string, string> OnRemoveNetworkElement;

        private readonly MlNetRoutingAgent _routingAgent;
        private Timer? _timer;
        private GraphData? _networkGraph;
        private List<DataPacket> _packets = new List<DataPacket>();
        private readonly Random _random = new Random();
        private int _simulationTime = 0;
        public bool IsRunning { get; private set; } = false;
        private string _activeMetric = "latency";
        private string _activeRoutingAlgorithm = "Dijkstra";

        private ConcurrentDictionary<string, FlowStats> _flowStats;

        private string? _congestedLinkId;
        private int _congestionTime;
        private double _congestionValue;
        private bool _congestionApplied = false;

        private string? _failureType;
        private string? _failureTargetId;
        private int _failureTime;
        private bool _failureApplied = false;

        /// <summary>
        /// Costruttore che riceve il contesto dell'Hub SignalR per poter comunicare con i client.
        /// </summary>
        public SimulationService(MlNetRoutingAgent routingAgent)
        {
            _routingAgent = routingAgent;
            _flowStats = new ConcurrentDictionary<string, FlowStats>();
        }

        public void NotifyGraphUpdate(string action, object data)
        {
            // Questo metodo sostituisce la vecchia notifica SignalR
            if (data is string elementId)
            {
                UpdateGraph(action, elementId);
            }
        }

        public void StartSimulation(GraphData graph, string routingAlgorithm, string metric,
                                    string? congestedLinkId, int congestionTime, double congestionValue,
                                    string? failureType, string? failureTargetId, int failureTime)
        {
            if (IsRunning) return;
            _networkGraph = graph;
            _packets.Clear();
            _simulationTime = 0;
            IsRunning = true;
            _activeMetric = metric;
            _activeRoutingAlgorithm = routingAlgorithm;
            _flowStats.Clear();

            _congestedLinkId = congestedLinkId;
            _congestionTime = congestionTime;
            _congestionValue = congestionValue;
            _congestionApplied = false; // Resetta lo stato ad ogni avvio

            _failureType = failureType;
            _failureTargetId = failureTargetId;
            _failureTime = failureTime;
            _failureApplied = false; // Resetta lo stato ad ogni avvio

            _timer = new Timer(SimulationStep, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            Console.WriteLine("Simulazione avviata.");
        }

        public void StopSimulation()
        {
            _timer?.Change(Timeout.Infinite, 0);
            IsRunning = false;
            Console.WriteLine("Simulazione fermata.");
        }

        private void SimulationStep(object? state)
        {
            if (!IsRunning || _networkGraph == null) return;
            _simulationTime++;
            Console.WriteLine($"--- Simulation Tick: {_simulationTime}s ---");

            // --- LOGICA PER LA GESTIONE DEL GUASTO ---
            if (!_failureApplied && !string.IsNullOrEmpty(_failureTargetId) && _simulationTime >= _failureTime)
            {
                string logMessage = "";
                if (_failureType == "node")
                {
                    _networkGraph.Links.RemoveAll(l => l.From == _failureTargetId || l.To == _failureTargetId);
                    _networkGraph.Nodes.RemoveAll(n => n.Id == _failureTargetId);
                    logMessage = $"[{_simulationTime}s] SCENARIO: Guasto del nodo {_failureTargetId}. Rimosso dalla topologia.";
                }
                else if (_failureType == "link")
                {
                    _networkGraph.Links.RemoveAll(l => l.Id == _failureTargetId);
                    logMessage = $"[{_simulationTime}s] SCENARIO: Guasto del link {_failureTargetId}. Rimosso dalla topologia.";
                }

                if (!string.IsNullOrEmpty(logMessage))
                {
                    OnLogEvent?.Invoke(logMessage);
                    Console.WriteLine(logMessage);

                    OnRemoveNetworkElement?.Invoke(_failureType, _failureTargetId);
                }
                _failureApplied = true;
            }
            // --- FINE LOGICA GESTIONE DEL GUASTO ---

            // --- APPLICA LA CONGESTIONE AL MOMENTO GIUSTO ---
            if (!_congestionApplied && !string.IsNullOrEmpty(_congestedLinkId) && _simulationTime >= _congestionTime)
            {
                var linkToCongest = _networkGraph.Links.FirstOrDefault(l => l.Id == _congestedLinkId);
                if (linkToCongest != null)
                {
                    linkToCongest.Latency += _congestionValue;
                    _congestionApplied = true;

                    // Invia un log per notificare l'utente
                    var logMessage = $"[{_simulationTime}s] SCENARIO: Congestione applicata al link {linkToCongest.Id}. Latenza aumentata di {_congestionValue}ms.";
                    OnLogEvent?.Invoke(logMessage);
                    Console.WriteLine(logMessage);
                }
            }
            // --- FINE LOGICA CONGESTIONE ---

            UpdateNetworkConditions();
            GenerateTraffic();
            RouteAndMovePackets();
            UpdateResultsPanel();
            BroadcastState();

            // Ogni 5 secondi, calcola e invia le statistiche per-flusso attuali
            if (_simulationTime > 0 && _simulationTime % 5 == 0)
            {
                var livePerFlowStats = _flowStats.Values.Select(f => new LiveFlowStats
                {
                    SourceNodeId = f.SourceNodeId,
                    PacketsGenerated = f.PacketsGenerated,
                    PacketsDelivered = f.PacketsDelivered,
                    TotalLatencySum = f.TotalLatencySum
                }).ToList();

                OnUpdateLiveStats?.Invoke(_simulationTime, livePerFlowStats);
            }
        }

        public FinalSimulationResults GetFinalResults()
        {
            var perFlowStats = _flowStats.Values.ToList();

            // Calcola i totali aggregati dai dati per-flusso
            var totalGenerated = perFlowStats.Sum(f => f.PacketsGenerated);
            var totalDelivered = perFlowStats.Sum(f => f.PacketsDelivered);
            var totalLatency = perFlowStats.Sum(f => f.TotalLatencySum);
            var totalDataDelivered = perFlowStats.Sum(f => f.TotalDataDelivered);

            // Calcola il throughput corretto (KB/s)
            double throughput = 0;
            if (_simulationTime > 0)
            {
                throughput = totalDataDelivered / _simulationTime;
            }

            var aggregateStats = new SimulationStats
            {
                PacketsGenerated = totalGenerated,
                PacketsDelivered = totalDelivered,
                AverageLatency = totalDelivered > 0 ? totalLatency / totalDelivered : 0,
                Throughput = throughput
            };

            return new FinalSimulationResults
            {
                AggregateStats = aggregateStats,
                PerFlowStats = perFlowStats
            };
        }

        /// <summary>
        /// Simula le variazioni delle condizioni di rete (es. congestione, interferenze).
        /// </summary>
        private void UpdateNetworkConditions()
        {
            if (_networkGraph?.Links == null) return;
            foreach (var link in _networkGraph.Links)
            {
                // Simula una variazione casuale della latenza (jitter)
                var newLatency = link.Latency + ((_random.NextDouble() - 0.5) * 2); // Variazione di +/- 1ms
                // Arrotonda il nuovo valore a due cifre decimali
                link.Latency = Math.Round(newLatency, 2);
                if (link.Latency < 1) link.Latency = 1;
            }
        }

        /// <summary>
        /// Aggiorna la copia interna della topologia di rete in tempo reale.
        /// Questo metodo viene chiamato dall'Hub quando il client notifica una modifica.
        /// </summary>
        public void UpdateGraph(string action, string itemId)
        {
            if (_networkGraph == null) return;

            switch (action.ToLower())
            {
                case "removelink":
                    _networkGraph.Links.RemoveAll(l => l.Id == itemId);
                    Console.WriteLine($"[Real-time Update] Link {itemId} rimosso dalla simulazione.");
                    break;

                case "removenode":
                    var nodeToRemove = _networkGraph.Nodes.FirstOrDefault(n => n.Id == itemId);
                    if (nodeToRemove != null)
                    {
                        _networkGraph.Nodes.Remove(nodeToRemove);
                        // Rimuove anche tutti i collegamenti connessi a quel nodo
                        _networkGraph.Links.RemoveAll(l => l.From == itemId || l.To == itemId);
                        Console.WriteLine($"[Real-time Update] Nodo {itemId} e i suoi link rimossi dalla simulazione.");
                    }
                    break;
            }
        }

        /// <summary>
        /// Genera nuovi pacchetti di dati dai nodi di tipo Sensore.
        /// </summary>
        private void GenerateTraffic()
        {
            if (_networkGraph == null) return;
            var sensors = _networkGraph.Nodes.Where(n => n.Type == NodeType.Sensor);
            foreach (var sensor in sensors)
            {
                if (_random.NextDouble() < (sensor.DataRate / (sensor.PacketSize ?? 1.0)))
                {
                    var internetNode = _networkGraph.Nodes.FirstOrDefault(n => n.Type == NodeType.Internet);
                    if (internetNode == null) continue;

                    var newPacket = new DataPacket
                    {
                        SourceId = sensor.Id,
                        DestinationId = internetNode.Id,
                        CurrentLocationId = sensor.Id,
                        FullPath = new List<string> { sensor.Id },
                        PathIndex = 0,
                        Size = sensor.PacketSize ?? 1,
                        Priority = sensor.LatencyRequirement < 50 ? 1 : 5,
                        CreationTime = _simulationTime,
                        ArrivalTimeAtCurrentNode = _simulationTime
                    };

                    _packets.Add(newPacket);
                    // Aggiorna il contatore per il flusso specifico
                    var stats = _flowStats.GetOrAdd(sensor.Id, new FlowStats { SourceNodeId = sensor.Id, DestinationNodeId = internetNode.Id });
                    stats.PacketsGenerated++;
                    // Invia un log eventi in tempo reale
                    OnLogEvent?.Invoke($"[{_simulationTime}s] Pacchetto generato da {newPacket.SourceId}");
                }
            }
        }

        /// <summary>
        /// Calcola il percorso ottimale per ogni sensore attivo e invia un riepilogo al frontend.
        /// </summary>
        private void UpdateResultsPanel()
        {
            if (_networkGraph == null) return;

            // Trova dinamicamente il nodo di destinazione "Internet"
            var internetNode = _networkGraph.Nodes.FirstOrDefault(n => n.Type == NodeType.Internet);
            if (internetNode == null) return; // Se non c'è destinazione, non fare nulla

            // Identifica quali sensori hanno pacchetti attualmente in transito
            var activeSensorIds = _packets.Select(p => p.SourceId).Distinct().ToList();
            var allFlowResults = new List<FlowResult>();

            foreach (var sensorId in activeSensorIds)
            {
                // Per ogni sensore attivo, calcola il suo percorso ottimale attuale
                var pathResult = CalculateDijkstraPath(_networkGraph, sensorId, internetNode.Id, _activeMetric);
                if (pathResult.Path != null)
                {
                    allFlowResults.Add(new FlowResult
                    {
                        SourceId = sensorId,
                        Path = pathResult.Path,
                        TotalCost = pathResult.TotalCost
                    });
                }
            }

            OnUpdateAllPaths?.Invoke(allFlowResults, _activeRoutingAlgorithm, _activeMetric);
        }

        /// <summary>
        /// Per ogni pacchetto, decide il prossimo passo, aggiorna lo stato e invia i dati al frontend.
        /// </summary>
        private void RouteAndMovePackets()
        {
            if (_networkGraph == null) return;

            // 1. Gestisce i pacchetti arrivati a destinazione
            var deliveredPackets = _packets.Where(p => p.CurrentLocationId == p.DestinationId).ToList();
            foreach (var packet in deliveredPackets)
            {
                // Verifica che il pacchetto sia "arrivato" anche temporalmente
                if (_simulationTime >= packet.ArrivalTimeAtCurrentNode)
                {
                    if (_flowStats.TryGetValue(packet.SourceId, out var stats))
                    {
                        stats.PacketsDelivered++;
                        // Converte la latenza calcolata (in secondi) in millisecondi prima di sommarla
                        stats.TotalLatencySum += (packet.ArrivalTimeAtCurrentNode - packet.CreationTime) * 1000.0;
                        stats.TotalDataDelivered += packet.Size;
                        stats.FinalPath = packet.FullPath ?? new List<string>();
                    }
                }
            }
            // Rimuove solo i pacchetti arrivati e processati
            _packets.RemoveAll(p => deliveredPackets.Contains(p) && _simulationTime >= p.ArrivalTimeAtCurrentNode);

            // 2. Gestisce i pacchetti persi (TTL scaduto)
            _packets.RemoveAll(p => p.Ttl <= 0);

            // 3. Muove i pacchetti ancora in transito
            foreach (var packet in _packets)
            {
                // Se il pacchetto non è ancora arrivato (è in transito su un link), non fare nulla per questo tick
                if (_simulationTime < packet.ArrivalTimeAtCurrentNode)
                {
                    continue;
                }

                packet.Ttl--;
                string nextHop = packet.CurrentLocationId;

                // Logica di routing per decidere il nextHop (invariata)
                if (_activeRoutingAlgorithm.Equals("AI", StringComparison.OrdinalIgnoreCase))
                {
                    nextHop = _routingAgent.ChooseNextHop(_networkGraph, packet.CurrentLocationId, packet.DestinationId);
                }
                else
                {
                    var pathResult = CalculateDijkstraPath(_networkGraph, packet.CurrentLocationId, packet.DestinationId, _activeMetric);
                    if (pathResult.Path != null && pathResult.Path.Count > 1)
                    {
                        nextHop = pathResult.Path[1];
                    }
                }

                // Se c'è una mossa da fare...
                if (nextHop != packet.CurrentLocationId)
                {
                    var linkTaken = _networkGraph.Links.FirstOrDefault(l => (l.From == packet.CurrentLocationId && l.To == nextHop) || (l.To == packet.CurrentLocationId && l.From == nextHop));
                    if (linkTaken != null)
                    {
                        double delayInSeconds = linkTaken.Latency / 1000.0;

                        // Aggiorna la posizione e il nuovo tempo di arrivo
                        packet.PreviousLocationId = packet.CurrentLocationId;
                        packet.CurrentLocationId = nextHop;
                        packet.ArrivalTimeAtCurrentNode = _simulationTime + delayInSeconds;

                        // CORREZIONE HIGHLIGHTING: Aggiorna il percorso e l'indice
                        packet.FullPath?.Add(nextHop);
                        packet.PathIndex = (packet.FullPath?.Count ?? 1) - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Invia lo stato corrente della simulazione a tutti i client del frontend tramite SignalR.
        /// </summary>
        private async void BroadcastState()
        {
            try
            {
                // Invia la lista dei pacchetti e dei link (con le loro latenze aggiornate)
                OnUpdateSimulationState?.Invoke(_packets, _networkGraph.Links);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il broadcast SignalR: {ex.Message}");
            }
        }

        /// <summary>
        /// Algoritmo di Dijkstra corretto per usare le nuove metriche.
        /// </summary>
        private PathResult CalculateDijkstraPath(GraphData graph, string startNodeId, string endNodeId, string metric)
        {
            var distances = new Dictionary<string, double>();
            var predecessors = new Dictionary<string, string?>();
            var priorityQueue = new PriorityQueue<string, double>();
            var visited = new HashSet<string>();

            foreach (var node in graph.Nodes)
            {
                distances[node.Id] = double.PositiveInfinity;
                predecessors[node.Id] = null;
            }

            if (!distances.ContainsKey(startNodeId)) return new PathResult { Error = "Nodo di partenza non trovato." };

            distances[startNodeId] = 0;
            priorityQueue.Enqueue(startNodeId, 0);

            while (priorityQueue.Count > 0)
            {
                var currentNodeId = priorityQueue.Dequeue();

                if (visited.Contains(currentNodeId)) continue;
                visited.Add(currentNodeId);

                if (currentNodeId == endNodeId) break;

                var outgoingLinks = graph.Links.Where(l => l.From == currentNodeId);
                foreach (var link in outgoingLinks)
                {
                    var neighborNodeId = link.To;
                    if (!distances.ContainsKey(neighborNodeId)) continue;

                    // ORA QUESTO CALCOLO È CORRETTO CON LE NUOVE METRICHE
                    var newDistance = distances[currentNodeId] + GetMetricValue(link, metric);

                    if (newDistance < distances[neighborNodeId])
                    {
                        distances[neighborNodeId] = newDistance;
                        predecessors[neighborNodeId] = currentNodeId;
                        priorityQueue.Enqueue(neighborNodeId, newDistance);
                    }
                }
            }

            var path = new List<string>();
            var current = endNodeId;
            while (current != null)
            {
                path.Insert(0, current);
                current = predecessors.GetValueOrDefault(current);
            }

            if (path.FirstOrDefault() != startNodeId) return new PathResult { Error = "Nessun percorso trovato." };

            return new PathResult
            {
                Path = path,
                TotalCost = distances[endNodeId]
            };
        }

        /// <summary>
        /// Funzione helper che traduce le proprietà di un link in un "costo" da minimizzare.
        /// </summary>
        private static double GetMetricValue(Link link, string metric)
        {
            switch (metric.ToLower())
            {
                case "bandwidth":
                    // Invertiamo e scaliamo per avere un costo più leggibile.
                    // Una banda più alta (es. 1000 Mbps) avrà un costo più basso.
                    // Una banda più bassa (es. 100 Mbps) avrà un costo più alto.
                    return 1000 / (link.Bandwidth + 0.0001); // Aggiunto epsilon per evitare divisione per zero

                case "reliability":
                    // Trasformiamo l'inaffidabilità (un numero piccolo) in un costo intero più grande.
                    // Un'affidabilità più alta (es. 0.999) avrà un costo più basso.
                    return (1.0 - link.Reliability) * 1000;

                case "latency":
                default:
                    // Vogliamo minimizzare la latenza, quindi usiamo il suo valore diretto.
                    return link.Latency;
            }
        }

        /// <summary>
        /// Ferma il timer quando il servizio viene eliminato per evitare perdite di memoria.
        /// </summary>
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}