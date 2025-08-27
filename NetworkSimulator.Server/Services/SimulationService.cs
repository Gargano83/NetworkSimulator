using NetworkSimulator.Shared;

namespace NetworkSimulator.Server.Services
{
    public class SimulationService
    {
        public PathResult CalculateDijkstraPath(GraphData graph, string startNodeId, string endNodeId)
        {
            // 1. Inizializzazione
            var distances = new Dictionary<string, double>();
            var predecessors = new Dictionary<string, string>();
            var priorityQueue = new PriorityQueue<string, double>();
            var visited = new HashSet<string>();

            foreach (var node in graph.Nodes)
            {
                distances[node.Id] = double.PositiveInfinity;
            }

            if (!distances.ContainsKey(startNodeId))
            {
                return new PathResult { Error = "Nodo di partenza non trovato." };
            }

            distances[startNodeId] = 0;
            priorityQueue.Enqueue(startNodeId, 0);

            // 2. Ciclo principale dell'algoritmo
            while (priorityQueue.Count > 0)
            {
                var currentNodeId = priorityQueue.Dequeue();

                if (visited.Contains(currentNodeId)) continue;
                visited.Add(currentNodeId);

                if (currentNodeId == endNodeId) break; // Ottimizzazione: fermarsi se la destinazione è stata raggiunta

                var outgoingLinks = graph.Links.Where(l => l.From == currentNodeId);
                foreach (var link in outgoingLinks)
                {
                    var neighborNodeId = link.To;
                    var newDistance = distances[currentNodeId] + link.Weight;

                    if (newDistance < distances[neighborNodeId])
                    {
                        distances[neighborNodeId] = newDistance;
                        predecessors[neighborNodeId] = currentNodeId;
                        priorityQueue.Enqueue(neighborNodeId, newDistance);
                    }
                }
            }

            // 3. Ricostruzione del percorso
            if (!predecessors.ContainsKey(endNodeId))
            {
                return new PathResult { Error = "Nessun percorso trovato." };
            }

            var path = new List<string>();
            var current = endNodeId;
            while (current != null)
            {
                path.Insert(0, current);
                predecessors.TryGetValue(current, out current);
            }

            return new PathResult
            {
                Path = path,
                TotalCost = distances[endNodeId]
            };
        }

        public PathResult CalculateBellmanFordPath(GraphData graph, string startNodeId, string endNodeId)
        {
            // 1. Inizializzazione
            var distances = new Dictionary<string, double>();
            var predecessors = new Dictionary<string, string?>();

            foreach (var node in graph.Nodes)
            {
                distances[node.Id] = double.PositiveInfinity;
                predecessors[node.Id] = null;
            }

            if (!distances.ContainsKey(startNodeId))
            {
                return new PathResult { Error = "Nodo di partenza non trovato." };
            }
            distances[startNodeId] = 0;

            // 2. Rilassamento degli archi (ciclo principale)
            // L'algoritmo ripete il ciclo V-1 volte (V = numero di nodi)
            for (int i = 0; i < graph.Nodes.Count - 1; i++)
            {
                foreach (var link in graph.Links)
                {
                    if (distances[link.From] != double.PositiveInfinity &&
                        distances[link.From] + link.Weight < distances[link.To])
                    {
                        distances[link.To] = distances[link.From] + link.Weight;
                        predecessors[link.To] = link.From;
                    }
                }
            }

            // 3. Controllo per cicli di peso negativo
            // Se riusciamo a migliorare ancora un percorso, significa che c'è un ciclo negativo.
            foreach (var link in graph.Links)
            {
                if (distances[link.From] != double.PositiveInfinity && distances[link.From] + link.Weight < distances[link.To])
                {
                    return new PathResult { Error = "Rilevato ciclo di peso negativo. Impossibile calcolare il percorso." };
                }
            }

            // 4. Ricostruzione del percorso (identica a Dijkstra)
            if (distances[endNodeId] == double.PositiveInfinity)
            {
                return new PathResult { Error = "Nessun percorso trovato dalla partenza alla destinazione." };
            }

            var path = new List<string>();
            var current = endNodeId;
            while (current != null)
            {
                path.Insert(0, current);
                current = predecessors[current];
            }

            return new PathResult
            {
                Path = path,
                TotalCost = distances[endNodeId]
            };
        }

        public PathResult CalculateAcoPath(GraphData graph, string startNodeId, string endNodeId)
        {
            // 1. Parametri dell'algoritmo (potrai "giocarci" per la tesi)
            int numberOfAnts = 10;
            int numberOfIterations = 50;
            double evaporationRate = 0.5; // Quanto feromone evapora a ogni iterazione
            double alpha = 1.0; // Importanza del feromone
            double beta = 2.0;  // Importanza della visibilità (euristica)

            var random = new Random();

            // Inizializza i livelli di feromone su ogni arco a un piccolo valore
            var pheromones = new Dictionary<(string, string), double>();
            foreach (var link in graph.Links)
            {
                pheromones[(link.From, link.To)] = 0.1;
            }

            PathResult bestPathSoFar = new PathResult { TotalCost = double.PositiveInfinity };

            // 2. Ciclo principale delle iterazioni
            for (int i = 0; i < numberOfIterations; i++)
            {
                var antPaths = new List<PathResult>();

                // 3. Muovi ogni formica
                for (int ant = 0; ant < numberOfAnts; ant++)
                {
                    var currentPath = BuildPathForAnt(graph, startNodeId, endNodeId, pheromones, alpha, beta, random);
                    if (currentPath.Path != null)
                    {
                        antPaths.Add(currentPath);
                    }
                }

                // 4. Aggiorna il feromone
                // Evaporazione
                foreach (var key in pheromones.Keys.ToList())
                {
                    pheromones[key] *= (1 - evaporationRate);
                }

                // Deposito
                foreach (var path in antPaths)
                {
                    double pheromoneToAdd = 1.0 / path.TotalCost;
                    for (int j = 0; j < path.Path.Count - 1; j++)
                    {
                        var fromNode = path.Path[j];
                        var toNode = path.Path[j + 1];
                        pheromones[(fromNode, toNode)] += pheromoneToAdd;
                    }
                }

                // Aggiorna il percorso migliore trovato finora
                var bestPathInIteration = antPaths.OrderBy(p => p.TotalCost).FirstOrDefault();
                if (bestPathInIteration != null && bestPathInIteration.TotalCost < bestPathSoFar.TotalCost)
                {
                    bestPathSoFar = bestPathInIteration;
                }
            }

            if (bestPathSoFar.Path == null)
            {
                return new PathResult { Error = "ACO: Nessun percorso trovato." };
            }

            return bestPathSoFar;
        }

        // Metodo helper per costruire il percorso di una singola formica
        private PathResult BuildPathForAnt(GraphData graph, string startNodeId, string endNodeId,
            Dictionary<(string, string), double> pheromones, double alpha, double beta, Random random)
        {
            var path = new List<string> { startNodeId };
            var visited = new HashSet<string> { startNodeId };
            var current = startNodeId;
            double totalCost = 0;

            while (current != endNodeId)
            {
                var possibleMoves = graph.Links.Where(l => l.From == current && !visited.Contains(l.To)).ToList();
                if (!possibleMoves.Any()) return new PathResult(); // Sackgasse

                var moveProbabilities = new Dictionary<string, double>();
                double totalProbability = 0;

                foreach (var move in possibleMoves)
                {
                    double pheromoneLevel = pheromones[(move.From, move.To)];
                    double heuristicValue = 1.0 / move.Weight; // L'euristica è l'inverso del costo

                    double probability = Math.Pow(pheromoneLevel, alpha) * Math.Pow(heuristicValue, beta);
                    moveProbabilities[move.To] = probability;
                    totalProbability += probability;
                }

                // Scelta probabilistica della mossa successiva
                double randomValue = random.NextDouble() * totalProbability;
                string nextNode = "";
                foreach (var move in moveProbabilities)
                {
                    randomValue -= move.Value;
                    if (randomValue <= 0)
                    {
                        nextNode = move.Key;
                        break;
                    }
                }

                // Aggiorna stato formica
                var linkTaken = possibleMoves.First(l => l.To == nextNode);
                totalCost += linkTaken.Weight;
                visited.Add(nextNode);
                path.Add(nextNode);
                current = nextNode;
            }

            return new PathResult { Path = path, TotalCost = totalCost };
        }
    }
}
