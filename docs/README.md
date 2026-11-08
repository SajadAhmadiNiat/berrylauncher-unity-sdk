# Berry SDK for Unity

Make your game appear in the Berry launcher and get the player's name, room, players, host address - safely.

## Install
* Unity 2021.3 or newer (including Unity 6, 6000.0), Windows build. **Window > Package Manager > + > Add package from disk...** and choose `sdk/unity/Packages/com.berry.sdk/package.json` (or copy the folder into your project's `Packages/`).
* Import the sample: Package Manager > Berry SDK > Samples > **Berry Arena**.

## Use (5 minutes)
1. Menu **Berry > Add Berry to scene** (adds `BerryBehaviour`, set `gameId` to the id in your manifest).
2. Menu **Berry > Create berry-game.json** and edit it (see PUBLISHING.md).
3. In code:
```csharp
using BerrySdk;

void Start() {
    Berry.Ready += () => {
        Debug.Log("Hello " + Berry.User.DisplayName);              // permission: identity
        if (Berry.Room.Role == BerryRole.Guest)
            Connect(Berry.Room.HostAddress, 7777);                  // permission: room
        foreach (var p in Berry.Players) Debug.Log(p.DisplayName);  // permission: players
        Berry.SetPresence("In the lobby");                          // permission: presence
    };
    Berry.ContextChanged += ctx => RefreshPlayerList(ctx.Players);  // someone joined/left
}
```
4. **Developer platform (SDK 1.1):** if you added the game in the developer dashboard on the website, fill `Game Code` and `Game Number` on `BerryBehaviour` (or `BerryOptions.GameCode/GameNumber`). Details: DEVELOPER-PLATFORM.md, changes: CHANGELOG.md.
5. Press Play in the editor: without the launcher the SDK runs in **mock mode** with fake data (`Berry.IsMock`).

## API
| | |
|---|---|
| `Berry.IsReady / IsMock / IsConnected / IsHost` | state |
| `Berry.User` `Berry.Room` `Berry.Players` `Berry.Context` | data from the launcher (null/empty without the permission) |
| `Berry.Ready` `ContextChanged` `Pushed` `Disconnected` | events (main thread) |
| `Berry.SetPresence(text)` | shown next to the player in the launcher |
| `Berry.UnlockAchievement(id, title)` / `SubmitScore(board, value)` | recorded by the launcher (this PC) |
| `Berry.LeaveRoom()` / `SendCustom("custom.x", data)` | tell the launcher |
| `await Berry.GetIdentityTokenAsync(audience)` | signed token for your own server (SERVER-TOKENS.md) |
| `Berry.Profile` `Berry.Network` | **1.2** safe facts about the player (badge, club) and the virtual LAN state (permissions `profile`, `network`) |
| `await Berry.HostLanAsync(name, maxPlayers, password, publicRoom)` / `JoinLanAsync(code, password)` / `LeaveLanAsync()` / `GetLanStatusAsync()` | **1.2** create / join / leave a LAN (virtual LAN) room from inside the game; the player is asked first (permission `lan`) |
| `await Berry.GetNetworkAsync()` | **1.2** virtual address, peers and pings (permission `network`) |
| `await Berry.GetFriendsAsync()` / `InviteFriendAsync(username)` | **1.2** the player's friends (name + online) and an invitation to the room they host (permission `friends`) |
| `Berry.Launcher` `Berry.LinkState` | **1.3** launcher facts without a session: open, signed in, connected to Berry's server, official flag, version, uptime; `Connected / Mock / LauncherOpenNoSession / LauncherClosed` |
| `await Berry.ListRoomsAsync(allGames)` / `JoinRoomAsync(roomId, password)` | **1.3** public room list and join by id (permission `lan`; the player is asked to join) |
| `BerryRoom`, `BerryPlayer`, `BerryProfile`, `BerryUser` | **1.3** many more fields: admin level, nation, subscription end, room type / link / owner, per-player latency stats (see DATA-REFERENCE.md) |
| `BerryTransport`, `BerryUtil.ChooseTransport` | **1.3** UDP / TCP / Auto choice (TRANSPORT.md) |
| `BerryUtil.*` | **1.2.1** helpers: `TierRank/IsSubscriber/TierColorHex/BadgeColorHex/NameWithClub` (subscription), `PingQuality/PingText/FindPeer/PeerPing/WorstPing/PhaseText` (ping), `IsPrivateAddress/HostAddressToConnect/IsInRoom/ValidateRoom` (addresses, room), `ErrorText/LauncherSupportsLan/WaitForNetworkAsync/TryAsync` (errors, async) |

`BerrySession.Discover()`, `BerryClient` are available if you want to build on the raw connection (no Unity types inside).

## Full example project
`samples/BerryShooter` is a complete Unity project (UI written in code, Persian + English) that uses almost everything above. Start with its `README.md`.

## More documents
`PROTOCOL.md` (wire format) - `SECURITY.md` (player, launcher, SDK) - `TRANSPORT.md` (UDP / TCP) - `ROOMS.md` (room list, direct join, two lists) - `TESTING.md` (test with Berry) - `DATA-REFERENCE.md` (every field and its permission) - `CHANGELOG.md`.

Scripts of the SDK contain **no comments**; everything is explained in these documents.

## Honest notes
* The C# package could not be compiled on Berry's build machine (no Unity/.NET there). The **protocol** is tested end to end against the real core with the JavaScript client (`node sdk/tests/node-client.mjs`) and the C# code is checked by `sdk/tests/csharp-lint.mjs` and a careful review; run **Test Runner > PlayMode** (`Berry SDK Tests`) in Unity once and tell us about any compile message.
* Achievements/scores are kept by the launcher on the player's PC in this version; server-side leaderboards for SDK games are not part of version 1.
* Mock mode is for development only; ship builds with `mockWhenStandalone` off if you do not want the game to run outside Berry.
* Other engines: the protocol is small (PROTOCOL.md); `sdk/node/berry-sdk.mjs` is a 100-line reference.

## Berry team: keeping the SDK up to date
Every time a Berry feature is added or changed (new connection method, new context field, new permission, protocol change) update: `core/src/sdk`, `sdk/unity/...`, `sdk/node/berry-sdk.mjs`, `docs/sdk/*`, bump `SdkVersion` + `package.json` version, and run `sdk/tests/*` and `core/tests/sdk.mjs`. (Rule is in CLAUDE.md.)
