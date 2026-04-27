using YggdrasilApi.Dtos;
using YggdrasilApi.GameLogick;
using YggdrasilApi.Models;

namespace YggdrasilApi.Services
{
    public interface ISessionService
    {
        Task<List<UnitResponse>> GetAllUnitsAsync();

        // Session management
        GameSession CreateSession(string sessionId);
        GameSession? GetSession(string sessionId);
        void DeleteSession(string sessionId);

        // Player management
        void AddPlayerToSession(string sessionId, Player player);
        Player? GetPlayer(string sessionId, string playerId);

        // Unit management
        void AddUnitToSession(string sessionId, int unitId, Unit unit);
        Unit? GetUnit(string sessionId, int unitId);
        void AssignUnitToPlayer(string sessionId, int unitId, string playerId);
        List<Unit> GetPlayerUnits(string sessionId, string playerId);

        // Input request management
        UserInputRequest RequestUnitInput(string sessionId, int unitId, string requestType, Dictionary<string, object?> inputSchema);
        UserInputRequest? GetInputRequest(string sessionId, string requestId);
        List<UserInputRequest> GetUnitPendingInputRequests(string sessionId, int unitId);
        List<UserInputRequest> GetAllPendingInputRequests(string sessionId);
        void ResolveInputRequest(string sessionId, string requestId, object? response);

        // Dice roll specific
        UserInputRequest RequestDiceRoll(string sessionId, int unitId, Dice dice);
    }
}
