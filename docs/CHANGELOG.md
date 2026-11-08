# Berry SDK — changelog

(Rule from CLAUDE.md: every time the SDK changes, this file gets a new entry in the same delivery.)

## 1.4.0 — M60 (game info, connection per player, profile / follow / achievements / club / tournament / account / settings queries, room-admin actions, launcher actions)

**What is new**
* **Game facts** (`Berry.Game`, `BerryGame`, always present): `Id, Name, Path, InstalledVersion, Source (berry | manual | detected | custom), DownloadedFromBerry, Defined, Free, Port, Protocol (udp | tcp | both), StartedUtc, RunningSeconds`. Shortcuts: `Berry.IsFreeConnection`, `Berry.InstalledVersion`, `Berry.IsDownloadedFromBerry`.
* **How each player is connected**: `BerryPlayer.Connection` (`Method, Phase, Relay, RttMs`), `IsSelf`, `ConnectedUtc`, `IsRelayed`. This is the same number the launcher shows as "link 83 ms via relay".
* **Room extras**: `IsOpen, InGame, MemberCount, Voice (Active, Open, Count, Mode, Transport, MeIn), Relay, RecommendedTransport` and the `Is*` forms `IsLocked, IsFree, IsAdminOnly, IsInGame, IsRelayed, IsHost, IsFull, IsPrivate, RequiresReady, IsVoiceActive`.
* **Launcher extras**: `Theme, VoiceJoined, VoiceChannel` and `IsOpen, IsLoggedIn, IsConnected, IsOfficial, IsDark, IsInVoice`.
* **Profile extras**: `HasProfile, Followers, SinceUtc, IsStaff, IsAdmin (= launcher staff), IsOnline, IsPremium`.
* **Both a property and an `Is*`** for almost every boolean: players (`IsReady, IsMuted, IsLaggy, IsInVoice`), connection, voice, game, club, tournament, account, settings. The old field names still work.
* **Static shortcuts** on `Berry`: `Game, IsLoggedIn, IsInRoom, IsFreeConnection, IsLauncherAdmin, HasProfile, IsRoomOpen, IsVoiceActive, IsInLauncherVoice, RoomMemberCount, InstalledVersion, IsDownloadedFromBerry`.
* **Queries** (permission `profile`; one query per 400 ms per game; read-only): `GetProfileAsync(username?)` (bio, followers, since, achievements count, club), `CheckFollowAsync(a, b)` / `DoesFollowAsync(a, b)` ("does X follow Y"), `HasAchievementAsync(id, username?)`, `ListAchievementsAsync()`, `GetClubAsync(handle?)`, `ListTournamentsAsync()`, `GetTournamentAsync(id)`, `GetAccountAsync()` (coins, plan, expiry, staff, country; **no email, no phone**), `GetSettingsAsync()` (language, theme, notifications, sounds, auto-join, beta).
* **Room-admin actions** (permission `lan`; the player is asked **every time**): `KickAsync, MuteAsync, SetRoomAdminAsync, SetRoomPasswordAsync, SetRoomStatusAsync, SetMaxPingAsync, SetRequireReadyAsync, SetLaunchModeAsync, CloseRoomAsync`. The launcher uses the player's own rights in that room: a game cannot do what the player may not do.
* **Launcher actions**: `ShowLauncherAsync, OpenLauncherPageAsync(page), OpenLoginAsync, OpenForgotPasswordAsync` (no permission, one per 2 s) and `LogoutAsync` (permission `identity`, the player is asked every time). Pages: `home, rooms, games, friends, profile, settings, shop, downloads, tournaments, ranking, club, mag, arcade, free`.
* **Not exposed on purpose**: logging in with a password from a game (the game would see the password). `OpenLoginAsync` and `OpenForgotPasswordAsync` open the launcher / the website instead. Email, phone and wallet history are never given.
* Server: new `profile.followCheck {a, b}` request (returns `follows`, `followedBy`).
* Node SDK 1.4.0: `getProfile, checkFollow, doesFollow, hasAchievement, listAchievements, getClub, listTournaments, getTournament, getAccount, getSettings, kick, mute, setRoomAdmin, setRoomPassword, setRoomStatus, setMaxPing, setRequireReady, setLaunchMode, closeRoom, showLauncher, openLauncherPage, openLogin, openForgotPassword, logout`.

**Compatibility.** Protocol stays `1`. Old launchers answer the new requests with `unknown_name` / `unsupported`.

**Honest limits.** The C# could not be compiled here. `sdk/tests/csharp-lint.mjs`, the core test (`core/tests/sdk.mjs`) and the Node client tests pass; run the PlayMode tests in Unity once. Room-admin actions depend on the player's rights in the room.

## 1.3.0 — M59 (launcher state, room list, join by id, rich data, transport helpers)

**What is new**
* **Launcher state, even without a session.** The core writes `launcher.json` (`{port, pid, started, core, protocol}`) in the Berry config folder (`<config>/sdk/launcher.json`) and answers an unauthenticated `probe` message with `probe.ok` (open, version, build, language, loggedIn, connected, official, uptime, hasSession, activeSessions). `Berry.Launcher` and `Berry.LinkState` (`Connected`, `Mock`, `LauncherOpenNoSession`, `LauncherClosed`) tell a game the truth: "launcher is open but this game has no SDK session" is no longer shown as "launcher not found". `BerryProbe.RunAsync()` is the same probe without a session. The probe reveals no player data.
* **Games started outside the launcher's game list now get a session.** The core reads `berry-game.json` beside the game's exe (`games.inspect`); a free-room game that carries a manifest is upgraded to an SDK game (permissions are still validated by Berry; unknown permissions are refused).
* **Room list and join by id.** `await Berry.ListRoomsAsync(allGames)` returns `BerryRoomInfo` (id, name, host, players, maxPlayers, locked, free, status, pingMs, gameId, gameName). `await Berry.JoinRoomAsync(roomId, password)` joins one. Both need permission `lan`; joining asks the player. Public rooms only; the room's password hash is never sent (only `locked`).
* **Much more context** (each field still needs its permission): identity (`berryCode`, `userId`), profile (`country`, `plan`, `planUntil/purpleUntil/grayUntil`, `staff`, `gender`, `bio`), room (`visibility`, `locked`, `free`, `adminOnly`, `status`, `kind`, `gameId`, `gameName`, `owner`, `hostName`, `playerCount`, `description`, `tags`, `maxPing`, `requireReady`, `launchMode`, `createdAt`, `openedAt`, `network`, `link`, `myRole`, `myAdminLevel`), players (`role`, `adminLevel`, `adminPerms`, `ready`, `muted`, `laggy`, `inVoice`, `country`, `berryCode`, `userId`, `joinedAt`, `serverPingMs`, `stats` = latency now/avg/min/max, jitter, loss, stable) and launcher (`version`, `build`, `lang`, `loggedIn`, `connected`, `official`, `startedAt`, `uptimeSec`). C# classes: `BerryLauncher`, `BerryPlayerStats`, extended `BerryRoom`, `BerryPlayer`, `BerryProfile` (`SubscriptionEndUtc`, `DaysLeft`), `BerryUser`.
* **Transport helpers.** `BerryTransport` (`Auto`, `Udp`, `Tcp`), `BerryUtil.ChooseTransport(preferred, context)` (chooses TCP when Berry reports a relay link), `TransportName`. Berry itself forwards both UDP and TCP (`"protocol": "both"` in `berry-game.json`).
* **Text helpers.** `BerryUtil.ExpiryText`, `SubscriptionText`, `RoleText`, `RoomTypeText`, `LinkStateText`, all with `lang` ("fa" / "en"); `ErrorText` has Persian text and the new codes `not_found`, `not_connected`, `timeout`.
* **Security.** Strings from the launcher are cleaned (`BerryText.Clean`: control characters, `<` and `>` removed, length limited) before a game can print them. The probe port is read from `launcher.json` (max 4 KB) and checked (1024-65535) before connecting; the probe only talks to 127.0.0.1 and has a 1.5 s timeout.
* **Unity 6.** No obsolete API calls (`FindObjectOfType` is guarded by `UNITY_2023_1_OR_NEWER`). Package works in Unity 2021.3 and Unity 6 (6000.0).
* Node SDK `berry-sdk.mjs` 1.3.0: `listRooms()`, `joinRoom(id, password)`, `probeLauncher()`.
* No comments inside scripts any more; everything is explained in `docs/sdk`.

**Compatibility.** The protocol version stays `1`. `SdkVersion` is `1.3.0`. Older launchers answer the new requests with `unsupported` / `unknown_name`; the game must handle that (`BerryUtil.ErrorText` does).

**What you need to change.** Nothing. To show a room list use `Berry.ListRoomsAsync`; to show launcher state use `Berry.LinkState`.

**Honest limits.** The C# could not be compiled here (no Unity/.NET). `sdk/tests/csharp-lint.mjs` and the JS client tests pass; run the PlayMode tests in Unity once. The "official launcher" flag only says the launcher is connected to Berry's server; it is not proof against a modified launcher. The room password hash is never given to games. Tournament and ranking data are not part of the SDK yet.

## 1.2.1 — M58 (helper class `BerryUtil`, Berry icon, sample game)

**What is new**
* **`BerryUtil`** (static, `Runtime/BerryUtil.cs`): small helpers so every game does not rewrite them.
  * Subscription: `TierRank(tier)`, `IsSubscriber(tier)`, `TierColorHex(tier)`, `BadgeColorHex(badge)`, `NameWithClub(displayName, profile)`.
  * Ping: `PingQuality(ms)` (enum `BerryPingQuality`), `PingText(ms)`, `FindPeer(network, username)`, `PeerPing(network, username)`, `WorstPing(network)`, `PhaseText(phase)`.
  * Addresses / room: `IsPrivateAddress(ip)`, `HostAddressToConnect(context)`, `IsInRoom(context)`, `ValidateRoom(name, maxPlayers, password)`.
  * Errors / async: `ErrorText(code, lang)`, `LauncherSupportsLan()`, `await WaitForNetworkAsync(timeoutMs, pollMs)`, `await TryAsync(call, lang)` (returns null on success or a readable message).
* The Berry icon is the package icon (`Editor/BerryIconSetter.cs`, `Documentation~/berry-icon*.png`).
* New complete sample project `samples/BerryShooter` (a PC shooter built from cubes and capsules that uses LAN, virtual LAN, launcher connection, profile, subscription, ping, friends, achievements, scores).
* Tests: `Util_TiersBadgesAndPing`, `Util_AddressesAndRoomChecks`.

**Compatibility.** Protocol and `SdkVersion` stay `1.2.0`; additive only, nothing removed.

**What you need to change.** Nothing. Update the package to 1.2.1 to use `BerryUtil`.

**Not enforced / honest limits.** The C# could not be compiled here (no Unity/.NET); lint passes. Run the PlayMode tests once.

## 1.2.0 — M54 (LAN, virtual LAN, network state, friends, profile)

**What is new**
* **LAN / virtual LAN from inside the game** (permission `lan`): `Berry.HostLanAsync(name, maxPlayers, password, publicRoom)`, `JoinLanAsync(code, password)`, `LeaveLanAsync()`, `GetLanStatusAsync()`. They create / join / leave the same rooms as the launcher itself, so the game rides on Berry's virtual network exactly like the games that are started from the launcher's room list. **The player is asked** before the launcher acts.
* **Network state** (permission `network`): `Berry.Network` and `GetNetworkAsync()` give the state of the virtual LAN, my virtual address, the peers' virtual addresses, connection phase (`direct` / `relay` ...) and ping.
* **Friends** (permission `friends`): `GetFriendsAsync()` (username, display name, online, playing — nothing else) and `InviteFriendAsync(username)`.
* **Profile** (permission `profile`): `Berry.Profile` = badge (`none` / `gray` / `purple` / `gold`), club tag, language, online. Still never: e-mail, wallet, country, real IP, login token.
* Context: `sdk.version` ("1.2.0"), `profile`, `network`. `contextKey` includes them, so a change of network state reaches the running game as a `ctx` push.
* Node SDK: `hostLan`, `joinLan`, `leaveLan`, `lanStatus`, `getNetwork`, `getFriends`, `inviteFriend`, `request(name, data)`.
* Core: new request names `lan.status|host|join|leave` (*lan*), `network.get` (*network*), `friends.list|invite` (*friends*); four new profile permissions accepted (`profile` JSON, `games.addCustom`, schema, website list, developer dashboard).
* Launcher: consent dialog (`SdkConsentDialog`), per-game rate limit, addresses restricted to private ranges for **all** address fields (the old `players[].address` and `room.hostAddress` too).

**Compatibility.** Protocol version stays `1`; additive only. A 1.1 game keeps working. A 1.2 game on an older launcher gets `unknown_name` / `unsupported` for the new calls (check `Berry.Context.SdkVersion` or catch the exception). The C# facade methods throw `BerryException` with the launcher's code (`denied` is the player's "no").

**What you need to change**
1. Update the package to 1.2.0.
2. Add the permissions you need to `berry-game.json` (`"network"`, `"lan"`, `"friends"`, `"profile"`) and, for developer-platform games, tick them in the dashboard.
3. Call the new methods. Always handle `denied`.

**Not enforced / honest limits**
* The C# part could not be compiled on the build machine (no Unity/.NET); the lint (`sdk/tests/csharp-lint.mjs`) passes and the protocol is tested end to end with the JavaScript client against the real core (`sdk/tests/node-client.mjs`, `core/tests/sdk.mjs`). The launcher's consent dialog and the room calls are tested as logic (`frontend-vue/tests/sdkLogic.test.mjs`), but the dialog itself was not seen on a screen.
* A game can still open its own sockets inside the virtual network; Berry cannot stop a game from talking to its peers — the SDK only gives it safe facts and a way to create / join Berry rooms.
* `pingMs` is `-1` until the core reports a round trip for that peer.

## 1.1.0 — M34 (developer platform)

**What is new**
* **Game code + game number.** Every game you add in the developer dashboard on the Berry website gets a *game code* (`BG-ABCD-EFGH`) and a 6-digit *game number*. Put them in `BerryBehaviour` (fields `gameCode`, `gameNumber`) or in `BerryOptions.GameCode / GameNumber`. The SDK sends them in `hello`; berry-core compares them with the values the launcher has for the game and refuses a wrong hello with `bad_game_code`. (They are *inside* your game build, so they are not a secret key — they only stop a different program from using your game's session.)
* **Developer token (`bdt_…`).** A separate secret for *your tools* (build server, the `berry-dev` command), shown once on the dashboard, stored only as a hash on the website. It is **never** used inside a game.
* **`berry-dev` command line** (`sdk/node/berry-dev.mjs`): publish a version of your game (`release … --mandatory --publish`), list releases, see room/player statistics and sales.
* **Mandatory updates and extra files** are handled by the launcher (no SDK code needed): a release marked *mandatory* becomes the new minimum version; the launcher shows «نیاز به آپدیت» on the Downloads card and an update dialog when the player starts the game. A release can list extra files (`--dep "url|sha256|zip|Data"`) that the launcher downloads into the game's folder after the game itself.
* Core: `games.addCustom` accepts `sdk: [permissions]` (developer games are registered as `custom-<gameId>` with the permissions you chose); `sdk.rejected` event; the `dev` block of the context is kept by the core and never given to the game.
* Node SDK: `berry.connect(session, { gameCode, gameNumber })`.

**Compatibility.** Games built with SDK 1.0 keep working: the code + number are only required for games of the developer platform (the launcher marks them). Protocol version stays `1`.

**What you need to change in a game of the developer platform**
1. Update the package to 1.1.0.
2. In `BerryBehaviour` fill *Game Code* and *Game Number* (from the dashboard).
3. Done. Build, upload the zip (anywhere with an https address), then `berry-dev release …` or the dashboard form.

**Not enforced / honest limits**
* The download address of a *paid* game is not protected: if the link leaks, others can download without paying (use a private/signed link).
* The C# part could not be compiled on the build machine; the protocol is tested with the JavaScript client against the real core (`sdk/tests/node-client.mjs`, `core/tests/sdk.mjs`).

## 1.0.0 — M33
First release: identity, room, players, presence, achievements, scores, signed identity tokens, Unity package + sample, Node client.
