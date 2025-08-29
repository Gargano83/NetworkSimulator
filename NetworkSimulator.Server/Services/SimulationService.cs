using Microsoft.AspNetCore.SignalR;
using NetworkSimulator.Server.Hubs;
using NetworkSimulator.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NetworkSimulator.Server.Services
{
    /// <summary>
    /// Gestisce la logica principale della simulazione di rete in tempo reale.
    /// Mantiene lo stato della rete e dei pacchetti, e scandisce il tempo della simulazione.
    /// </summary>
    public class SimulationService : IDisposable
    {
        private readonly IHubContext<SimulationHub> _hubContext;
        private Timer? _timer;
        private GraphData? _networkGraph;
        private List<DataPacket> _packets = new List<DataPacket>();
        private readonly Random _random = new Random();
        private int _simulationTime = 0;
        public bool IsRunning { get; private set; } = false;
        private string _activeMetric = "latency";

        /// <summary>
        /// Costruttore che riceve il contesto dell'Hub SignalR per poter comunicare con i client.
        /// </summary>
        public SimulationService(IHubContext<SimulationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void StartSimulation(GraphData graph, string metric)
        {
            if (IsRunning) return;
            _networkGraph = graph;
            _packets.Clear();
            _simulationTime = 0;
            IsRunning = true;
            _timer = new Timer(SimulationStep, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            _activeMetric = metric;
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
        /// Genera nuovi pacchetti di dati dai nodi di tipo Sensore.
        /// </summary>
        private void GenerateTraffic()
        {
            if (_networkGraph?.Nodes == null) return;
            var sensors = _networkGraph.Nodes.Where(n => n.Type == NodeType.Sensor);
            foreach (var sensor in sensors)
            {
                // La probabilità di generare un pacchetto dipende dal rapporto tra DataRate e PacketSize
                if (_random.NextDouble() < (sensor.DataRate / (sensor.PacketSize ?? 1.0)))
                {
                    // La destinazione "Internet" deve esistere nella topologia
                    var internetNode = _networkGraph.Nodes.FirstOrDefault(n => n.Type == NodeType.Internet);
                    if (internetNode == null) continue;

                    var pathResult = CalculateDijkstraPath(_networkGraph, sensor.Id, internetNode.Id, _activeMetric);

                    // Crea il pacchetto solo se un percorso valido esiste
                    if (pathResult.Path != null && pathResult.Path.Count > 1)
                    {
                        _hubContext.Clients.All.SendAsync("PathCalculated", pathResult, "Dijkstra", _activeMetric);

                        var newPacket = new DataPacket
                        {
                            SourceId = sensor.Id,
                            DestinationId = internetNode.Id,
                            CurrentLocationId = sensor.Id,
                            Size = sensor.PacketSize ?? 1,
                            Priority = sensor.LatencyRequirement < 50 ? 1 : 5,
                            FullPath = pathResult.Path // Salva l'intero percorso nel pacchetto
                        };
                        _packets.Add(newPacket);
                    }
                }
            }
        }

        /// <summary>
        /// Per ogni pacchetto, decide il prossimo passo e aggiorna la sua posizione.
        /// </summary>
        private void RouteAndMovePackets()
        {
            // Rimuove i pacchetti che hanno completato il loro percorso
            _packets.RemoveAll(p => p.FullPath != null && p.PathIndex >= p.FullPath.Count - 1);

            foreach (var packet in _packets)
            {
                if (packet.FullPath == null) continue;

                // Incrementa l'indice per avanzare al prossimo nodo nel percorso pianificato
                packet.PathIndex++;

                // Aggiorna la posizione precedente e attuale
                packet.PreviousLocationId = packet.FullPath[packet.PathIndex - 1];
                packet.CurrentLocationId = packet.FullPath[packet.PathIndex];
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
                    // Vogliamo massimizzare la banda, quindi minimizziamo il suo inverso.
                    // Si aggiunge un valore piccolo per evitare la divisione per zero.
                    return 1.0 / (link.Bandwidth + 0.0001);

                case "reliability":
                    // Vogliamo massimizzare l'affidabilità (vicina a 1.0), quindi minimizziamo l'inaffidabilità (1.0 - reliability).
                    return 1.0 - link.Reliability;

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