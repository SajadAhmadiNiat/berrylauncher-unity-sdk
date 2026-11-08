# Berry SDK protocol (version 1)

The game talks to the launcher through **berry-core**, on a local WebSocket: `ws://127.0.0.1:<BERRY_SDK_PORT>/` (this PC only).
Reference implementations: `sdk/unity/...` (C#) and `sdk/node/berry-sdk.mjs` (JavaScript). Tested against the real core by
`core/tests/sdk.mjs` and `sdk/tests/node-client.mjs`.

## How a game gets its session
The launcher starts the game through berry-core. For a game whose profile has an `sdk` block, the core creates a **one-time session** and gives the game:

| Environment variable | Meaning |
|---|---|
| `BERRY_SDK_PORT` | the local port |
| `BERRY_SDK_TOKEN` | 256-bit random secret for this launch only |
| `BERRY_SDK_GAME` | the game id |
| `BERRY_SDK_PROTOCOL` | `1` |

Programs started elevated (Windows hides the environment then) can read the same data from `%APPDATA%\Berry\sdk\<gameId>.json` (`{port, token, game, protocol}`; readable by the current user only). The file is deleted when the session ends.

## Envelope
Every message is one JSON text frame (max 64 KB): `{ "v": 1, "type": "...", "id": 7, "payload": { ... } }`.

## Game -> core
| type | payload | answer |
|---|---|---|
| `hello` | `{token, sdk, game, gameCode?, gameNumber?}` (first message, within 5 s). **SDK 1.1:** a game of the developer platform must also send its `gameCode` (like `BG-ABCD-EFGH`) and `gameNumber` (6 digits) | `hello.ok {game, protocol, permissions, context, core}` or `error bad_token` / `bad_game_code` (connection closed) |
| `ping` | `{}` | `pong` |
| `ctx.get` | `{}` | `ctx` (the context) |
| `event` | `{name, data}` | `event.ok` or `error` |
| `req` | `{name, data}` | later `res {ok, data}` or `res {ok:false, error}` with the **same id** |

Event names: `presence.set {text}` (permission *presence*), `achievement.unlock {id,title}` (*achievements*), `score.submit {board,value}` (*scores*), `room.leave` (*room*), `custom.<anything>` (no permission).
Request names: `token {audience}` (*token*), `custom.<anything>`, and since **SDK 1.2** (protocol version stays `1`; all of them are answered later with `res`):

| request | permission | data | answer `data` |
|---|---|---|---|
| `lan.status` | *lan* | `{}` | `{inRoom, id?, code?, name?, maxPlayers?, host?, network?}` (no question to the player) |
| `lan.host` | *lan* | `{name, maxPlayers?, password?, visibility?: "public"\|"private"}` | the same room object. The launcher **asks the player** first |
| `lan.join` | *lan* | `{code, password?}` | the room object, or `{requested:true, roomName}` when the host must let the player in. **Asked** |
| `lan.leave` | *lan* | `{}` | `{inRoom:false}`. **Asked** |
| `network.get` | *network* | `{}` | `{state, address, nat, peers:[{username, address, phase, pingMs}]}` |
| `friends.list` | *friends* | `{}` | `{friends:[{username, displayName, online, playing}]}` (max 100). **Asked once per launch** |
| `friends.invite` | *friends* | `{username}` | `{invited:true}` (needs to be the host of a room, friend online). **Asked every time** |

Error codes of these requests (`res {ok:false, error}`): `denied` (the player said no), `already_in_room`, `not_in_room`, `bad_request`, `rate_limited` (one room action per 2 s per game), `not_logged_in`, `unknown_game`, `unsupported` (older launcher), plus the server's room errors (`room_full`, `room_not_found`, `wrong_password`, `invalid_name` ...). The core answers `forbidden` before the launcher is even asked when the permission is missing.

Errors (`error` message, `payload.code`): `bad_token`, `bad_game_code` (SDK 1.1: the code or number does not match the game; the session stays usable for the right build), `unauthorized`, `already_connected`, `forbidden`, `unknown_name`, `too_big` (data > 4 KB), `rate_limited` (20 messages/s, bursts of 30), `busy`, `session_ended`, `unknown_type`, `bad_json`, `unsupported_version`.

## Core -> game
`ctx` (id null): the context changed. `push {name, data}`: a named event from the launcher (e.g. `player.joined`). `bye {reason}`: the session ended. `res`: answer to a `req`.

## The context
```json
{ "sdk": {"protocol":1}, "game": {"id":"berry-arena","name":"Berry Arena"}, "launcher": {"version":"1","lang":"fa"},
  "permissions": ["identity","room","players"],
  "identity": {"username":"ali","displayName":"Ali","tier":"gold"},
  "room": {"id":"...","name":"...","code":"ABC234","maxPlayers":4,"role":"host|guest|none","mode":"lan|vlan|wireguard|ip|direct|dedicated","hostAddress":"10.77.0.2"},
  "players": [{"username":"ali","displayName":"Ali","host":true,"tier":"gold","address":"10.77.0.2"}],
  "profile": {"badge":"none|gray|purple|gold","club":"ABC","lang":"fa","online":true},
  "network": {"state":"off|starting|running|error","address":"10.77.0.2","nat":"cone","peers":[{"username":"sara","address":"10.77.0.3","phase":"direct","pingMs":12}]} }
```
`sdk.version` is `"1.2.0"` in the context of a launcher that knows SDK 1.2 (the field is missing in older launchers). `profile` needs the permission *profile*, `network` needs *network*.
Fields exist only when the game's profile has the matching permission. **Never** included: Berry login token, e-mail, wallet, password hashes (also not a room's password hash: only `locked`), real IP addresses (`address` / `hostAddress` are addresses inside Berry's network).

## Security rules (what is enforced, and where)
* Listens on 127.0.0.1 only; a connection with an `Origin` header (a web page) is refused; a connection that is silent for 5 s is closed.
* The token is random per launch, compared in constant time, usable by one connection at a time, and dies when the game closes (`sdk.end`) or restarts.
* The launcher builds the context; the core checks the **permission of every request again**; the permissions come from the game's profile, which Berry controls, not from the game.
* The launcher treats everything from the game as untrusted text: it is cut short, validated and never executed.
* Identity for a game's own server is a **signed token** (see SERVER-TOKENS.md); the local data above is for display and convenience, not for trust.

### What SDK 1.2 adds to the security model
* **The player decides.** `lan.host`, `lan.join`, `lan.leave` and `friends.list` show a dialog in the launcher (once per launch of the game for each group); `friends.invite` is asked every time, so a game cannot spam friends. Closing the dialog is a "no".
* **Only private addresses.** The launcher passes an address to the game only when it is a private one (Berry's virtual network is `10.77.0.x`); a public address is dropped. A game never learns a player's real IP.
* **Rate limits.** The core allows 20 messages/s; the launcher lets one game start one room action per 2 s and keeps at most 3 questions open per game.
* **Permissions come from Berry's profile / the website's list**, never from the game. A game without `lan` gets `forbidden` from the core itself.
* **No new capabilities outside the launcher's own rooms:** `lan.host` uses the same room rules (name 3-60 chars, 2-64 players, optional password 4-64 chars, restrictions and bans) as the room dialog of the launcher.

## Launcher side (UI <-> core)
`games.launch {gameId, sdk:{context}}`, `sdk.context`, `sdk.push`, `sdk.respond`, `sdk.status`, `sdk.end`; events to the UI: `sdk.connected`, `sdk.event`, `sdk.request`, `sdk.rejected {gameId, reason}` (SDK 1.1: a hello with a wrong game code / number).
SDK 1.1 also: `games.addCustom {gameId:"custom-<id>", name, path, args?, sdk?:[permissions]}` registers a game of the developer platform with its permissions, and the launcher puts `dev: {code, number}` into the `context` of `games.launch`/`sdk.context`; the core keeps it, checks the game's hello against it and never sends it to the game.


## SDK 1.3 additions (protocol version stays 1)

### Finding the launcher without a session
The core writes `<Berry config>/sdk/launcher.json`: `{"port":<n>,"pid":<n>,"started":<ms>,"core":"...","protocol":1}`. A program that has no session (the game was not started with the one-time token) can read it and send **one** unauthenticated message to `ws://127.0.0.1:<port>/`:

```json
{ "v": 1, "type": "probe", "id": 1, "payload": { "game": "my-game-id" } }
```
Answer (the connection is closed after it): `probe.ok` with `{open:true, core, protocol, version, build, lang, loggedIn, connected, official, uptimeSec, hasSession, activeSessions}`. No player data, no token. `hasSession` is true when a session for that game id exists. Everything a normal connection does (`hello`, requests) still needs the token.

### Launcher state push
The launcher gives the core its state (`sdk.launcher`: version, build, language, logged in, connected to Berry's server, official flag, started at). The core adds the uptime. `connected` + `official` are what the launcher knows about itself; treat them as a claim, not proof.

### New requests (permission `lan`)
| request | data | answer `data` |
|---|---|---|
| `rooms.list` | `{all?: true}` | `{rooms:[{id, name, host, players, maxPlayers, locked, free, status, pingMs, gameId, gameName}]}` — public rooms of this game (all games when `all` is true or the game is a developer-platform game); max 50 |
| `lan.join` | `{roomId}` (new) or `{code}` | the room object / `{requested:true}`. `roomId` must match `[A-Za-z0-9_-]{1,64}`. The player is asked |

### Context fields added in 1.3 (see CHANGELOG for the full list)
`launcher {version, build, lang, open, loggedIn, connected, official, startedAt, uptimeSec}` (always present); `identity.berryCode/userId`; `profile.country/plan/planUntil/purpleUntil/grayUntil/staff/gender/bio`; `room.visibility/locked/free/adminOnly/status/kind/gameId/gameName/owner/hostName/playerCount/description/tags/maxPing/requireReady/launchMode/createdAt/openedAt/network/link/myRole/myAdminLevel`; `players[].role/adminLevel/adminPerms/ready/muted/laggy/inVoice/country/berryCode/userId/variant/joinedAt/serverPingMs/stats{nowMs,avgMs,minMs,maxMs,jitterMs,lossPct,stable}`.
Every string is cut to a fixed length by the launcher and cleaned again by the SDK. `country` is a two-letter code and only comes with the permission `profile` (own profile) or `players` (other players).

### Limits
Messages are 64 KB at most (the context stays below 48 KB: at most 48 players, 64 peers, 8 tags). Requests: 20 messages per second per game.

## SDK 1.4 additions (protocol version stays 1)
New `req` names. Permission `profile`: `profile.get {username?}`, `follow.check {a?, b}`, `achievements.has {id, username?}`, `achievements.list`, `club.get {handle?}`, `tournament.list`, `tournament.get {id}`, `account.get`, `settings.get`. Permission `lan` (the player is asked every time): `room.kick {username}`, `room.mute {username, muted}`, `room.setAdmin {username, level 0-3}`, `room.setPassword {password}`, `room.setStatus {status: waiting | in_game}`, `room.setMaxPing {ms}`, `room.setRequireReady {on}`, `room.setLaunchMode {mode: auto | menu}`, `room.close`. No permission: `launcher.show`, `launcher.open {page}`, `account.openLogin`, `account.openForgot`. Permission `identity` (asked every time): `account.logout`.
Context additions: `game {path, installedVersion, source, downloadedFromBerry, defined, free, port, startedAt, protocol}`, `launcher.theme`, `launcher.voice {joined, channel}`, `profile.hasProfile/followers/since`, `room.isOpen/inGame/memberCount/voice{active,open,count,mode,transport,meIn}/relay/recommendedTransport`, `players[].isSelf/connectedAt/connection{method,phase,relay,rttMs}`.
