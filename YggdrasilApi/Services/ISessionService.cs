using YggdrasilApi.Dtos;
using YggdrasilApi.GameLogick;
using YggdrasilApi.Models;

namespace YggdrasilApi.Services
{
    public interface ISessionService
    {
        Task<List<UnitResponse>> GetAllUnitsAsync();

        // Session management
        GameSession CreateSession(string sessionId, string leaderUserId, string leaderUserName);
        GameSession? GetSession(string sessionId);
        void DeleteSession(string sessionId);

        // User management
        SessionUser AddUserToSession(string sessionId, string userId, string userName);
        SessionUser? GetUser(string sessionId, string userId);
        bool RemoveUserFromSession(string sessionId, string userId);
        bool UpdateUserRole(string sessionId, string userId, SessionUserRole newRole);
        List<SessionUser> GetSessionUsers(string sessionId);
        List<SessionUser> GetPendingUsers(string sessionId);
        List<SessionUser> GetAuthorizedUsers(string sessionId);
        bool CanManageRoles(string sessionId, string userId);
        bool IsUserBlackListed(string sessionId, string userId);

        // Player management
        void AddPlayerToSession(string sessionId, Player player);
        Player? GetPlayer(string sessionId, string playerId);

        // Unit management
        void AddUnitToSession(string sessionId, int unitId, Unit unit);
        Unit? GetUnit(string sessionId, int unitId);
        void AssignUnitToPlayer(string sessionId, int unitId, string playerId);
        List<Unit> GetPlayerUnits(string sessionId, string playerId);

        // Input request management
        UserInputRequest RequestUnitInput(string sessionId, int unitId, string requestType, Dictionary<string, InputField> schema);
        UserInputRequest? GetInputRequest(string sessionId, string requestId);
        List<UserInputRequest> GetUnitPendingInputRequests(string sessionId, int unitId);
        List<UserInputRequest> GetAllPendingInputRequests(string sessionId);
        void ResolveInputRequest(string sessionId, string requestId, object? response);

        // Dice roll specific
        UserInputRequest RequestDiceRoll(string sessionId, int unitId, Dice dice);
    }
}
