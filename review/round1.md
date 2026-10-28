I read through all 13 scripts (`Game.cs`, `Player.cs`, `Enemy.cs`, `Interact.cs`, `Heli.cs`, `Data.cs`, `BaseHub.cs`, `LevelGen.cs`, `UI.cs`, `UiKit.cs`, `Art.cs`, `FX.cs`, `Sfx.cs`) against the design doc and traced the state machine, loot/settle flow, and the procedural generator for the classes of bug you asked about. Here are the real ones, prioritized.

## 1. `Deploy()` has no state guard → re-entrant deploy during the fade-in
**`Game.cs:70-92`**
Every other state-transition entry point (`Die`, `Extract`, `BackToBase`, `OpenStation`) guards itself and flips `state` *synchronously* before any `yield`. `Deploy()` does neither: it consumes gear, sets `S.inRaid`, and starts `DeployCo`, but `state = St.Raid` (line 88) only happens **after** the fade-in loop reaches its first `yield return null` (line 81) — a real ~0.3s window where `state` is still `St.Base`. During that window `InputLocked` is false (no modal/loot/cine), so the player can still interact with base stations, including the map table, and call `Deploy()` again. A second call runs a second `DeployCo`, building a second `Level` (duplicate houses/heli/enemy/loot) while the first coroutine's objects are never tracked/cleaned up, and `S.stats.raids`/gear consumption double-fire.
**Fix:** add `if (state != St.Base) return;` as the first line of `Deploy()`, and move `state = St.Raid;` out of `DeployCo` into `Deploy()` itself (synchronously, right after the gear checks), so the state flips before `StartCoroutine` returns, exactly like `Die()`/`Extract()` do.

## 2. Procedural key can be spawned with no matching locked door
**`LevelGen.cs:94-107`** (degree check) and **`LevelGen.cs:45`** (key insertion)
The "locked leaf room" search (lines 102-107) computes node degree from `conn`, but `conn` already includes the "extra loop" edges added at line 100 — edges added *after* the spanning tree, each with 20% independent probability. For small grids (house 0 is always 2 rows × 2-3 cols) it's quite possible for an extra-loop edge to land exactly between the tree's two leaves, raising both to degree ≥2 and eliminating every degree-1 node. When that happens `lockedId` stays `-1`, no door is tagged `"locked"`, and no safe/wardrobe gets placed — but the key insertion at line 45 runs unconditionally (`keyable.Count > 0`), handing the player a useless key with the toast "某扇門在等你" and no door that ever existed.
**Fix:** move the locked-leaf search block (lines 102-107) to run immediately after the spanning-tree `while` loop and *before* the extra-loop `for` (line 100), so degree is computed on the tree alone — any tree with ≥3 nodes guarantees a leaf other than node 0.

## 3. Mid-raid app-relaunch recovery forgets to advance the seed
**`Game.cs:29-34`**
On a normal resolution (`Settle()`, `Game.cs:212`) the code always does `S.seedN++;` so the next raid gets a new layout/loot roll. The "you quit mid-raid, treated as death" recovery path in `Awake()` (lines 29-34) rolls gear insurance and increments `stats.deaths`, but never touches `seedN`. Since `Deploy()`'s seed is `SaveSys.S.seedN * 7919 + 17`, the very next raid after a crash/relaunch will regenerate the **identical** house layout, container contents, and key placement the player just abandoned.
**Fix:** add `SaveSys.S.seedN++;` next to `SaveSys.S.stats.deaths++;` on line 32.

## 4. Quest type `"theme"` never checks which theme was actually played
**`Game.cs:199`**
`CheckQuest`'s `case "theme": r = true; break;` ignores `q.theme` entirely — any successful extraction completes a "extract alive from 【民房社區】" quest, regardless of which map was run. It's masked today because only theme 0 is playable, but the design doc explicitly plans more themes (`mart`, `office`, `base`, `port`), at which point this quest type silently becomes a no-op "extract from anywhere" quest.
**Fix:** `r = level.theme != null && level.theme.id == Data.Themes[q.theme].id;` (requires passing/having access to the just-completed `level.theme`, which `Settle`/`CheckQuest` already run while `level` is still valid).

## 5. Every corpse loot container uses the same RNG seed
**`Game.cs:188`**
`lc.Init("corpse", new System.Random(level.seed + 99), ...)` seeds the corpse's roll from `level.seed + 99` with no per-enemy component. With one enemy this is invisible, but the moment a second enemy exists (the design doc's own "Risks" section flags enemy-count as a near-term change), every corpse in a given raid would roll **identical** loot, because the seed doesn't vary by enemy identity/position.
**Fix:** include something enemy-specific in the seed, e.g. `new System.Random(level.seed + 99 + e.GetInstanceID())` or an incrementing kill counter.

## 6. `LootAll()` keeps hammering a full bag instead of stopping
**`Game.cs:171, 174`**
`Pickup()` sets `ui.autoTake = false` the first time the bag is full, but `LootAll()`'s loop (`for (...) if (openC.slots[i].state == 2) TakeSlot(i);`) doesn't check that flag per-iteration — it keeps calling `TakeSlot`→`Pickup` for every remaining revealed slot in the same batch, firing the "背包滿了" toast and `error_004` SFX once per leftover item.
**Fix:** break out of the loop as soon as `Pickup` fails, e.g. check `if (!ui.autoTake) break;` right after the `TakeSlot(i)` call in `LootAll()`.

## 7. Death screen's "-$lost" double-counts items the secure slot actually saved
**`Game.cs:218, 228-230`**
`Die()` captures `long lost = BagValue;` (full bag value) before `Settle(false)` runs, then `DeathCo` prints `"-" + Fmt(lost)` as the headline loss **and separately** lists `info.kept` ("安全箱保住：…") without subtracting those items' value from the displayed loss. The player is shown they lost the full bag value even though the secure-slot upgrade they paid for demonstrably kept part of it.
**Fix:** display `lost - info.kept.Sum(i => i.v)` as the loss figure (or explicitly label it as "potential loss before insurance").

## 8. Enemy's patrol index isn't synced to its randomized spawn point
**`LevelGen.cs:66-68`**
```csharp
en.patrol.AddRange(new[] { ... });
en.transform.position = Pick(en.patrol) + Vector3.up * 0.1f;
```
The enemy is placed at a *random* patrol waypoint, but `wp` (the "next waypoint" index in `Enemy`) stays at its default `0`. If the randomly chosen spawn point isn't index 0, the enemy's very first patrol leg can be a long walk back toward a point it didn't just leave, instead of continuing the loop from where it spawned.
**Fix:** when picking the spawn point, also set `en.wp` to that index (requires exposing `wp` or an `int idx = Random.Range(...)`; `en.transform.position = en.patrol[idx] + ...; en.wp = idx;`).
to frame hitches as scenes grow.
Fix: cache the `Collider` reference in `LootContainer.Init`/`GroundLoot.Init`, and cache `Player.I.cam` once instead of calling `Camera.main` every frame.

**10. No mid-raid feedback when a quest condition is actually met**
`Game.cs:196-200` (`CheckQuest`)
`CheckQuest` only runs inside `Settle()`, i.e. at raid end — a player who hits a "kill 1 enemy" or "value ≥ X" quest mid-raid gets zero acknowledgement until the end screen. A quick toast/SFX the moment the condition becomes true (e.g. checked right after `OnEnemyKilled`/`Pickup`) would make quest completion feel like a real moment instead of a line of end-screen text.

**11. Player's `CharacterController` keeps simulating gravity during the extraction cinematic**
`Player.cs:83-84`
`vy`/`cc.Move` run unconditionally in `Update()` even when `controlEnabled` is false and the camera has been detached for the heli cinematic (`Game.cs:245`). It's harmless today (no gameplay effect since the camera no longer follows the player), but it's dead work every frame of a ~9s cutscene and a trap for later changes — gate the physics step behind `canAct` (or at least `alive && inRaid`) the same way look/move input already is.
