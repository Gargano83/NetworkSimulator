using Microsoft.AspNetCore.SignalR;

namespace NetworkSimulator.Server.Hubs
{
    public class SimulationHub : Hub
    {
        // Questo metodo potrà essere chiamato dal client per inviare un messaggio a tutti
        public async Task BroadcastMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
