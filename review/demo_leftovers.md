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
