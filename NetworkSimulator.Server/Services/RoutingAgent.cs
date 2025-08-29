using NetworkSimulator.Shared;

namespace NetworkSimulator.Server.Services
{
    /// <summary>
    /// Un agente intelligente che utilizza l'algoritmo di Q-Learning per prendere decisioni di routing.
    /// </summary>
    public class RoutingAgent
    {
        // La Q-Table memorizza il "valore" di ogni azione in un dato stato.
        // Struttura: Dictionary<stato, Dictionary<azione, valore_Q>>
        // Stato = "nodoCorrente-destinazioneFinale"
        // Azione = "prossimoNodo"
        private Dictionary<string, Dictionary<string, double>> _qTable = new Dictionary<string, Dictionary<string, double>>();

        private readonly Random _random = new Random();

        // Parametri dell'algoritmo di apprendimento
        private readonly double _learningRate = 0.1; // Alpha: quanto velocemente impariamo
        private readonly double _discountFactor = 0.9; // Gamma: importanza delle ricompense future
        private readonly double _explorationRate = 0.1; // Epsilon: probabilità di esplorare nuove rotte

        /// <summary>
        /// Sceglie il prossimo passo per un pacchetto, bilanciando esplorazione e sfruttamento.
        /// </summary>
        public string ChooseNextHop(GraphData graph, string currentNodeId, string destinationId)
        {
            var possibleMoves = graph.Links.Where(l => l.From == currentNodeId).Select(l => l.To).ToList();
            if (!possibleMoves.Any()) return currentNodeId; // Sackgasse, il pacchetto si ferma

            string state = $"{currentNodeId}-{destinationId}";
            EnsureStateExists(state, possibleMoves);

            // Decisione: esplorare o sfruttare?
            if (_random.NextDouble() < _explorationRate)
            {
                // Esplorazione: scegli una mossa casuale
                return possibleMoves[_random.Next(possibleMoves.Count)];
            }
            else
            {
                // Sfruttamento: scegli la mossa con il valore Q più alto
                return _qTable[state].OrderByDescending(kv => kv.Value).First().Key;
            }
        }

        /// <summary>
        /// Aggiorna la Q-Table dopo che un pacchetto si è mosso, imparando dalla mossa fatta.
        /// </summary>
        public void UpdateQValue(GraphData graph, string previousNodeId, string currentNodeId, string destinationId, Link linkTaken)
        {
            string previousState = $"{previousNodeId}-{destinationId}";
            string action = currentNodeId;
            string currentState = $"{currentNodeId}-{destinationId}";

            // 1. Calcola la ricompensa per l'azione intrapresa.
            // Una bassa latenza dà una ricompensa alta.
            double reward = 100.0 / linkTaken.Latency;

            // 2. Trova il massimo valore Q possibile dal nuovo stato
            var possibleNextMoves = graph.Links.Where(l => l.From == currentNodeId).Select(l => l.To).ToList();
            EnsureStateExists(currentState, possibleNextMoves);
            double maxFutureQ = _qTable[currentState].Any() ? _qTable[currentState].Max(kv => kv.Value) : 0;

            // 3. Applica la formula di Bellman per aggiornare il valore Q
            double oldQValue = _qTable[previousState][action];
            double newQValue = oldQValue + _learningRate * (reward + _discountFactor * maxFutureQ - oldQValue);

            _qTable[previousState][action] = newQValue;
        }

        /// <summary>
        /// Assicura che uno stato esista nella Q-Table. Se non esiste, lo inizializza.
        /// </summary>
        private void EnsureStateExists(string state, List<string> possibleActions)
        {
            if (!_qTable.ContainsKey(state))
            {
                _qTable[state] = new Dictionary<string, double>();
                foreach (var action in possibleActions)
                {
                    _qTable[state][action] = 0.0; // Inizializza a 0
                }
            }
        }
    }
}