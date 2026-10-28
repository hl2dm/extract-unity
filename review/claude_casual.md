Reviewed the five provided files (`Game.cs`, `Pet.cs`, `Bus.cs`, `Player.cs`, `LevelGen.cs`). I don't have `UI.cs`, `Data.cs`, `LootContainer.cs`, `Door.cs`, or `SaveSys.cs`, so bugs confined to those are out of scope. Here's what actually holds up as serious:

1. **Pet AI isn't paused while the player is input-locked (looting/modals) — breaks the core fairness contract.**
   `Pet.cs` `Update()`: `bool active = p != null && p.inRaid && Game.I.RaidActive;` and the `Sees(p)`/Chase/Catch logic never check `Game.I.InputLocked`. Meanwhile `Player.cs` `Update()` gates all movement on `canAct = controlEnabled && !Locked`. Result: while a loot wheel is spinning (`ui.LootOpen`), a cat/dog can still Notice → Chase → catch the player, steal an item, and rack up `caught` toward the crying penalty, with the player completely unable to move or escape.
   **Fix:** in `Pet.Update()` add `&& !Game.I.InputLocked` to the `active`/`sees` gate, and force Chase→Patrol (like the existing `!active` branch) when `InputLocked` is true.

2. **`Fail()` reports confiscated loot that was actually kept — contradicts the "secure/pocket" upgrade it's selling.**
   `Game.cs` `Fail()`: `long lost = BagValue; int n = bag.Count(b => !b.key); var info = Settle(false);` — `lost`/`n` are computed from the *entire* bag before `Settle(false)` pulls the top‑N items into `info.kept` (which are never removed from `bag`). The end screen then shows "`n` 樣東西被沒收了 (value `lost`)" *and* separately lists those same top items as "口袋裡偷偷藏住" — double-counting the kept items as both lost and saved, which makes the `secure` upgrade's payoff look broken/contradictory to the player.
   **Fix:** compute `lost`/`n` from `bag.Where(b=>!b.key).Except(info.kept)` after calling `Settle`, or subtract `info.kept`'s count/value before building the message.

3. **Quest "theme" check can index out of range if `q.theme` doesn't match a live theme.**
   `Game.cs` `CheckQuest`: `case "theme": r = level != null && level.theme == Data.Themes[q.theme]; break;` — no bounds check on `q.theme`. If quest generation (`Data.MakeQuest`, not shown) ever picks an index tied to a theme that's later removed/reordered, or any off-by-one in that table, this throws `IndexOutOfRangeException` and kills the extraction flow.
   **Fix:** clamp/validate `q.theme` against `Data.Themes.Length` before indexing, or store the theme id string instead of an index.

4. **Latent crash on pet naming via negative modulo once `seedN` grows large.**
   `LevelGen.cs` `SpawnPets`: `DogNames[(i + L.seed) % DogNames.Length]` (same for cats). `L.seed = SaveSys.S.seedN * 7919 + 17`, and `seedN` increments every single raid (`S.seedN++` in `Settle`). Once the multiplication overflows `int` and goes negative, `(i + L.seed) % N` can be negative in C#, producing a negative array index → `IndexOutOfRangeException`. Low likelihood in practice (needs ~hundreds of thousands of raids) but it is a real crash path tied to persistent save state that only gets worse over time, not reset by anything.
   **Fix:** use `((i + L.seed) % N + N) % N`, or reduce the seed into a small positive range before use.

5. **Minor but real: Chase give-up message can fire for a pet the player can't perceive as "chasing" during a lock.** Combined with #1, since Chase/Notice timers (`stT`) keep advancing even while `InputLocked`, a pet can go Notice→Chase→give-up entirely off-screen while the player is stuck in a modal, then immediately re-notice once unlocked — not a crash, but confirms AI state is fully decoupled from the player's actual ability to respond. Same fix as #1 resolves this too.

Most impactful to fix first: **#1** (directly breaks game fairness / can feel like cheating against the player) and **#2** (actively lies to the player about a paid upgrade's effect).
