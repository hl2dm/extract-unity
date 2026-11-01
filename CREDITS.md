# Credits (casual build)
- Cat / Dog models (Assets/Resources/Pets) — Quaternius, "Animated Animales Low Poly" / Animal Pack Vol.2, CC0 1.0 (https://opengameart.org/content/animated-animales-low-poly). Verified 2026-10-28: files are byte-identical (MD5) to Cat.fbx/Dog.fbx in the official zip.
- UI icons — derived from Noto Color Emoji (SIL OFL 1.1 / Apache 2.0), re-processed into pastel crayon stickers (posterise, pastelise, grain, wobbly outline) by casual/pastel_icons.py.
- Sounds cry/honk/engine — procedurally synthesized (casual/gen.py). Cat/dog sounds are real CC0 recordings (see below).
- Font — 源樂圓體 GenSenRounded2 TW v2.100 (Bold / Heavy) by ButTaiwan (SIL OFL 1.1, subset to used glyphs; licence at Assets/Resources/Fonts/GenSenRounded-OFL.txt). Noto Sans TC and Iansui (SIL OFL 1.1) kept as fallback files.
- Remaining furniture / props — see the original CREDITS (Poly Haven CC0, Kenney CC0, Quaternius CC0).
- Removed in casual build: soldier, rifles, helicopter.

## One-map rebuild (阿姨家)
All town, house, furniture, bus and bedroom geometry is procedural (SoftKit/SoftProps, original code). No third-party meshes are used on this map or in the bedroom base. Pet models: Quaternius (CC0), proportions adjusted at runtime.

## Assets in this public repository
- Tex/ and Sky/ (PBR textures, HDRI): Poly Haven / ambientCG, CC0.
- Audio/*.ogg: Kenney (Impact Sounds, Interface Sounds), CC0. Audio/*.wav: procedurally synthesised.
- ItemPics/: item illustrations generated for this project (not third-party stock).
- Unused realistic assets (Poly Haven models, soldier, guns, Quaternius UAL animations, helicopter, car) were removed from this repository and from the casual build.

## v0.9.20
- Hideout/menu music `Resources/Audio/bgm_hideout.ogg` — AI-generated (YuE) continuation, seed 20261101; only the generated part after 15.3 s of the extension is used (loop 18.30–103.63 s of the extended render, 128 beats @ ~90 BPM, 0.6 s equal-power seam, -18 LUFS). The 0–15 s prompt audio is not included anywhere in this project.

## v0.9.21
- Cat / dog sounds (`Resources/Audio/cat_meow0-4, cat_hiss1, cat_purr0, dog_woof0-3, dog_yip0-3, dog_growl0 .ogg`) — real recordings from BigSoundBank (Joseph SARDIN, https://bigsoundbank.com), CC0: #0098, #1472, #1475, #1480, #1885, #1010, #2954, #2353, #2352, #0612, #1060, #0682 (trimmed, high-passed, loudness-matched).
- `cat_hiss0.ogg` — Wikimedia Commons recording by Zabuhailo, CC0.
- Pond wading / splash (`water_step0-3.ogg`, `water_splash0-1.ogg`) — BigSoundBank (Joseph SARDIN), CC0: #0690 "Shoes in the Water", #1529 and #1533 "Splash, Small" (trimmed + loudness-matched).
- Goose honk is still procedurally synthesised (quieter and rate-limited in this version).
- Fish icons (Resources/Icons u1f990, u1f421, u1f420, u1f40d, u1f38f) — generated for this project.
