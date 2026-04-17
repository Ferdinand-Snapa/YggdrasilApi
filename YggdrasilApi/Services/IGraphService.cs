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
    }
}
