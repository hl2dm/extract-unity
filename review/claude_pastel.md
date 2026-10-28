Reviewed the shaders, `Art.cs` (PaletteFor/Toon/Toonify/SetupEnvironment), `UiKit.cs`, `Pet.cs`'s facing fix, and `Game.cs`'s bonus payout. Here are the issues that actually hold up as bugs (not style nits):

**Pet.cs:22-30 (`Fit()` facing correction) — non-deterministic yaw estimate**
The coroutine measures front/back bone world positions 2 frames after the model is instantiated to compute a correction yaw. But by then `Update()` (Pet.cs:127-128) has already started `Play("idle"/"sit"/...)`, which `CrossFade`s the Animation component — so the bones are mid-blend into a moving pose, not the bind pose. The resulting `yaw` (and whether it crosses the `>30f` threshold at Pet.cs:29) depends on animation timing/frame-rate, so the same model can get "corrected" on one spawn and left uncorrected on another. The `Debug.Log` "ok"/"corrected" verdict at Pet.cs:30 is therefore not trustworthy.

**Pet.cs:59-60 — comment/code mismatch**
Comment: `// models face +Z? if the longest horizontal axis is X, turn it.` Code: unconditionally does `m.transform.localRotation = Quaternion.Euler(0, 180, 0);`. No axis-length check exists — any model actually facing ±X rather than ∓Z is never corrected here, and gets left entirely to the unreliable heuristic above.

**Art.cs:351-352 (`SetupEnvironment`, Casual sky branch) — level sky params partially ignored**
Only `_Top` and `_Ground` are set on the `PastelSky` material; `_Mid`, `_Horizon`, and `_Cloud` (3 of the shader's 5 color properties, including the horizon band — the most visually dominant one) retain the shader's hardcoded defaults. Every level's casual skybox ends up with the same peach horizon/cloud tint regardless of the `sky`/`ground` arguments passed in, defeating per-level theming.

**UiKit.cs:72 (`PaperOverlay`) — stale UV rect on resize**
`ri.uvRect = new Rect(0, 0, Screen.width / 256f, Screen.height / 256f);` is computed once at creation and never recalculated. On WebGL (or any runtime window/canvas resize), the paper-grain tiling becomes stretched/incorrect since nothing re-reads `Screen.width/height` afterward.

**Game.cs bonus payout (`Settle`/`Extract`/`Fail`, Game.cs:225-283):** reviewed closely — the arithmetic (`100 + bv0/5 + clean bonus`), the `caughtRun==0` gating, and the bag/stash/overflow-sale interplay are internally consistent; I didn't find a correctness bug here.
