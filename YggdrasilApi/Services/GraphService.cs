using Microsoft.EntityFrameworkCore;
using YggdrasilApi.Data;
using YggdrasilApi.Dtos;
using YggdrasilApi.Models;

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
        .Select(g => new GraphResponse{
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
}
