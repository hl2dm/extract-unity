# v0.9.7 demo-era leftover sweep (阿姨家 map + bedroom hub)

| Element | Location | Old form | Replacement | Status |
|---|---|---|---|---|
| Window grille decals | AuntMap houses, SoftProps.Grille | random circles over mullions | 3 forged motifs (lotus / diamond lattice / sunrise fan) aligned to panes, mullions skipped | DONE |
| Round window | SoftProps.RoundWin | fat rings + centre ball + glint ball | recess, tinted glass, slim frame, moulding, thin petals/spokes | DONE |
| Door fanlight | SoftProps.DoorVisual | beige disc + covering plank | half-round glazed fanlight with sunburst bars | DONE |
| House-number plate / mailbox | Craft() gt==2 | covered by grille, orange slab | plaque above arch, real mailbox (lid, slot, flag) beside door | DONE |
| Eave brackets | ArchDetail | orange pegs | knee brackets (post + arm + strut) | DONE |
| Bed | SoftProps.Bed | box + slab | framed bed, headboard slats, mattress, pillow, stitched quilt | DONE |
| Bedroom furnishing | BaseHub | ring of 7 flat balls round rug | nightstand + lamp light, low bookshelf, toy bin, wall frames, plant | DONE |
| Toy blocks | SoftProps.ToyBlocks | loose rounded lumps | lettered bevelled cubes, tower w/ arch + roof, pieces lying flat | DONE |
| Rug | SoftProps.Rug | flat disc | patterned rectangular rug with border, medallion, fringe | DONE |
| Living-room ring | AuntMap:272 | Ring around rug | removed | DONE |
| Floor mat | AuntMap:304 | flat Mint ball disc | SoftProps.Rug | DONE |
| Manhole covers | AuntMap:434 | flat ball | cast grate ribs | DONE |
| Park/street trees (kind 0/1) | SoftProps.Tree | stick + pom-pom ball | tapered trunk, root flare, forked branches, leafy clusters | DONE |
| Clouds | SoftProps.Cloud | 4 balls | layered cumulus (flat base deck, mid puffs, top puffs, cool underside) | DONE |
| Wardrobe feet / knobs | ContainerBody wardrobe | ball feet, ball knobs | turned feet, bar pulls | DONE |
| Pendant lamp | BaseHub ceiling | 4-point cone | bell shade + bulb | DONE |
| Conifer tree (kind 2) | SoftProps.Tree | stacked lathed cones | — | OPEN (reads OK, stylised) |
| TV antennas | TVStand | ball tips | — | OPEN |
| Pink diamond marker over bedroom rug | hub (likely ItemArt marker cube) | rotated cube | — | OPEN (source not pinned) |
| Blue cable line near desk | BaseHub | thin line on floor | — | OPEN |

## v0.9.8 status (final build e51)
- FIXED pink diamond over bedroom rug: rug backing block (12 mm, bevelled) poked through the patterned top; now 6 mm under the field. Verified by per-renderer isolation + before/after shot.
- FIXED blue line by desk: HintRing floor ring shown under hub Stations; hidden for Stations.
- Wall frames (bedroom 2 + hub 4) and the town-map poster are now single illustrated storybook PNGs on quads inside modelled frames (Assets/Resources/Art).
- Real conifer, TV antennas with ball tips (TV ≤1.6 m), leaf-cluster foliage, skirting/crown/wainscot, toy-block colour jitter + relief letters.
- Remaining: shop signs, menus, book spines and packaging are still primitive/procedural (not converted to illustrated art this round); map poster reads small/dark at distance.

## v0.9.9 round 2 (after e62 Claude review = 4)
- Toy corner grey: cause was the muted house palette (LavD/MintD read grey under light) in ToyBlocks; now saturated toy paint. Bare cylinders/prisms replaced by wooden train, panelled beach ball, tin robot.
- Cone: ridged body, two reflective bands, rubber base. Hydrant: flange bolts, collar, dome bonnet, pentagon nut, hex caps, pumper nozzle, cap chain.
- Plush bear: tapered limbs, muzzle + stitched smile, seam stitches, foot pads, bow.
- Round-window medallion varies (4/6/8 petals) per window.
- Item pickups: opaque black disc ("hole in rug") -> soft blob shader.
- Shop icons + town map redrawn flat/outlined to match world. Remaining painterly art: art_tv/menu/kid/hills/pk/s/sp.
- HUD: bag + heart icons. Ammo/timer still text-only.
- Removed temp PicCount/NoArt toggle.

## v0.9.9 round-3/4 status (stopped: <8 after 2 rounds)
- r1 (e65) SCORE 4, r2 (e66) SCORE 6. Remaining blockers per review:
  - wall clock in L_interior has no face (numbers or hands)
  - L_cu_mailbox camera doesn't frame the mailbox
  - blank brown board near the bench (L_st_7)
  - the beach ball still reads as a plain sphere in L_cu_blocks
  - featureless cardboard box (L_cu_bench)
  - scooter wheels/neck are still primitive
  - the lamp base and trash cans still read plain at street distance

## v0.9.9 round 3 (e74): SCORE 5, stopped and not released
- Done: illustrated clock face, textured 6-panel beach ball, cardboard box (flaps, tape, label, seams), scooter tyre/rim/spokes and neck collars, dark plinth on the lamp base, bin with contrast band and dark lid, mailbox with a MAIL plate and flag, moved clear of the door pillar, close-up camera fixed.
- Reviewer now flags: a 2D illustration style that doesn't match the 3D world (map, TV, menu icons, sign card), the hydrant and robot reading as blockout, three different sign-making styles on the street, the yellow door pillar, the teal wall art, and the medallion.
- The score swings between runs (6 to 5, and items praised in r2 were criticised in r3), so reviewer variance is about ±1.
