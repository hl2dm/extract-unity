## Review: v0.8 vs v0.7

**POLY — 8/10**
Character silhouettes read far better: the pets' ears, paw pads, and tail are properly rounded now instead of faceted blobs (compare `a_poly_pets_wire` vs `b_poly_pets_wire`), and the sofa's cushions/shelf edges show clean multi-step bevels instead of hard chamfers. The density increase isn't fully adaptive though — the street/floor planes and background architecture picked up nearly as much extra mesh as the hero characters, which is triangle budget spent where players won't notice it.

**LIGHTING — 8/10**
Shadows are soft and grounded now (visible contact AO under the sofa and shelf), the warm-key/cool-fill split gives the fur and cushions a nice modeled look, and there's a subtle warm rim on the pets' backs against the street backdrop. Distant buildings in the street shot show gentle atmospheric falloff (fog) that wasn't there before. I can't verify the lamp light pools or bloom from lamps since every reference shot is daytime — worth a dusk/night capture before sign-off.

**PALETTE_COMFORT — 7/10**
The interiors are a genuine success: bedroom walls went from saturated magenta-pink to cream/warm-wood, the floor from lavender checkerboard to soft sage-blue, and pink is now correctly demoted to small accents (cushion, collar, shelf trinket) with no purple cast — this is exactly the brief. The street scene is the weak link: the sky gradient and road surface still carry a visible lavender/purple cast at the top, barely changed from v0.7. The living room ceiling was detoned but landed on a flat muddy brown rather than a clean warm cream, reading a bit dull under the new lighting.

**OVERALL — 7/10**
Strong, correct direction on both geometry and interior palette; the street/exterior hub is the one space that didn't get the same palette pass as the interiors and now feels inconsistent with the rest of the game.

### Top fixes
1. **Street sky + road**: push the sunset gradient and asphalt off lavender toward peach/amber or soft sky blue — this is the most visible remaining purple cast in the set.
2. **Living room ceiling**: replace the muddy brown-mauve with a cleaner cream or terracotta so it doesn't read as a desaturated leftover of the old pink.
3. **Adaptive tessellation**: bias the 4.3x triangle budget toward silhouette-critical areas (ears, paws, cushion seams) and off flat floor/road/sky geometry.
4. **Show a night/dusk reference**: lamp light pools are a named v0.8 feature but unverifiable from the current daytime-only screenshots.

SCORE: 7/10
