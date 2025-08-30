using Microsoft.ML;
using Microsoft.ML.Trainers;
using NetworkSimulator.Shared;
using System.Collections.Concurrent;

namespace NetworkSimulator.Server.Services
{
    /// <summary>
    /// Un agente intelligente che usa la libreria ML.NET per le decisioni di routing.
    /// </summary>
    public class MlNetRoutingAgent
    {
        // Classi per definire l'input e l'output del nostro modello
        private class StateActionInput { public uint State { get; set; } public uint Action { get; set; } public float Score { get; set; } }
        private class QValueOutput { public float Score { get; set; } }

        private readonly MLContext _mlContext;
        private PredictionEngine<StateActionInput, QValueOutput> _predictionEngine;
        private ITransformer _model;

        // Usiamo una coda concorrente per raccogliere le "esperienze" in modo thread-safe
        private readonly ConcurrentQueue<StateActionInput> _experienceBuffer = new ConcurrentQueue<StateActionInput>();
        private readonly object _modelLock = new object(); // Oggetto per gestire l'accesso al modello

        // Parametri dell'algoritmo
        private readonly double _explorationRate = 0.2; // Epsilon più alto all'inizio
        private readonly Random _random = new Random();

        public MlNetRoutingAgent()
        {
            _mlContext = new MLContext(seed: 0);
            // Avvia il processo di apprendimento in background
            Task.Run(() => TrainingLoop());
        }

        /// <summary>
        /// Sceglie il prossimo passo usando il modello di ML.NET.
        /// </summary>
        public string ChooseNextHop(GraphData graph, string currentNodeId, string destinationId)
        {
            var possibleMoves = graph.Links.Where(l => l.From == currentNodeId).ToList();
            if (!possibleMoves.Any()) return currentNodeId;

            // L'esplorazione avviene se non abbiamo ancora un modello o casualmente
            if (_predictionEngine == null || _random.NextDouble() < _explorationRate)
            {
                return possibleMoves[_random.Next(possibleMoves.Count)].To;
            }

            // Sfruttamento: usa il motore di previsione attuale (operazione velocissima)
            string bestMove = possibleMoves.First().To;
            float maxQValue = float.MinValue;

            // Usa una copia locale del motore di previsione per sicurezza in ambiente multi-threaded
            PredictionEngine<StateActionInput, QValueOutput> localPredictionEngine;
            lock (_modelLock)
            {
                localPredictionEngine = _predictionEngine;
            }

            foreach (var move in possibleMoves)
            {
                var prediction = localPredictionEngine.Predict(new StateActionInput
                {
                    State = GetStateId(currentNodeId, destinationId),
                    Action = GetActionId(move.To)
                });

                if (prediction.Score > maxQValue)
                {
                    maxQValue = prediction.Score;
                    bestMove = move.To;
                }
            }
            return bestMove;
        }

        /// <summary>
        /// Metodo super-veloce: aggiunge solo l'esperienza a un buffer, non addestra.
        /// </summary>
        public void UpdateModel(string previousNodeId, string currentNodeId, string destinationId, double reward)
        {
            _experienceBuffer.Enqueue(new StateActionInput
            {
                State = GetStateId(previousNodeId, destinationId),
                Action = GetActionId(currentNodeId),
                Score = (float)reward
            });
        }

        /// <summary>
        /// Ciclo di apprendimento che viene eseguito in un thread separato in background.
        /// </summary>
        private async Task TrainingLoop()
        {
            while (true)
            {
                // Attende 5 secondi prima di ogni ciclo di addestramento
                await Task.Delay(TimeSpan.FromSeconds(5));

                if (_experienceBuffer.IsEmpty) continue;

                // Svuota il buffer di tutte le esperienze raccolte
                var trainingBatch = new List<StateActionInput>();
                while (_experienceBuffer.TryDequeue(out var experience))
                {
                    trainingBatch.Add(experience);
                }

                if (trainingBatch.Count == 0) continue;

                Console.WriteLine($"[ML.NET Agent] Addestramento su un batch di {trainingBatch.Count} esperienze...");

                var dataView = _mlContext.Data.LoadFromEnumerable(trainingBatch);

                var estimator = _mlContext.Recommendation().Trainers.MatrixFactorization(new MatrixFactorizationTrainer.Options
                {
                    MatrixColumnIndexColumnName = nameof(StateActionInput.State),
                    MatrixRowIndexColumnName = nameof(StateActionInput.Action),
                    LabelColumnName = nameof(StateActionInput.Score),
                    NumberOfIterations = 50,
                    ApproximationRank = 10
                });

                // L'addestramento (la parte lenta) avviene qui, in background
                var newModel = estimator.Fit(dataView);
                var newPredictionEngine = _mlContext.Model.CreatePredictionEngine<StateActionInput, QValueOutput>(newModel);

                // Sostituisce il vecchio modello con quello nuovo in modo sicuro
                lock (_modelLock)
                {
                    _model = newModel;
                    _predictionEngine = newPredictionEngine;
                }
                Console.WriteLine("[ML.NET Agent] Modello aggiornato.");
            }
        }

        // Metodi helper per mappare gli ID (invariati)
        private Dictionary<string, uint> _stateToActionMap = new Dictionary<string, uint>();
        private uint _nextActionId = 0;
        private uint GetStateId(string current, string dest) => GetActionId($"{current}-{dest}");
        private uint GetActionId(string action)
        {
            if (!_stateToActionMap.ContainsKey(action)) _stateToActionMap[action] = _nextActionId++;
            return _stateToActionMap[action];
        }
    }
}
