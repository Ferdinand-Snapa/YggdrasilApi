using Microsoft.AspNetCore.Mvc;
using YggdrasilApi.Dtos;
using YggdrasilApi.GameLogick;
using YggdrasilApi.Models;
using YggdrasilApi.Services;

namespace YggdrasilApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SessionController(ISessionService service) : ControllerBase
{
    /// <summary>
    /// Creates a new game session.
    /// </summary>
    [HttpPost]
    public ActionResult<string> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            var session = service.CreateSession(request.SessionId);
            return CreatedAtAction(nameof(GetSession), new { sessionId = request.SessionId }, request.SessionId);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    [HttpGet("{sessionId}")]
    public ActionResult<object> GetSession(string sessionId)
    {
        var session = service.GetSession(sessionId);
        if (session == null)
            return NotFound($"Session '{sessionId}' not found.");

        return Ok(new
        {
            session.Id,
            session.CreatedAt,
            PlayerCount = session.ActivePlayerCount,
            UnitCount = session.TotalUnitCount,
            PendingInputRequests = session.GetAllPendingInputRequests().Count,
            Duration = session.Duration
        });
    }

    /// <summary>
    /// Deletes a session.
    /// </summary>
    [HttpDelete("{sessionId}")]
    public ActionResult DeleteSession(string sessionId)
    {
        var session = service.GetSession(sessionId);
        if (session == null)
            return NotFound($"Session '{sessionId}' not found.");

        service.DeleteSession(sessionId);
        return NoContent();
    }

    /// <summary>
    /// Adds a player to a session.
    /// </summary>
    [HttpPost("{sessionId}/players")]
    public ActionResult AddPlayer(string sessionId, [FromBody] AddPlayerRequest request)
    {
        try
        {
            var player = new Player(request.PlayerId, request.PlayerName);
            service.AddPlayerToSession(sessionId, player);
            return CreatedAtAction(nameof(GetPlayer), new { sessionId, playerId = request.PlayerId }, player);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets a player from a session.
    /// </summary>
    [HttpGet("{sessionId}/players/{playerId}")]
    public ActionResult<object> GetPlayer(string sessionId, string playerId)
    {
        try
        {
            var player = service.GetPlayer(sessionId, playerId);
            if (player == null)
                return NotFound($"Player '{playerId}' not found in session '{sessionId}'.");

            var units = service.GetPlayerUnits(sessionId, playerId);
            return Ok(new
            {
                player.Id,
                player.Name,
                UnitCount = player.UnitIds.Count,
                UnitIds = player.UnitIds
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Adds a unit to a session.
    /// </summary>
    [HttpPost("{sessionId}/units")]
    public ActionResult AddUnit(string sessionId, [FromBody] Unit unit, [FromQuery] int unitId)
    {
        try
        {
            service.AddUnitToSession(sessionId, unitId, unit);
            return CreatedAtAction(nameof(GetUnit), new { sessionId, unitId }, unit);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets a unit from a session.
    /// </summary>
    [HttpGet("{sessionId}/units/{unitId}")]
    public ActionResult<Unit> GetUnit(string sessionId, int unitId)
    {
        try
        {
            var unit = service.GetUnit(sessionId, unitId);
            if (unit == null)
                return NotFound($"Unit '{unitId}' not found in session '{sessionId}'.");

            return Ok(unit);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Assigns a unit to a player in a session.
    /// </summary>
    [HttpPost("{sessionId}/assign-unit")]
    public ActionResult AssignUnitToPlayer(string sessionId, [FromBody] AssignUnitRequest request)
    {
        try
        {
            service.AssignUnitToPlayer(sessionId, request.UnitId, request.PlayerId);
            return Ok(new { message = $"Unit {request.UnitId} assigned to player {request.PlayerId}" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets all pending input requests in a session.
    /// This acts as a webhook endpoint for clients to poll for requests.
    /// </summary>
    [HttpGet("{sessionId}/pending-requests")]
    public ActionResult<List<UserInputRequestDto>> GetPendingRequests(string sessionId)
    {
        try
        {
            var requests = service.GetAllPendingInputRequests(sessionId);
            var dtos = requests.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets all pending input requests for a specific unit.
    /// Clients can poll this endpoint to check for new requests for their unit.
    /// </summary>
    [HttpGet("{sessionId}/units/{unitId}/pending-requests")]
    public ActionResult<List<UserInputRequestDto>> GetUnitPendingRequests(string sessionId, int unitId)
    {
        try
        {
            var requests = service.GetUnitPendingInputRequests(sessionId, unitId);
            var dtos = requests.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets a specific input request.
    /// </summary>
    [HttpGet("{sessionId}/requests/{requestId}")]
    public ActionResult<UserInputRequestDto> GetInputRequest(string sessionId, string requestId)
    {
        try
        {
            var request = service.GetInputRequest(sessionId, requestId);
            if (request == null)
                return NotFound($"Input request '{requestId}' not found.");

            return Ok(MapToDto(request));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Requests a dice roll from a unit.
    /// The response should contain the rolled values in the format: { "6": [3, 5], "20": [18] }
    /// </summary>
    [HttpPost("{sessionId}/units/{unitId}/roll-dice")]
    public ActionResult<DiceRollRequestDto> RequestDiceRoll(string sessionId, int unitId, [FromBody] Dice dice)
    {
        try
        {
            var request = service.RequestDiceRoll(sessionId, unitId, dice);
            var dto = new DiceRollRequestDto
            {
                RequestId = request.Id,
                UnitId = unitId,
                DiceSpec = dice.Rolls,
                CreatedAt = request.CreatedAt
            };
            return CreatedAtAction(nameof(GetInputRequest), new { sessionId, requestId = request.Id }, dto);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Resolves an input request with a response.
    /// For dice rolls, the response should be: { "6": [3, 5], "20": [18] }
    /// </summary>
    [HttpPost("{sessionId}/requests/{requestId}/resolve")]
    public ActionResult ResolveInputRequest(string sessionId, string requestId, [FromBody] ResolveInputRequestDto dto)
    {
        try
        {
            service.ResolveInputRequest(sessionId, requestId, dto.Response);
            return Ok(new { message = $"Input request '{requestId}' resolved." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Webhook endpoint for resolving a dice roll request.
    /// Expected POST body: { "rolls": { "6": [3, 5], "20": [18] } }
    /// </summary>
    [HttpPost("{sessionId}/requests/{requestId}/resolve-dice")]
    public ActionResult ResolveDiceRoll(string sessionId, string requestId, [FromBody] DiceRollResponseDto response)
    {
        try
        {
            service.ResolveInputRequest(sessionId, requestId, response.Rolls);
            return Ok(new { message = $"Dice roll request '{requestId}' resolved.", response = response.Rolls });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Helper method to convert UserInputRequest to DTO
    private static UserInputRequestDto MapToDto(UserInputRequest request)
    {
        return new UserInputRequestDto
        {
            Id = request.Id,
            UnitId = request.UnitId,
            RequestType = request.RequestType,
            InputSchema = request.InputSchema,
            Response = request.Response,
            IsResolved = request.IsResolved,
            CreatedAt = request.CreatedAt,
            ResolvedAt = request.ResolvedAt,
            ElapsedTime = request.ElapsedTime
        };
    }
}
