using Microsoft.AspNetCore.Mvc;
using YggdrasilApi.Dtos;
using YggdrasilApi.Models;
using YggdrasilApi.Services;

namespace YggdrasilApi.Controllers;

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

    [HttpPost]
    public async Task<ActionResult<GraphResponse>> AddGraph(CreateGraphRequest graph)
    {
        var createdGraph = await service.CreateGraphAsync(graph);
        return CreatedAtAction(nameof(GetGraph), new { id = createdGraph.Id }, createdGraph);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateGraph(int id, UpdateGraphRequest graph)
    {
        var result = await service.UpdateGraphAsync(id, graph);
        return result ? NoContent() : NotFound("Graph with given Id was not found");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteGraph(int id)
    {
        var result = await service.DeleteGraphAsync(id);
        return result ? NoContent() : NotFound("Graph with given Id was not found");
    }
}