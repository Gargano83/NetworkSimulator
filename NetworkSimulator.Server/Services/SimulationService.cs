using Microsoft.AspNetCore.SignalR;
using NetworkSimulator.Server.Hubs;
using NetworkSimulator.Shared;

namespace NetworkSimulator.Server.Services
{
    /// <summary>
    /// Gestisce la logica principale della simulazione di rete in tempo reale.
    /// Mantiene lo stato della rete e dei pacchetti, e scandisce il tempo della simulazione.
    /// </summary>
    public class SimulationService : IDisposable
    {
        private readonly IHubContext<SimulationHub> _hubContext;
        private readonly MlNetRoutingAgent _routingAgent;
        private Timer? _timer;
        private GraphData? _networkGraph;
        private List<DataPacket> _packets = new List<DataPacket>();
        private readonly Random _random = new Random();
        private int _simulationTime = 0;
        public bool IsRunning { get; private set; } = false;
        private string _activeMetric = "latency";
        private string _activeRoutingAlgorithm = "Dijkstra";

        private int _packetsGenerated = 0;
        private int _packetsDelivered = 0;
        private double _totalLatencySum = 0;
        private DateTime _simulationStartTime;

        /// <summary>
        /// Costruttore che riceve il contesto dell'Hub SignalR per poter comunicare con i client.
        /// </summary>
        public SimulationService(IHubContext<SimulationHub> hubContext, MlNetRoutingAgent routingAgent)
        {
            _hubContext = hubContext;
            _routingAgent = routingAgent;
        }

        public void StartSimulation(GraphData graph, string routingAlgorithm, string metric)
        {
            if (IsRunning) return;
            _networkGraph = graph;
            _packets.Clear();
            _simulationTime = 0;
            IsRunning = true;
            _timer = new Timer(SimulationStep, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            _activeMetric = metric;
            _activeRoutingAlgorithm = routingAlgorithm;
            _packetsGenerated = 0;
            _packetsDelivered = 0;
            _totalLatencySum = 0;
            _simulationStartTime = DateTime.UtcNow;
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

            UpdateNetworkConditions();
            GenerateTraffic();
            RouteAndMovePackets();
            BroadcastState();

            // Ogni 5 secondi, calcola e invia le statistiche attuali.
            if (_simulationTime % 5 == 0)
            {
                var currentStats = GetSimulationStats();
                // Invia le statistiche al client tramite un nuovo messaggio SignalR
                _hubContext.Clients.All.SendAsync("UpdateLiveStats", _simulationTime, currentStats);
            }
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
                link.Latency += (_random.NextDouble() - 0.5) * 2; // Variazione di +/- 1ms
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
        /// NUOVO: Metodo che calcola e restituisce le statistiche finali.
        /// </summary>
        public SimulationStats GetSimulationStats()
        {
            var elapsedSeconds = (DateTime.UtcNow - _simulationStartTime).TotalSeconds;
            if (elapsedSeconds < 1) elapsedSeconds = 1;

            return new SimulationStats
            {
                PacketsGenerated = _packetsGenerated,
                PacketsDelivered = _packetsDelivered,
                AverageLatency = _packetsDelivered > 0 ? _totalLatencySum / _packetsDelivered : 0,
                // Throughput calcolato come totale dei pacchetti arrivati diviso il tempo
                Throughput = _packets.Sum(p => p.Size) / elapsedSeconds
            };
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
                        CreationTime = _simulationTime
                    };

                    _packets.Add(newPacket);
                    _packetsGenerated++;
                    // Invia un log eventi in tempo reale
                    _hubContext.Clients.All.SendAsync("LogEvent", $"[{_simulationTime}s] Pacchetto generato da {newPacket.SourceId}");
                }
            }
        }

        /// <summary>
        /// Per ogni pacchetto, decide il prossimo passo, aggiorna lo stato e invia i dati al frontend.
        /// </summary>
        private void RouteAndMovePackets()
        {
            // Modifichiamo la logica di rimozione
            var deliveredPackets = _packets.Where(p => p.CurrentLocationId == p.DestinationId).ToList();
            foreach (var packet in deliveredPackets)
            {
                _packetsDelivered++;
                _totalLatencySum += (_simulationTime - packet.CreationTime); // Assumendo che aggiungi CreationTime al DataPacket
            }
            _packets.RemoveAll(p => p.CurrentLocationId == p.DestinationId);

            bool hasPanelBeenUpdatedThisTick = false;

            foreach (var packet in _packets)
            {
                if (_networkGraph == null || packet.FullPath == null) continue;

                string nextHop = packet.CurrentLocationId;

                // 1. DECISIONE DI ROUTING (RISPETTA LA SCELTA DELL'UTENTE)
                if (_activeRoutingAlgorithm.Equals("AI", StringComparison.OrdinalIgnoreCase))
                {
                    // --- MODIFICA #3: Chiama il nuovo agente ---
                    nextHop = _routingAgent.ChooseNextHop(_networkGraph, packet.CurrentLocationId, packet.DestinationId);
                }
                else // Default: usa Dijkstra
                {
                    // Ricalcola il percorso dalla posizione attuale per reagire ai guasti
                    var pathResult = CalculateDijkstraPath(_networkGraph, packet.CurrentLocationId, packet.DestinationId, _activeMetric);
                    if (pathResult.Path != null && pathResult.Path.Count > 1)
                    {
                        nextHop = pathResult.Path[1];
                    }
                }

                // 2. MOVIMENTO E AGGIORNAMENTO STATO PACCHETTO
                if (nextHop != packet.CurrentLocationId)
                {
                    var linkTaken = _networkGraph.Links.FirstOrDefault(l => l.From == packet.CurrentLocationId && l.To == nextHop);
                    if (linkTaken != null)
                    {
                        if (_activeRoutingAlgorithm.Equals("AI", StringComparison.OrdinalIgnoreCase))
                        {
                            // --- MODIFICA #4: Calcola la ricompensa e aggiorna il modello ---
                            double reward = 100.0 / linkTaken.Latency;
                            _routingAgent.UpdateModel(packet.CurrentLocationId, nextHop, packet.DestinationId, reward);
                        }

                        packet.FullPath.Add(nextHop);
                        packet.PathIndex = packet.FullPath.Count - 1;
                        packet.PreviousLocationId = packet.CurrentLocationId;
                        packet.CurrentLocationId = nextHop;
                    }
                }

                // --- 3. AGGIORNAMENTO PANNELLO (CORRETTO E SINCRONIZZATO) ---
                // Aggiorniamo il pannello solo UNA VOLTA per tick, usando il primo pacchetto come riferimento
                // per evitare sfarfallii e dati incoerenti.
                if (!hasPanelBeenUpdatedThisTick)
                {
                    var pathForDisplay = new PathResult
                    {
                        Path = packet.FullPath,
                        TotalCost = CalculatePathCost(packet.FullPath, _activeMetric)
                    };
                    _hubContext.Clients.All.SendAsync("PathCalculated", pathForDisplay, _activeRoutingAlgorithm, _activeMetric);
                    hasPanelBeenUpdatedThisTick = true;
                }
            }
        }

        /// <summary>
        /// Metodo helper per calcolare il costo totale di un percorso dato.
        /// </summary>
        private double CalculatePathCost(List<string> path, string metric)
        {
            double totalCost = 0;
            if (_networkGraph == null || path.Count < 2) return 0;
            for (int i = 0; i < path.Count - 1; i++)
            {
                var link = _networkGraph.Links.FirstOrDefault(l => l.From == path[i] && l.To == path[i + 1]);
                if (link != null) totalCost += GetMetricValue(link, metric);
            }
            return totalCost;
        }

        /// <summary>
        /// Invia lo stato corrente della simulazione a tutti i client del frontend tramite SignalR.
        /// </summary>
        private async void BroadcastState()
        {
            try
            {
                // Invia la lista dei pacchetti e dei link (con le loro latenze aggiornate)
                await _hubContext.Clients.All.SendAsync("UpdateSimulationState", _packets, _networkGraph.Links);
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