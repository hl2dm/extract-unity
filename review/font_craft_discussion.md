## 1) Font

**Recommend GenSenRounded 源樂圓體** as the primary UI font — the core complaint is thinness, and it's the only option with real weight range, so hierarchy comes from weight, not just size. Keep **jf-openhuninn 2.1** as a secondary "flavor" font for 3D extruded shop-sign lettering and speech-bubble text, since its bouncy single-weight letterforms read as more hand-drawn/crayon than GenSenRounded's geometric roundness. Skip Noto Sans TC Black — too corporate/sturdy for this tone.

**Hierarchy:**
- Title: GenSenRounded Heavy/Black, 56–64px
- Button: GenSenRounded Bold, 30–32px
- Body/dialogue: GenSenRounded Medium, 20–24px
- Small labels/HUD: GenSenRounded Regular, 14–16px

**Legibility on pastel:** 3–4px outline in a dark plum/brown (not black — softer against pastel), plus a low-opacity soft drop shadow (2px offset, 30–40% opacity) for buttons/titles only; skip outline on small labels, use a solid rounded pill background instead.

**3D extruded shop-sign letters:** use jf-openhuninn, extrude depth ~8–10% of cap height, rounded bevel edges, warm cream/candy-color faces with a thick (6–8px) outline stroke matching the sign border, crayon-grain normal map (no color texture) so it catches light like wax.

## 2) Buildings — 透天厝 craft checklist

1. Chamfer all box edges — no sharp cube corners
2. Randomize story height/count per building for skyline rhythm
3. Recess windows slightly; add tiny rounded flower-box ledges
4. Simplified low-poly 窗花 (iron grille) as geometry, not texture
5. Ground-floor rolling shutter (鐵捲門), a few left half-open
6. Scalloped-edge pastel awning over storefront
7. Rooftop clutter: rounded water tank, clothes rack, potted plants
8. Sagging clothesline/wire cylinders strung between buildings
9. Protruding rounded-corner signboard for shop name
10. Slight random rotation/offset per building — avoid grid alignment
11. Flat 2–3 color block per floor instead of photo texture
12. Small rounded AC box units on exterior walls
13. Darker pastel weathering band at the base, not a grime texture
14. Baked vertex-color gradient (cool top, warm base) instead of texture maps
