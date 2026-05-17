using Microsoft.EntityFrameworkCore;
using YggdrasilApi.Data;
using YggdrasilApi.Dtos;
using YggdrasilApi.Models;
using YggdrasilApi.Utils;

namespace YggdrasilApi.Services;

public class GraphService(AppDbContext context) : IGraphService
{
    public async Task<GraphResponse> CreateGraphAsync(CreateGraphRequest graph)
    {
        var newGraph = new Graph
        {
            Name = graph.Name
        };

        context.Graphs.Add(newGraph);
        await context.SaveChangesAsync();

        return new GraphResponse
        {
            Id = newGraph.Id,
            Name = newGraph.Name,
            Nodes = newGraph.Nodes,
            Connections = newGraph.Connections
        };
    }

    public async Task<bool> DeleteGraphAsync(int id)
    {
        var graphToDelete = await context.Graphs
            .Include(g => g.Nodes)
            .Include(g => g.Connections)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (graphToDelete is null)
            return false;

        if (graphToDelete.Nodes?.Count > 0)
            context.Nodes.RemoveRange(graphToDelete.Nodes);

        if (graphToDelete.Connections?.Count > 0)
            context.Connections.RemoveRange(graphToDelete.Connections);

        context.Graphs.Remove(graphToDelete);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<List<GraphResponse>> GetAllGraphsAsync()
        => await context.Graphs
        .Include(g => g.Nodes)
        .Include(g => g.Connections)
        .Select(g => new GraphResponse
        {
            Id = g.Id,
            Name = g.Name,
            Nodes = g.Nodes,
            Connections = g.Connections
        }).ToListAsync();

    public async Task<GraphResponse?> GetGraphByIdAsync(int id)
    {
        var result = await context.Graphs
            .Where(g => g.Id == id)
            .Include(g => g.Nodes)
            .Include(g => g.Connections)
            .Select(g => new GraphResponse
            {
                Id = g.Id,
                Name = g.Name,
                Nodes = g.Nodes,
                Connections = g.Connections
            })
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<bool> UpdateGraphAsync(int id, UpdateGraphRequest graph)
    {
        var existingGraph = await context.Graphs.FindAsync(id);

        if (existingGraph is null)
            return false;

        existingGraph.Name = graph.Name;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetNodeValuesAsync(string nodeId, IDictionary<string, object?> values)
    {
        var node = await context.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId);
        if (node is null)
            return false;

        // Replace node values and persist
        node.Values = values.ToDictionary(k => k.Key, v => v.Value);
        node.SaveValues();

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error, Connection? Connection)> AddConnectionAsync(int graphId, CreateConnectionRequest request)
    {
        var graph = await context.Graphs
            .Include(g => g.Nodes)
            .Include(g => g.Connections)
            .FirstOrDefaultAsync(g => g.Id == graphId);

        if (graph is null)
            return (false, "Graph not found", null);

        var fromNode = graph.Nodes.FirstOrDefault(n => n.Id == request.FromNodeId);
        var toNode = graph.Nodes.FirstOrDefault(n => n.Id == request.ToNodeId);

        if (fromNode is null)
            return (false, "FromNodeId does not exist in the specified graph", null);

        if (toNode is null)
            return (false, "ToNodeId does not exist in the specified graph", null);

        var fromDef = fromNode.Definition;
        var toDef = toNode.Definition;

        if (fromDef is null)
            return (false, $"Node type '{fromNode.Type}' for FromNodeId is not registered", null);

        if (toDef is null)
            return (false, $"Node type '{toNode.Type}' for ToNodeId is not registered", null);

        var outPort = fromDef.OutputPorts.FirstOrDefault(p => p.PortId == request.FromPortId);
        var inPort = toDef.InputPorts.FirstOrDefault(p => p.PortId == request.ToPortId);

        if (outPort is null)
            return (false, "FromPortId not found on the From node's definition", null);

        if (inPort is null)
            return (false, "ToPortId not found on the To node's definition", null);

        // FlowPort ↔ DataPort connections are never valid.
        bool outIsFlow = outPort is FlowPort;
        bool inIsFlow = inPort is FlowPort;
        if (outIsFlow != inIsFlow)
            return (false, "Cannot connect a FlowPort to a DataPort.", null);

        // For data ports, validate that the output type is accepted by the input type.
        if (!outIsFlow)
        {
            var outFieldType = ((DataPort)outPort).PortType;
            var inFieldType = ((DataPort)inPort).PortType;
            if (!TypeCompatibility.IsCompatible(outFieldType, inFieldType))
                return (false,
                    $"Output type {outFieldType} is not compatible with input type {inFieldType}.",
                    null);
        }

        var conn = new Connection
        {
            FromNodeId = request.FromNodeId,
            FromPortId = request.FromPortId,
            ToNodeId = request.ToNodeId,
            ToPortId = request.ToPortId
        };

        graph.Connections.Add(conn);
        await context.SaveChangesAsync();

        return (true, null, conn);
    }

    public async Task<Node?> AddNodeAsync(int graphId, AddNodeRequest request)
    {
        var graph = await context.Graphs.FindAsync(graphId);
        if (graph is null)
            return null;

        var node = new Node
        {
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            Type = request.Type
        };

        context.Nodes.Add(node);
        graph.Nodes.Add(node);
        await context.SaveChangesAsync();
        return node;
    }

    public async Task<bool> DeleatNodeAsync(string nodeId)
    {
        var node = await context.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId);

        if (node is null)
            return false;

        context.Nodes.Remove(node);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateNodeAsync(string nodeId, UpdateNodeRequest request)
    {
        var node = await context.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId);

        if (node is null)
            return false;

        if (request.Type != "") node.Type = request.Type;
        node.PositionX = request.PositionX;
        node.PositionY = request.PositionY;

        await context.SaveChangesAsync();
        return true;
    }
}
