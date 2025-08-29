using Microsoft.AspNetCore.SignalR;

namespace NetworkSimulator.Server.Hubs
{
    /// <summary>
    /// Hub di SignalR per la comunicazione in tempo reale tra server e client.
    /// In questa architettura, la classe è vuota perché l'invio dei messaggi è gestito dal SimulationService tramite IHubContext.
    /// </summary>
    public class SimulationHub : Hub
    {
        // Questa classe può essere vuota.
        // La sua esistenza è sufficiente per definire il punto di connessione.
    }
}
