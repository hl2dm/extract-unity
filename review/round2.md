## Fix verification (all 5 confirmed correct)

| # | Fix | Status |
|---|-----|--------|
| 1 | `Game.cs:70,73,79` — `deploying` bool guard added to `Deploy()` | ✅ Correct — blocks re-entrant `Deploy()` calls during the fade window |
| 2 | `LevelGen.cs:99-107` — locked-leaf search moved before the extra-loop `for` (`LevelGen.cs:107`) | ✅ Correct — degree now computed on the spanning tree alone; house 0 always has `cols*rows>=4` so a leaf is always found |
| 3 | `Game.cs:32` — `SaveSys.S.seedN++;` added to crash-recovery path | ✅ Correct |
| 6 | `Game.cs:176` — `LootAll()` now does `TakeSlot(i); if (!ui.autoTake) break;` | ✅ Correct for that loop |
| 8 | `LevelGen.cs:66-68` — `en.SetWaypoint(wi)` syncs `wp` to the randomized spawn index | ✅ Correct |

## Remaining real bugs

**1. `Game.cs:161` — the #6 toast/SFX-spam bug still exists in the *other* auto-take loop**
The round-1 fix patched `LootAll()`'s loop but `StepLoot()`'s idle auto-take loop has the identical unguarded pattern:
```csharp
if (cur == null) { if (ui.autoTake) for (int i = 0; i < openC.slots.Count; i++) if (openC.slots[i].state == 2) TakeSlot(i); return; }
```
This runs every frame while a container is open and nothing is currently spinning. The moment the bag fills, `Pickup` sets `ui.autoTake = false`, but the loop already captured `ui.autoTake` once at entry and keeps calling `TakeSlot` on every remaining revealed slot in that same frame — firing "背包滿了" toast + `error_004` once per leftover item, same as the bug already fixed elsewhere.
**Fix:** `if (cur == null) { if (ui.autoTake) for (int i = 0; i < openC.slots.Count; i++) { if (openC.slots[i].state == 2) { TakeSlot(i); if (!ui.autoTake) break; } } return; }`

**2. `Game.cs:278` — `OpenStation` race during the Deploy() fade can strand the Modal open into the raid**
`Deploy()`'s new `deploying` guard stops a second `Deploy()` call, but `OpenStation` only checks `state != St.Base`, not `deploying`. `state` doesn't flip to `St.Raid` until `DeployCo` (`Game.cs:90`), ~0.3s after `Deploy()` starts, and `Deploy()`'s own `ui.CloseModal()` only fires once at the start. If the player interacts with *any* base station (map table, stash, shop, workbench) again during that ~0.3s window, `OpenStation` reopens the Modal — and nothing ever closes it when the state flips to `St.Raid`. Since `InputLocked` includes `ui.ModalOpen` regardless of `state`, the player spawns into the raid with movement/shooting locked and a stale base-menu covering the screen. It's recoverable (Escape still closes any open modal unconditionally in `Update()`), but it's a genuine flow-breaking regression surfaced by the round-1 fix.
**Fix:** `Game.cs:278` — `if (state != St.Base || deploying) return;`

No other crash/null-ref/softlock-class issues found in `Enemy.cs`, `Player.cs`, `Interact.cs`, `Heli.cs`, `BaseHub.cs`, `Data.cs`, `UI.cs`, `UiKit.cs`, `Art.cs`, `FX.cs`, `Sfx.cs`. (Round-1's remaining unfixed items #4/5/7/9/10/11 are cosmetic/logic polish, not crash/softlock class, so excluded per your scope.)
