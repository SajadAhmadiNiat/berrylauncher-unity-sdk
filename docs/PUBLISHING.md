# Publishing a game on Berry

1. Build the Windows version of your game (`MyGame.exe`).
2. Write `berry-game.json` (Unity: menu **Berry > Create berry-game.json**). Schema: `sdk/schema/berry-game.schema.json`. Example: `sdk/examples/berry-game.json`.
   * `connect`: how players can play together: `lan` (game finds others by LAN broadcast - works over Berry's virtual LAN), `vlan`, `ip` (host's address typed/launched by argument), `wireguard`, `direct`, `dedicated`...
   * `hostArgs` / `joinArgs`: command-line arguments Berry adds when the player hosts or joins. `joinArgs` may use `{host}` and `{port}`. Only plain text.
   * `sdk.permissions`: ask only for what you need (identity, room, players, presence, achievements, scores, token, and since SDK 1.2: network, lan, friends, profile).
3. Check it: `node sdk/tools/manifest-to-profile.mjs berry-game.json` (prints the profile or the list of problems).
3b. **Developer platform (SDK 1.1):** instead of sending files to the Berry team you can add the game yourself in the dashboard and publish versions with `berry-dev` — see DEVELOPER-PLATFORM.md. The owner still approves a new game before players see it.
4. Send the manifest and the game to the Berry team. They convert it to a profile (`shared/game-profiles/<id>.json`), add the game (name, picture, guide, videos) on the website panel, and your game appears in the launcher with the SDK badge.
5. Test in the launcher: host a room, join from a second PC, watch that name, room and host address arrive.

Rules: the executable name is fixed; Berry never runs anything from the manifest except your exe with the listed plain arguments.
