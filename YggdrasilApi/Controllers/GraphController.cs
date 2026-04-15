using System.Numerics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YggdrasilApi.Models;
using YggdrasilApi.Services;

namespace YggdrasilApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphController(IGraphService service) : ControllerBase
    {
   
        [HttpGet]
        public async Task<ActionResult<List<Graph>>> GetGraphs()
            => Ok(await service.GetAllGraphsAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<Graph>> GetGraph(int id)
        {
            var graph = await service.GetGraphByIdAsync(id);
            return graph is null ? NotFound("Character with given Id was not found") : Ok(graph);
        }
    }
}
