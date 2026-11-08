# Security of the Berry SDK — launcher, player and SDK

Honest summary: the SDK is designed so that a **game can only learn what its profile allows, can only do what the player confirms, and can never prove who a player is to a third party without Berry's signature.** Nothing here protects against malware that already runs as the same Windows user.

## 1. The player (what a game can never get)
| Never given to a game | How it is enforced |
|---|---|
| Berry login token, password, e-mail, phone, wallet | the launcher builds the context from a fixed list of fields (`frontend-vue/src/services/sdkLogic.js`); nothing else is copied |
| Real IP addresses | `address` / `hostAddress` are passed only if they are private addresses (10.x, 172.16-31.x, 192.168.x, 169.254.x) |
| A room's password or its hash | only `locked` (true/false) is given |
| Other players' profile details without `players` | the permission is checked by the launcher **and again by berry-core** |
Everything the game asks for that changes something (create / join / leave a room, read friends, invite a friend) opens a **dialog in the launcher**; closing it is "no". Invitations are asked every time. Rate limits: 20 messages per second per game, one room action per 2 s, at most 3 open questions.

## 2. The launcher
* The SDK port listens on `127.0.0.1` only; a request with an `Origin` header (a web page) is refused; a silent connection is closed after 5 s.
* One-time session token (256-bit random, per launch, constant-time compare, one connection at a time, dies with the game).
* The unauthenticated `probe` can only read launcher facts (open / version / language / signed in / connected / official / uptime). It never returns names, rooms or tokens, and the connection is closed after the answer.
* Permissions come from Berry's game profile (or from the manifest, validated: unknown permissions are refused), never from what the game says.
* All text from a game is treated as data: cut to length, control characters removed, never executed or inserted as HTML.
* `games.inspect` reads `berry-game.json` as text, with a size limit; it never runs the game to learn about it.

## 3. The SDK (inside the game)
* Connects only to `ws://127.0.0.1:<port>` (checked by `sdk/tests/csharp-lint.mjs`). The probe port from `launcher.json` must be 1024-65535.
* No outside JSON library, no process / assembly loading, no file writes in the runtime (also lint-checked).
* Strings from the launcher are cleaned with `BerryText.Clean` before the game uses them (no `<` `>` rich-text injection, no control characters, fixed length).
* Mock mode (editor / no launcher) never opens a network connection to Berry.

## 4. Trusting a player's identity in YOUR game
Local data (`Berry.User`) is **for display only**: a modified game or a fake launcher can say anything. If your game has a server (ranking, purchases), ask Berry for a signed token (`GetIdentityTokenAsync`, see SERVER-TOKENS.md) and verify it on your server. Never put a developer token in the game.

## 5. In a LAN match (what the sample game does)
The host decides everything (movement, hits, waves). A guest sends keys only; every packet is parsed with bounds, NaN is rejected, packets are limited in size (1200 bytes), and one player cannot take another's slot (a re-join with the same user id is accepted only from the same IP). Names are cleaned. This stops casual cheating; it does not stop a modified host.

## 5b. What SDK 1.4 adds (and what it refuses)
* Room-admin actions and `account.logout` are asked **every time**; the launcher runs them with the player's own rights, so a game cannot kick or close what the player may not. Usernames are checked (`[A-Za-z0-9_]{3,20}`), numbers have bounds.
* A game cannot log a player in with a password (it would see it). `OpenLoginAsync` / `OpenForgotPasswordAsync` only open the launcher / the website.
* `GetAccountAsync` never returns e-mail, phone or purchase history. Queries are read-only and limited to one per 400 ms per game; launcher actions to one per 2 s.

## 6. Known limits
* "Official launcher" = the launcher says it is connected to Berry's server. A modified launcher could lie; do not use it for anything of value.
* Malware running as the same Windows user can read the session file `%APPDATA%\Berry\sdk\<game>.json`. This is the normal limit of local software.
* TCP/UDP game traffic between players is not encrypted by the SDK (Berry's virtual network between players is). Do not send secrets in game packets.
