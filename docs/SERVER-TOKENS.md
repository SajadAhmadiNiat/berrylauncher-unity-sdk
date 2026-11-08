# Identity tokens for your game server

A client can lie ("I am Ali"). If your game has its own server (matchmaking, leaderboard, shop, anti-cheat), do not trust the name the client sends. Use a Berry identity token.

1. The game calls `await Berry.GetIdentityTokenAsync("my-server-name")` (permission **token** in `berry-game.json`). The launcher asks Berry's server, which signs the token with its **Ed25519** private key.
2. The game sends the token to your server.
3. Your server downloads Berry's **public** key once: `GET https://<berry-server>/api/public/sdk-key` -> `{alg:"Ed25519", key:<base64 32 bytes>, pem}` and checks the token.

Token = `base64url(payload) . base64url(signature)`, payload: `{v:1, iss:"berry", sub:<username>, dn, tier, game, aud, iat, exp, jti}`; valid 5 minutes.

Your server MUST check: signature, `iss == "berry"`, `exp` in the future, `game` equals your game id, `aud` equals YOUR server's name (so a token made for another server is useless here), and `jti` not seen before (one use).

Reference (Node): `verifySdkToken` in `server/src/sdksvc.js`, example `sdk/examples/verify-token.mjs`. Other languages: any Ed25519 library (libsodium, `cryptography` in Python, `System.Security` NSec in .NET).
Limits: 10 tokens per minute per player.
