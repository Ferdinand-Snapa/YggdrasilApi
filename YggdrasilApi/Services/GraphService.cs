using Microsoft.EntityFrameworkCore;
using YggdrasilApi.Data;
using YggdrasilApi.Dtos;
using YggdrasilApi.Models;

namespace YggdrasilApi.Services;

public class GraphService(AppDbContext context) : IGraphService
{
    public Task<GraphResponse> CreateGraphAsync(Graph graph)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteGraphAsync(int id)
    {
        throw new NotImplementedException();
    }
        
    public async Task<List<GraphResponse>> GetAllGraphsAsync()
        => await context.Graphs
        .Include(g => g.Nodes)
        .Include(g => g.Connections)
        .Select(g => new GraphResponse{
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
                Name = g.Name,
                Nodes = g.Nodes,
                Connections = g.Connections
            })
            .FirstOrDefaultAsync();

        return result;
    }

    public Task<bool> UpdateGraphAsync(int id)
    {
        throw new NotImplementedException();
    }
}
