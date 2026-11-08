# Changelog

## 1.1.0 (M59)
* Berry SDK 1.3.0. Persian + English UI with a runtime language switch (own Persian shaper).
* New: room list (join from inside the game), room code, direct join from a free room, create room, two lobby lists (Berry room / joined the game) with a details panel, info tabs (launcher, account, room, network, friends, lab).
* New: UDP / TCP / Auto transport; protocol version 2 (country and role are sent).
* Fixed: partly drawn cubes/capsules and invisible tracers (own shader, bullets, viewmodel), black screen of a guest who joined late (camera could start inside a cover block), two players with the same name (mock names are unique, the host numbers duplicates).
* New: "Create the full main scene" builds the arena, light, camera and spawn points as real scene objects.
* Unity 6 warnings removed (obsolete API calls, unused fields). No comments in scripts.

## 1.0.0 (M58)
* First release: solo / host / join, Berry LAN + virtual LAN, friends, ping, subscription, token, achievements, scores, presence.
