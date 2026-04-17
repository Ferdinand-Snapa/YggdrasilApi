using Microsoft.AspNetCore.Mvc;
using YggdrasilApi.Dtos;
using YggdrasilApi.Models;
using YggdrasilApi.Services;

namespace YggdrasilApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphController(IGraphService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<GraphResponse>>> GetGraphs()
            => Ok(await service.GetAllGraphsAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<GraphResponse>> GetGraph(int id)
        {
            var graph = await service.GetGraphByIdAsync(id);
            return graph is null ? NotFound("Graph with given Id was not found") : Ok(graph);
        }
    }
}
