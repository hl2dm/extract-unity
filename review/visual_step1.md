## What Improved
- **Base/bunker**: Floor swapped to a worn-concrete PBR texture (visible normal detail, dirt variation); ceiling got a diamond-plate metal material instead of flat gray. Real texture read at floor level now.
- **Street**: Skybox upgraded from a flat solid-blue gradient to a proper procedural sky with volumetric clouds — biggest single realism win in the set.
- **Interior/exterior/loot**: Essentially no change pass-to-pass — same flat wall materials, same UI, same lighting.

## Top 8 Remaining Weak Spots (ranked by impact)

1. **Unlit-looking flat materials everywhere** (house walls, fridge, cabinets, lockers, couch, bunker walls/door). No normal/roughness maps, uniform albedo. → Add a tri-planar/detail-normal layer to the URP Lit shader on every architectural material; set Smoothness 0.1–0.3 for stucco/drywall, add a subtle noise-driven Normal Map strength ~0.3–0.5.

2. **No real-time shadows/contact shadows.** Characters and props cast no visible ground shadow even in bright daylight. → Enable Main Light shadows in URP asset (Hard/Soft Shadows, Cascade count 4), turn on "Soft Shadows" per-light, enable Screen Space Shadows + Contact Shadows via a Renderer Feature.

3. **Missing SSAO.** Corners (bunker, kitchen) show no occlusion darkening. → Add Screen Space Ambient Occlusion Renderer Feature, Intensity ~0.6–1.0, Radius 0.3–0.6, set to "Multiply" blend.

4. **No post-processing stack.** Flat sRGB "raw" look, no grading/contrast. → Add a Global Volume: ACES Tonemapping, Color Adjustments (contrast +10, saturation -5), subtle Bloom (threshold 1.1, intensity 0.3), Vignette (~0.25).

5. **Flat, uniform grass plane.** No blade geometry, no texture variation, looks like a green floor. → Replace with a tessellated/terrain-layer grass texture (albedo + normal + AO splat), or add GPU-instanced grass detail mesh via Terrain Details; add a dirt/wear blend layer under props and paths.

6. **Low-res tiling brick/stucco textures** (bunker brick, house exterior) look painted-on at close range. → Increase texture resolution to 2K+, add a secondary detail-normal tile at higher frequency, enable anisotropic filtering.

7. **No atmospheric depth cue outdoors.** Despite the new sky, the street scene has zero fog/haze, making distant houses look pasted onto the sky. → Add URP Fog (Exponential Squared, density ~0.002–0.01, color-matched to horizon) for depth falloff.

8. **Interior lighting is flat/ambient-only** — no window light shafts, no bounce light variation, kitchen/bunker read as uniformly lit boxes. → Add baked/mixed GI with a few point/spot lights at realistic intensity (~1–3 lux equivalent, warm 2700K indoors), enable Volumetric/light shafts through the kitchen window via a Light Renderer Feature.

**Priority order for next pass:** materials (#1) → shadows/AO (#2–3) → post-processing (#4), since those three compound to fix nearly everything else cheaply.
