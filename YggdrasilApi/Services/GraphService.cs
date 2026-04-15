using YggdrasilApi.Models;

namespace YggdrasilApi.Services
{
    public class GraphService : IGraphService
    {
        static List<Graph> graphs = new List<Graph> {
            new Graph { Id = 1, Name = "Graph 1", Nodes = [new Node { Id = 1, PositionX = 1, PositionY = 1 }, new Node { Id = 2, PositionX = 1, PositionY = 1 }] },
            new Graph { Id = 2, Name = "Graph 2", Nodes = [new Node { Id = 1, PositionX = 1, PositionY = 1 }, new Node { Id = 2, PositionX = 1, PositionY = 1 }] },
        };
        public Task<Graph> CreateGraphAsync(Graph graph)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteGraphAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Graph>> GetAllGraphsAsync()
            => await Task.FromResult(graphs);

        public async Task<Graph?> GetGraphByIdAsync(int id)
        {
            var result = graphs.FirstOrDefault(g => g.Id == id);
            return await Task.FromResult(result);
        }

        public Task<bool> UpdateGraphAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
