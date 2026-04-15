using YggdrasilApi.Models;
namespace YggdrasilApi.Services
{
    public interface IGraphService
    {
        Task<List<Graph>> GetAllGraphsAsync();
        Task<Graph?> GetGraphByIdAsync(int id);
        Task<Graph> CreateGraphAsync(Graph graph);
        Task<bool> UpdateGraphAsync(int id);
        Task<bool> DeleteGraphAsync(int id);
    }
}
