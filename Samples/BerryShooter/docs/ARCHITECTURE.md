# Architecture

Scripts in `Assets/BerryShooter/Scripts` (assembly `BerryShooter.Runtime`, references `Berry.Sdk`):

| File | Job |
|---|---|
| `Bootstrap.cs` | the only component in the scene; starts Berry (with a mock context in the editor), then `GameApp`; pumps `BerryMainThread` |
| `Common.cs` | `Settings` (PlayerPrefs), `Mats` (materials from the game's own shader, cached), `Fonts`, `TextUtil` |
| `Tr.cs`, `TrFa.cs` | translation: `Tr.T("English text")`, `Tr.F("... {0}", x)`, `Tr.Set("fa")`; the Persian dictionary |
| `Persian.cs` | Persian text engine for uGUI: letter shaping (isolated/initial/medial/final forms), right-to-left reordering with numbers and brackets, rich-text tags kept, line breaking; `FaText` component on every label |
| `ArenaLayout.cs` | walls/cover positions and collision math (shared by view and simulation) |
| `World.cs` | **pure simulation** (no Unity objects): players, enemies, waves, hits. `Step` runs at 30 Hz on the host only. Duplicate names get a number |
| `NetProtocol.cs` | text messages, version 2 (`J` join, `I` input, `P` ping, `L` leave; host to guest `W R Q S E B M`) |
| `NetLinks.cs` | `ILink`: `UdpLink`, `TcpHostLink`, `TcpClientLink`, `MultiLink` (host: UDP + TCP on one port), `ClientLink` (guest: UDP then TCP fallback) |
| `GameSession.cs` | Solo / Host / Guest; guest prediction + soft reconciliation; waits for the first snapshot before showing the world (no black screen) |
| `Views.cs` | `ArenaFactory` (arena as objects), player / enemy views, bullets, tracers, sparks |
| `Sfx.cs` | sounds generated in code |
| `UiKit.cs` | uGUI helpers (`Ui.Btn`, `Ui.Field`, `Ui.Scroll`, `Ui.ListItem`...) |
| `GameApp*.cs` | flow and camera, menus / room list / lobby / dialogs, HUD / name tags / game over |
| `BerryHub.cs` | **all** Berry calls and the text of the information tabs in one place |
| `Resources/BSFlat.shader` | tiny lit/unlit shader that works in Built-in, URP and HDRP; loaded from `Resources` so it is always in the build |

Why host-authoritative: guests only send inputs; the host moves everyone, so a modified guest cannot give itself kills.
Honest limits: no encryption and no cheat-proof design beyond input clamping; packets are plain text.
Persian typing in input fields is not shaped (Unity limitation); displayed text is shaped.
