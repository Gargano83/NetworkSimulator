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

        [HttpPost("dijkstra/{startNodeId}/{endNodeId}")]
        public ActionResult<PathResult> RunDijkstra([FromBody] GraphData graph, string startNodeId, string endNodeId)
        {
            var result = _simulationService.CalculateDijkstraPath(graph, startNodeId, endNodeId);

            if (result.Error != null)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("bellman-ford/{startNodeId}/{endNodeId}")]
        public ActionResult<PathResult> RunBellmanFord([FromBody] GraphData graph, string startNodeId, string endNodeId)
        {
            var result = _simulationService.CalculateBellmanFordPath(graph, startNodeId, endNodeId);

            if (!string.IsNullOrEmpty(result.Error))
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("aco/{startNodeId}/{endNodeId}")]
        public ActionResult<PathResult> RunAco([FromBody] GraphData graph, string startNodeId, string endNodeId)
        {
            var result = _simulationService.CalculateAcoPath(graph, startNodeId, endNodeId);

            if (!string.IsNullOrEmpty(result.Error))
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
