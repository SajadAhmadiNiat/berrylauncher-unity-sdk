# Which Berry SDK feature is used where (SDK 1.3.0)

| SDK feature | Permission | File / place in the game |
|---|---|---|
| `Berry.InitAsync`, `Berry.Shutdown`, `BerryMainThread` | - | `Bootstrap.cs` (start / quit) |
| `Berry.Ready`, `ContextChanged`, `Pushed`, `Disconnected` | - | `GameApp.cs`, `GameApp.Menus.cs` (refresh lobby, leave on disconnect) |
| `Berry.IsReady / IsMock / IsHost` | - | `BerryHub`, `GameApp.TryAutoStart` (auto start is skipped in mock mode) |
| `Berry.User` | identity | `BerryHub.MyProfile()` -> player name |
| `Berry.Room`, `Berry.Players` | room, players | auto join when started as guest/host from the launcher; lobby list |
| `Berry.Profile` | profile | badge + club tag next to names (`Views.cs` name tags, scoreboard) |
| `Berry.Network`, `GetNetworkAsync` | network | Network tab, HUD ping, scoreboard ping (`RefreshNetwork` every few seconds) |
| `HostLanAsync / JoinLanAsync / LeaveLanAsync / GetLanStatusAsync` | lan | "Host a Berry LAN room", "Join through Berry", Berry Lab |
| `GetFriendsAsync / InviteFriendAsync` | friends | Friends tab, lobby "Invite friends" |
| `SetPresence` | presence | "In menu", "Wave 3" ... |
| `UnlockAchievement` | achievements | first_blood, ten_kills, forty_kills, wave_5, wave_10, team_player |
| `SubmitScore` | scores | boards `kills`, `best_wave` at the end of a match |
| `GetIdentityTokenAsync(audience)` | token | Berry Lab > "Get identity token" (audience `berry-shooter-server`) |
| `LeaveRoom`, `SendCustom` | - | Berry Lab buttons |
| `BerryUtil` tiers/badges | - | colours of names |
| `BerryUtil` ping helpers | - | `PingQuality/PingText/WorstPing/PeerPing/PhaseText` |
| `BerryUtil.HostAddressToConnect / IsInRoom` | - | `GameApp.TryAutoStart` |
| `BerryUtil.ValidateRoom / ErrorText / TryAsync` | - | host dialog, every launcher question |

Permissions are listed in `berry-game.json` (`sdk.permissions`). The launcher decides what the game really gets.

## Added in SDK 1.3 (M59)
| SDK feature | Permission | Place in the game |
|---|---|---|
| `Berry.ListRoomsAsync`, `JoinRoomAsync` | lan | Room list screen (`GameApp.Menus.cs`, `BerryHub.RefreshRooms / JoinRoomById`) |
| `Berry.Launcher`, `Berry.LinkState`, `BerryProbe` | - | Launcher tab, main menu status line |
| `BerryLauncher.Official / LoggedIn / Connected / UptimeSec` | - | Launcher tab |
| `Berry.User.BerryCode / UserId`, `BerryProfile.Country / Plan / SubscriptionEndUtc / Gender / Bio / Staff` | identity, profile | Account tab, lobby details panel (nation) |
| `BerryRoom.*` (type, link, code, game, owner, admins, status) | room | Room tab, lobby header |
| `BerryPlayer.*` (role, admin level, ready, muted, voice, stats) | players | lobby details panel, Room tab |
| `BerryTransport`, `BerryUtil.ChooseTransport` | - | `BerryHub.ShouldPreferTcp`, `NetLinks.ClientLink` |
| `BerryUtil.ExpiryText / SubscriptionText / RoleText / RoomTypeText / LinkStateText` | - | info tabs |
