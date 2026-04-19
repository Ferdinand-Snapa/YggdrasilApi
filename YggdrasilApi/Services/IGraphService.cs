using YggdrasilApi.Dtos;
using YggdrasilApi.Models;
namespace YggdrasilApi.Services
{
    public interface IGraphService
    {
        Task<List<GraphResponse>> GetAllGraphsAsync();
        Task<GraphResponse?> GetGraphByIdAsync(int id);
        Task<GraphResponse> CreateGraphAsync(CreateGraphRequest graph);
        Task<bool> UpdateGraphAsync(int id, UpdateGraphRequest graph);
        Task<bool> DeleteGraphAsync(int id);
        Task<Node?> AddNodeAsync(int graphId, AddNodeRequest request);
        Task<bool> DeleatNodeAsync(int nodeId);
        Task<bool> UpdateNodeAsync(int nodeId, UpdateNodeRequest request);
        Task<(bool Success, string? Error, Connection? Connection)> AddConnectionAsync(int graphId, Dtos.CreateConnectionRequest request);
        Task<bool> SetNodeValuesAsync(int nodeId, System.Collections.Generic.IDictionary<string, object?> values);
    }
}
