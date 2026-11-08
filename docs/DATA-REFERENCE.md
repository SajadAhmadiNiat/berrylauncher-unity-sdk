# What a game can read (SDK 1.4)

| C# | Permission | Meaning |
|---|---|---|
| `Berry.Launcher.Open / LoggedIn / Connected / Official / Version / Build / StartedUtc / UptimeSec` | none | launcher facts (open = the launcher process is running, also without a game session) |
| `Berry.LinkState` | none | `Connected`, `Mock`, `LauncherOpenNoSession`, `LauncherClosed` |
| `Berry.User.Username / DisplayName / Tier / BerryCode / UserId` | identity | unique identity of the player |
| `Berry.Profile.Badge / Club / Country / Plan / SubscriptionEndUtc / DaysLeft / Staff / Gender / Bio / Online / Language` | profile | the tick, club, nation, subscription and expiry |
| `Berry.Room.Id / Name / Code / Link / GameId / GameName` | room | room identity and link |
| `Berry.Room.Visibility / IsPublic / Locked / Free / Status / Kind / Mode / IsNetwork` | room | room type (private/public, password yes/no, free room, private network) |
| `Berry.Room.HostName / Owner / MyRole / MyAdminLevel / Role` | room | who is host / admin, and what I am |
| `Berry.Room.PlayerCount / MaxPlayers / MaxPing / RequireReady / LaunchMode / CreatedUtc / OpenedUtc / Tags / Description` | room | room settings |
| `Berry.Players[i].…` | players | `Username, DisplayName, IsHost, Role, AdminLevel, AdminPerms, Tier, Country, BerryCode, UserId, Address, Ready, Muted, Laggy, InVoice, JoinedUtc, ServerPingMs, Stats (NowMs, AvgMs, MinMs, MaxMs, JitterMs, LossPercent, Stable)` |
| `Berry.Network.State / Address / Nat / Peers[i].PingMs / Phase` | network | the virtual LAN and the ping to each peer |
| `await Berry.ListRoomsAsync()` | lan | the room list |
| `Berry.Game.Id / Name / Path / InstalledVersion / Source / DownloadedFromBerry / Defined / Free / Port / Protocol / StartedUtc` | none | game facts (defined game or free connection, from Berry or not, port, UDP/TCP) |
| `Berry.Players[i].IsSelf / Connection.Method / Phase / Relay / RttMs / ConnectedUtc` | players | how each player is connected and since when |
| `Berry.Room.IsOpen / InGame / MemberCount / Voice.* / Relay / RecommendedTransport` | room | room open/closed, voice chat running, members, relay |
| `Berry.Launcher.Theme / VoiceJoined / VoiceChannel` | none | theme and whether the player is in the launcher voice chat |
| `Berry.Profile.HasProfile / Followers / SinceUtc / IsStaff` | profile | has a public profile, followers, member since, launcher admin |
| `GetProfileAsync / CheckFollowAsync / HasAchievementAsync / ListAchievementsAsync / GetClubAsync / ListTournamentsAsync / GetTournamentAsync / GetAccountAsync / GetSettingsAsync` | profile | read-only queries |
| `KickAsync / MuteAsync / SetRoomAdminAsync / SetRoomPasswordAsync / SetRoomStatusAsync / SetMaxPingAsync / SetRequireReadyAsync / SetLaunchModeAsync / CloseRoomAsync` | lan | room-admin actions, the player is asked every time |
| `ShowLauncherAsync / OpenLauncherPageAsync / OpenLoginAsync / OpenForgotPasswordAsync / LogoutAsync` | none (logout: identity) | launcher actions |

**Not available (yet):** a room's password hash (never), global rankings (the launcher keeps scores per PC only), e-mail, phone, passwords, anything about money except the coin balance in `GetAccountAsync`. If a field is empty the player's launcher is older or the permission is missing — always check for `null` / empty.
