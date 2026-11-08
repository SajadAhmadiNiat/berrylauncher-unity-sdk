# Rooms from inside a game: direct join, room list, two player lists

Permissions needed: `room`, `players` (data) and `lan` (create / join / list).

## 1. Direct join (the game was started inside a room)
Berry starts the game with `-host` or `-connect <host>:<port>` and fills `Berry.Room`. This works for rooms from the launcher's list **and for free rooms**, as long as the game has an SDK session (see TESTING.md: put `berry-game.json` beside the exe).
```csharp
if (BerryUtil.IsInRoom(Berry.Context)) {
    string host = BerryUtil.HostAddressToConnect(Berry.Context);
    if (host != null) Connect(host, 7777); else if (Berry.IsHost) StartHosting();
}
```

## 2. The room list
```csharp
List<BerryRoomInfo> rooms = await Berry.ListRoomsAsync(allGames: false);
foreach (var r in rooms) Add(r.Name, r.Host, r.Players + "/" + r.MaxPlayers, r.PingMs, r.Locked);
await Berry.JoinRoomAsync(room.Id, password);
```
Public rooms only, max 50, refresh every ~4 s. `JoinRoomAsync` asks the player in the launcher. If the host must accept, `Requested` is true and `Berry.ContextChanged` brings the host address later. A room list entry never contains a password or its hash (`Locked` only).

## 3. Create / join by code
`Berry.HostLanAsync(name, max, password, publicRoom)` and `Berry.JoinLanAsync(code, password)`. Limits: name 3-60, players 2-64, password 4-64 (`BerryUtil.ValidateRoom` checks before asking the player).

## 4. The two lists in a lobby
* **In the Berry room**: `Berry.Players` (name, role/admin level, tier, country, ready, muted, voice, server ping, latency stats, join time). Updated by `Berry.ContextChanged`.
* **Joined the game**: players that really connected to the match (your own protocol). In the sample: `session.World.Players`.
Click a player to see details (`BerryHub.PlayerInfo`). Ping for a player: `BerryUtil.PeerPing(Berry.Network, username)`, or `player.Stats.NowMs` from the launcher.
