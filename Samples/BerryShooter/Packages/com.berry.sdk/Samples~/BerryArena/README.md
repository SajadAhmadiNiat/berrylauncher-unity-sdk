# Berry Arena (sample)

A complete mini game that uses every part of the Berry SDK. Two scripts, no assets.

1. Import the **Berry SDK** package, then in Package Manager > Berry SDK > Samples > **Import** "Berry Arena".
2. Open an empty scene, create an empty GameObject, add the component **Arena Bootstrap**, press Play.
   Without the launcher the SDK uses fake data ("Mock"), so you can try everything in the editor.
3. Build for Windows (`BerryArena.exe`). Use the `berry-game.json` next to this file (copy it beside your Assets folder).
4. (Only for games added in the developer dashboard) fill `Game Code` and `Game Number` on the Berry Behaviour component, or in `ArenaBootstrap` set `b.gameCode` / `b.gameNumber`.
5. Start it from the Berry launcher: the player name, room and host address come from Berry; the host listens on UDP 7777 and guests send to the host's Berry address.
