# Berry SDK

Connect your game to the **Berry (تمشک)** launcher: player name, room, players, LAN / virtual-LAN, friends, presence, achievements, scores and signed identity tokens.

## Install in Unity (2021.3+)
Window > Package Manager > **+** > *Add package from git URL*:

```
https://github.com/YOUR-NAME/berry-sdk.git?path=/Packages/com.berry.sdk#v1.4.0
```

Then menu **Berry > Add Berry to scene**, fill *Game Id / Game Code / Game Number* from your panel at the Berry website (`/dev`), and put `berry-game.json` next to your exe.

## Other parts
- `docs/` - full documentation (protocol, rooms, data, security, publishing, developer platform).
- `node/berry-sdk.mjs` - SDK for Node games. `node/berry-dev.mjs` - command line tool for developers (upload versions, VirusTotal hash, stats).
- `schema/`, `examples/`, `tools/` - `berry-game.json` schema, example and validator.

Licence: see LICENSE.
