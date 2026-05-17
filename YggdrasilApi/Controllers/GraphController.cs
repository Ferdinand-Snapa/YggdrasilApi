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

    [HttpPost("{id}/nodes")]
    public async Task<ActionResult<Node>> AddNode(int id, AddNodeRequest request)
    {
        var node = await service.AddNodeAsync(id, request);
        return node is null ? NotFound("Graph with given Id was not found") : CreatedAtAction(nameof(GetGraph), new { id = id }, node);
    }

    [HttpPost("{id}/connections")]
    public async Task<ActionResult> AddConnection(int id, CreateConnectionRequest request)
    {
        var (success, error, connection) = await service.AddConnectionAsync(id, request);
        if (!success)
            return BadRequest(error);

        return CreatedAtAction(nameof(GetGraph), new { id = id }, connection);
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

    [HttpDelete("/nodes/{nodeId}")]
    public async Task<ActionResult> DeleteNode(string nodeId)
    {
        var result = await service.DeleatNodeAsync(nodeId);
        return result ? NoContent() : NotFound("Node or Graph not found");
    }

    [HttpPut("/nodes/{nodeId}")]
    public async Task<ActionResult> UpdateNodeAsync(string nodeId, UpdateNodeRequest request)
    {
        var result = await service.UpdateNodeAsync(nodeId, request);
        return result ? NoContent() : NotFound("Node with given Id was not found");
    }

    [HttpPut("/nodes/{nodeId}/values")]
    public async Task<ActionResult> SetNodeValues(string nodeId, SetNodeValuesRequest request)
    {
        var result = await service.SetNodeValuesAsync(nodeId, request.Values);
        return result ? NoContent() : NotFound("Node not found");
    }

}
