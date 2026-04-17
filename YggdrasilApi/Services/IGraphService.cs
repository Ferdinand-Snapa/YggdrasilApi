using YggdrasilApi.Dtos;
using YggdrasilApi.Models;
namespace YggdrasilApi.Services
{
    public interface IGraphService
    {
        Task<List<GraphResponse>> GetAllGraphsAsync();
        Task<GraphResponse?> GetGraphByIdAsync(int id);
        Task<GraphResponse> CreateGraphAsync(Graph graph);
        Task<bool> UpdateGraphAsync(int id);
        Task<bool> DeleteGraphAsync(int id);
    }
}
