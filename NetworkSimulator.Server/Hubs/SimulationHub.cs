using Microsoft.AspNetCore.SignalR;
using NetworkSimulator.Server.Services;

namespace NetworkSimulator.Server.Hubs
{
    /// <summary>
    /// Gestisce la comunicazione bidirezionale.
    /// Invia lo stato della simulazione ai client e riceve le modifiche alla topologia dai client.
    /// </summary>
    public class SimulationHub : Hub
    {
        private readonly SimulationService _simulationService;

        // Inietta il SimulationService per potergli inoltrare gli aggiornamenti
        public SimulationHub(SimulationService simulationService)
        {
            _simulationService = simulationService;
        }

        /// <summary>
        /// Metodo chiamato dal client ogni volta che la topologia viene modificata
        /// mentre la simulazione è in esecuzione.
        /// </summary>
        public async Task NotifyGraphUpdate(string action, string itemId)
        {
            if (_simulationService.IsRunning)
            {
                _simulationService.UpdateGraph(action, itemId);
            }
        }
    }
}
