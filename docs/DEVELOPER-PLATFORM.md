# Berry developer platform (M34)

For game makers: publish your game on Berry, update it, see how it is played and get paid.

## 1. Sign up
On the website, **ساخت حساب** → tick «بازی‌ساز هستم». (An existing account can join from **حساب من ← داشبورد توسعه‌دهنده**.) You get a **developer token** (`bdt_…`): it is shown once. Copy it to a safe place; if you lose it, make a new one on the dashboard (the old one stops working).

## 2. Add your game
Dashboard → **ثبت بازی تازه**: game id (lowercase, 3–30 characters), name, SDK permissions. You get the **game code** and **game number**. The game waits for the Berry owner to approve it (you are notified); until then players cannot see it. You can set a price in Toman (0 = free).

## 3. Connect the game (Unity)
Import `com.berry.sdk` 1.1.0, add `BerryBehaviour`, fill `gameId`, `gameCode`, `gameNumber`. See README.md.

## 4. Publish a version
Upload your zip anywhere with an `https://` address, then either use the dashboard form, or:

```
set BERRY_DEV_TOKEN=bdt_...            (Windows)   /   export BERRY_DEV_TOKEN=bdt_...   (Linux, macOS)
node sdk/node/berry-dev.mjs sha256 MyGame.zip
node sdk/node/berry-dev.mjs release my-game --version 1.1.0 --url https://example.com/MyGame-1.1.0.zip --sha256 <hash> --exe MyGame.exe --notes "رفع باگ" --mandatory --publish
```
* `--mandatory`: players on older versions must update before the game starts (shown in the launcher as «نیاز به آپدیت» and a dialog at start). Without it the old version still works and the card says a new version exists.
* `--dep "https://…/data.zip|<sha256>|zip|Data"`: extra files downloaded after the game, into `<game folder>/Data`. `--dep "https://…/lib.dll|<sha256>|file|bin|lib.dll"` for one file.
* Versions are `1.2.3` and must be higher than the published one. A release can be saved as a draft (leave out `--publish`) and published later: `berry-dev publish my-game <releaseId>`; `revoke` takes one back.

## 5. Numbers
* **Rooms and players**: how many rooms were made with your game, who played, when, for how long (dashboard, paged). Kept 400 days.
* **Sales**: every purchase is split **80 % you / 20 % Berry** (the owner can change your share for later sales). The dashboard shows totals, per game, per payment method, per day and a paged list (buyer names are masked). Payouts are made by the owner by hand and recorded; your balance is *your share − payouts*. Amounts are before payment-gateway fees.

## Security notes
* The developer token is only for tools. Never put it in the game. Only the hash is stored.
* Game code and number are inside your build: they are not secret; they just bind a session to your game.
* All download addresses must be `https://`; the launcher checks the SHA-256 and unpacks only inside the game's own folder.

## M61: the developer panel

- **Panel:** `/dev` on the website (sections: overview, games and versions, SDK setup, finance, statistics, promotion and accelerator, account and contract, knowledge base). Full guide: `docs/DEVELOPER_PANEL.fa.md`.
- **SDK setup values:** `GET /api/dev/games/<id>/sdk` (tool token) or the panel's "SDK setup" page returns `berry-game.json`, the Unity fields (Game Id / Game Code / Game Number) and the C# line. CLI: `berry-dev sdk <gameId>`.
- **VirusTotal:** `berry-dev release ... --vt <sha256-or-md5> [--vt-url <link>]`. Required / optional / off is the owner's setting (`vt_mode`).
- **Upload:** the panel uploads the build to the Berry server (`dev/upload`); the release then carries `file_id`.
