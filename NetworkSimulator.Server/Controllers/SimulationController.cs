using Microsoft.AspNetCore.Mvc;
using NetworkSimulator.Server.Services;
using NetworkSimulator.Shared;

namespace NetworkSimulator.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimulationController : ControllerBase
    {
        private readonly SimulationService _simulationService;

        public SimulationController(SimulationService simulationService)
        {
            _simulationService = simulationService;
        }

        /// <summary>
        /// Avvia la simulazione con la topologia di rete fornita.
        /// </summary>
        [HttpPost("start")]
        public IActionResult StartSimulation([FromBody] GraphData graph, [FromQuery] string routingAlgorithm = "Dijkstra", [FromQuery] string metric = "latency")
        {
            if (graph == null || graph.Nodes.Count == 0)
            {
                return BadRequest("La topologia della rete non può essere vuota.");
            }
            _simulationService.StartSimulation(graph, routingAlgorithm, metric);
            return Ok("Simulazione avviata.");
        }

        /// <summary>
        /// Ferma la simulazione in corso.
        /// </summary>
        [HttpPost("stop")]
        public IActionResult StopSimulation()
        {
            _simulationService.StopSimulation();
            return Ok("Simulazione fermata.");
        }

        /// <summary>
        /// Restituisce lo stato attuale della simulazione (in esecuzione o no).
        /// </summary>
        [HttpGet("status")]
        public ActionResult<bool> GetStatus()
        {
            return Ok(_simulationService.IsRunning);
        }

        [HttpGet("stats")]
        public ActionResult<SimulationStats> GetStats()
        {
            return Ok(_simulationService.GetSimulationStats());
        }
    }
}
