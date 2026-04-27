# SessionController API Documentation

## Overview
The SessionController provides a webhook-based system for managing game sessions, players, units, and user input requests (including dice rolls).

## Endpoints

### Session Management

#### Create a Session
```
POST /api/session
Content-Type: application/json

{
  "sessionId": "game-001"
}

Response: 201 Created
Location: /api/session/game-001
```

#### Get Session Info
```
GET /api/session/{sessionId}

Response: 200 OK
{
  "id": "game-001",
  "createdAt": "2024-01-15T10:30:00Z",
  "playerCount": 2,
  "unitCount": 4,
  "pendingInputRequests": 1,
  "duration": "00:15:30"
}
```

#### Delete Session
```
DELETE /api/session/{sessionId}

Response: 204 No Content
```

---

### Player Management

#### Add Player to Session
```
POST /api/session/{sessionId}/players
Content-Type: application/json

{
  "playerId": "player-123",
  "playerName": "Alice"
}

Response: 201 Created
{
  "id": "player-123",
  "name": "Alice",
  "unitIds": []
}
```

#### Get Player Info
```
GET /api/session/{sessionId}/players/{playerId}

Response: 200 OK
{
  "id": "player-123",
  "name": "Alice",
  "unitCount": 2,
  "unitIds": [1, 3]
}
```

---

### Unit Management

#### Add Unit to Session
```
POST /api/session/{sessionId}/units?unitId=1
Content-Type: application/json

{
  "templateId": 5,
  "values": {
    "1": 10,
    "2": "warrior",
    "3": { "6": 2 }
  }
}

Response: 201 Created
```

#### Get Unit
```
GET /api/session/{sessionId}/units/{unitId}

Response: 200 OK
{
  "templateId": 5,
  "values": { ... }
}
```

#### Assign Unit to Player
```
POST /api/session/{sessionId}/assign-unit
Content-Type: application/json

{
  "unitId": 1,
  "playerId": "player-123"
}

Response: 200 OK
{
  "message": "Unit 1 assigned to player player-123"
}
```

---

### Input Request Management (Webhook Polling)

#### Get All Pending Requests in Session
**Clients can poll this endpoint to receive input requests**

```
GET /api/session/{sessionId}/pending-requests

Response: 200 OK
[
  {
    "id": "req-001",
    "unitId": 1,
    "requestType": "DiceRoll",
    "inputSchema": {
      "DiceNotation": "2d6+1d20",
      "Sides": [6, 20],
      "Counts": [2, 1]
    },
    "response": null,
    "isResolved": false,
    "createdAt": "2024-01-15T10:30:00Z",
    "resolvedAt": null,
    "elapsedTime": "00:00:05"
  }
]
```

#### Get Pending Requests for a Specific Unit
**A unit (player) polls this endpoint to check their own requests**

```
GET /api/session/{sessionId}/units/{unitId}/pending-requests

Response: 200 OK
[
  {
    "id": "req-001",
    "unitId": 1,
    "requestType": "DiceRoll",
    "inputSchema": { ... },
    "response": null,
    "isResolved": false,
    "createdAt": "2024-01-15T10:30:00Z",
    "resolvedAt": null,
    "elapsedTime": "00:00:05"
  }
]
```

#### Get Specific Request
```
GET /api/session/{sessionId}/requests/{requestId}

Response: 200 OK
{
  "id": "req-001",
  "unitId": 1,
  "requestType": "DiceRoll",
  "inputSchema": { ... },
  "response": null,
  "isResolved": false,
  "createdAt": "2024-01-15T10:30:00Z",
  "resolvedAt": null,
  "elapsedTime": "00:00:05"
}
```

---

### Dice Roll Operations

#### Request Dice Roll from Unit
**Server requests a unit to roll dice**

```
POST /api/session/{sessionId}/units/{unitId}/roll-dice
Content-Type: application/json

{
  "rolls": {
    "6": 2,
    "20": 1
  }
}

Response: 201 Created
{
  "requestId": "req-001",
  "unitId": 1,
  "diceSpec": {
    "6": 2,
    "20": 1
  },
  "createdAt": "2024-01-15T10:30:00Z"
}
```

#### Resolve Dice Roll Request
**Unit/Client responds with dice roll results**

```
POST /api/session/{sessionId}/requests/{requestId}/resolve-dice
Content-Type: application/json

{
  "rolls": {
    "6": [3, 5],
    "20": [18]
  }
}

Response: 200 OK
{
  "message": "Dice roll request 'req-001' resolved.",
  "response": {
    "6": [3, 5],
    "20": [18]
  }
}
```

---

### Generic Input Request Resolution

#### Resolve Any Input Request
```
POST /api/session/{sessionId}/requests/{requestId}/resolve
Content-Type: application/json

{
  "response": {
    "action": "attack",
    "target": "enemy-001"
  }
}

Response: 200 OK
{
  "message": "Input request 'req-001' resolved."
}
```

---

## Webhook Usage Pattern

### Server → Client (Polling)
1. Server creates a game session
2. Server adds players and units
3. Server calls `/requests/{requestId}/request-dice-roll` to request a dice roll from a unit
4. **Client polls** `/units/{unitId}/pending-requests` periodically
5. Client detects new request and displays it to the player
6. Player makes a decision (rolls dice)
7. Client calls `/requests/{requestId}/resolve-dice` with the response
8. Server receives the response and continues game logic

### Example Client Polling Loop (Pseudocode)
```javascript
async function pollForRequests(sessionId, unitId) {
  while (gameActive) {
    const requests = await fetch(`/api/session/${sessionId}/units/${unitId}/pending-requests`).then(r => r.json());

    for (const request of requests) {
      if (!request.isResolved) {
        if (request.requestType === 'DiceRoll') {
          const rolls = performDiceRoll(request.inputSchema);
          await fetch(`/api/session/${sessionId}/requests/${request.id}/resolve-dice`, {
            method: 'POST',
            body: JSON.stringify({ rolls })
          });
        }
      }
    }

    await sleep(1000); // Poll every second
  }
}
```

---

## Response Format for Dice Rolls

Dice roll responses must follow this JSON format:
```json
{
  "rolls": {
    "6": [3, 5],      // Array of rolled values for d6
    "20": [18],       // Array of rolled values for d20
    "4": [2, 3, 1]    // Array of rolled values for d4
  }
}
```

The key is the **number of sides** as a string, and the value is an **array of integers** representing each individual roll.

---

## Error Responses

All error responses follow this format:

```
400 Bad Request
{
  "error": "Description of what went wrong"
}

404 Not Found
{
  "error": "Session 'game-001' not found."
}

500 Internal Server Error
{
  "error": "An unexpected error occurred"
}
```

---

## Notes

- All timestamps are in UTC (ISO 8601 format)
- Session data is stored in memory and will be lost if the server restarts
- Input requests remain in "Pending" state until resolved
- Clients should implement exponential backoff for polling to reduce server load
- The `ElapsedTime` field shows how long the request has been pending
