using YggdrasilApi.Dtos;
using YggdrasilApi.GameLogick;
using YggdrasilApi.Models;

namespace YggdrasilApi.Services
{
    public class SessionService : ISessionService
    {
        private readonly Dictionary<string, GameSession> _sessions = new();

        public Task<List<UnitResponse>> GetAllUnitsAsync()
        {
            throw new NotImplementedException();
        }

        public GameSession CreateSession(string sessionId)
        {
            var session = new GameSession(sessionId);
            _sessions[sessionId] = session;
            return session;
        }

        public GameSession? GetSession(string sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public void DeleteSession(string sessionId)
        {
            _sessions.Remove(sessionId);
        }

        public void AddPlayerToSession(string sessionId, Player player)
        {
            var session = GetSession(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");

            session.AddPlayer(player);
        }

        public Player? GetPlayer(string sessionId, string playerId)
        {
            var session = GetSession(sessionId);
            return session?.GetPlayer(playerId);
        }

        public void AddUnitToSession(string sessionId, int unitId, Unit unit)
        {
            var session = GetSession(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");

            session.AddUnit(unitId, unit);
        }

        public Unit? GetUnit(string sessionId, int unitId)
        {
            var session = GetSession(sessionId);
            return session?.GetUnit(unitId);
        }

        public void AssignUnitToPlayer(string sessionId, int unitId, string playerId)
        {
            var session = GetSession(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");

            if (!session.AssignUnitToPlayer(unitId, playerId))
                throw new InvalidOperationException($"Failed to assign unit '{unitId}' to player '{playerId}'.");
        }

        public List<Unit> GetPlayerUnits(string sessionId, string playerId)
        {
            var session = GetSession(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");

            return session.GetPlayerUnits(playerId);
        }

        public UserInputRequest RequestUnitInput(string sessionId, int unitId, string requestType, Dictionary<string, object?> inputSchema)
        {
            var session = GetSession(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");

            return session.RequestUnitInput(unitId, requestType, inputSchema);
        }

        public UserInputRequest? GetInputRequest(string sessionId, string requestId)
        {
            var session = GetSession(sessionId);
            return session?.GetPendingInputRequest(requestId);
        }

        public List<UserInputRequest> GetUnitPendingInputRequests(string sessionId, int unitId)
        {
            var session = GetSession(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");

            return session.GetUnitPendingInputRequests(unitId);
        }

        public List<UserInputRequest> GetAllPendingInputRequests(string sessionId)
        {
            var session = GetSession(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");

            return session.GetAllPendingInputRequests();
        }

        public void ResolveInputRequest(string sessionId, string requestId, object? response)
        {
            var session = GetSession(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");

            if (!session.ResolveInputRequest(requestId, response))
                throw new KeyNotFoundException($"Input request '{requestId}' not found.");
        }

        public UserInputRequest RequestDiceRoll(string sessionId, int unitId, Dice dice)
        {
            var session = GetSession(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session '{sessionId}' not found.");

            var diceSpec = new Dictionary<string, object?>();
            diceSpec["DiceNotation"] = dice.ToNotation();
            diceSpec["Sides"] = dice.Rolls.Keys.ToList();
            diceSpec["Counts"] = dice.Rolls.Values.ToList();

            return session.RequestUnitInput(unitId, "DiceRoll", diceSpec);
        }
    }
}
