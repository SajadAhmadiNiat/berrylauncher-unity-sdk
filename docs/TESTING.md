# Testing a game with Berry

## A. In the Unity editor (no launcher)
Press Play. The SDK runs in **mock mode** with fake data (`Berry.IsMock`, `Berry.LinkState == Mock`). Use two editors / one editor + one build to test the network part. Give each copy a different name (the sample uses `player-<random id>`).

## B. Test as a real Berry game (free room, no admin approval)
1. Build the game (Windows x64). Put **`berry-game.json` in the same folder as the exe** (the sample's build menu does this).
2. In the launcher: add the game as a custom game (or use a free room and choose the exe). The core reads the manifest beside the exe and gives the game an **SDK session** with the permissions in it.
3. Open a **free room** (host) and let a friend join the same room; start the game from the room. The launcher passes `-host` / `-connect <host>:<port>`.
4. Inside the game the "Berry" panels must show: launcher open, signed in, room name, players.
If the game says the launcher is open but there is no session: the manifest is missing/invalid, the id differs, or the game was started by double-click. Fix and start again **from the launcher**.

## C. Test the room list
Host a public room with game X from the launcher or from the game's "Create a Berry room". On another account: game > Room list > Join.

## D. Checklist
* Menu > Launcher tab: "Launcher open: yes", "This game has an SDK session: yes".
* Two PCs: names differ, both appear in the lobby lists, ping shown.
* Settings > Network protocol: try UDP, TCP and Auto.
* Settings > Language: switch Persian/English at runtime.

## E. Automated tests (Berry team)
`node core/tests/sdk.mjs`, `node sdk/tests/csharp-lint.mjs`, `node sdk/tests/node-client.mjs`, `node server/tests/sdksvc.test.mjs`, `node samples/BerryShooter/tools/lint.mjs`. The C# itself was not compiled on the build machine.
