# Choosing UDP or TCP, and staying in step with Berry

Berry's virtual LAN forwards **both UDP and TCP**. Put `"protocol": "both"` in `berry-game.json` if your game can speak both; `"udp"` or `"tcp"` if only one.

## What the SDK gives you
```csharp
BerryTransport t = BerryUtil.ChooseTransport(BerryTransport.Auto, Berry.Context);
string name = BerryUtil.TransportName(t);
```
`ChooseTransport` returns your preference when it is `Udp` or `Tcp`. For `Auto` it returns `Tcp` if Berry reports a relay link to any peer (relays are slow and drop UDP bursts) and `Udp` otherwise. `Berry.Network.Peers[i].Phase` is `direct` or `relay`; `Berry.Context.SdkVersion` is the launcher's SDK level.

## What the sample game does (`NetLinks.cs`)
| Setting | Host | Guest |
|---|---|---|
| Auto | listens on UDP **and** TCP on the same port | tries UDP; after 2.4 s without a welcome it switches to TCP (starts with TCP when Berry says "relay") |
| UDP | UDP only | UDP only |
| TCP | TCP only | TCP only |
The guest locks to the path that got the welcome and closes the other. If the same user re-joins over the other protocol from the same IP, the host moves them to the new path without creating a second player.
TCP messages are one line each (`\n` ends a message), max 1200 bytes; a longer line closes that connection.
The setting is in Settings > Network protocol and in `PlayerPrefs` (`bs.proto`).

## Staying in sync with Berry
* Always connect to `Berry.Room.HostAddress` (the host's Berry address) and the port from `berry-game.json` (`gamePort`). Do not use public IPs.
* Listen to `Berry.ContextChanged`: it fires when the room, the players or the network change (also `Berry.Network` ping updates every 2 s if you call `GetNetworkAsync`).
* A game started by Berry gets `-host` or `-connect {host}:{port}`; also check `Berry.Room.Role`.
* `Berry.Players` is the **launcher's** list (everyone in the room); the game's own lobby list is separate (who joined the match). Show both; they differ while people are still starting the game.
