#!/bin/bash
tag=$1; note="$2"; cd /workspace/v07
cat <<PROMPT | timeout 600 claude -p --allowedTools Read --add-dir /workspace/v07 > rev/claude_$tag.md 2>rev/claude_$tag.err
You are a strict art director / level designer reviewing WORLD PROPORTIONS (scale) in a pastel toy-storybook Unity game. The player is a ~1.30 m child in first person. Open /workspace/v07/grid_$tag.jpg. Panels row-major: 1 first-person living room (sofa, round table), 2 FP kitchen (table, stool, fridge, counters), 3 FP bedroom, 4 FP street, 5 FP looking at cat+dog, 6 side view pets on street, 7 house front / shop doors, 8 base hub room, 9 scooter (parked or riding). Full PNGs in /workspace/v07/$tag/. Measured in-engine table (K = kid height 1.30 m): /workspace/v07/table_$tag.txt. Scale bible: /workspace/v07/scale_bible.md. $note
Judge ONLY proportions: do furniture, doors, ceilings, counters, vehicles, pets, houses, street props look consistent with each other and with a child's eye height and FOV? Toy exaggeration is fine if consistent. Ignore UI and lighting.
Reply exactly:
OVERALL_PROPORTION: n/10
INTERIOR: n/10 - critique
STREET: n/10 - critique
VEHICLES_PETS: n/10 - critique
CAMERA_EYE_FOV: n/10 - critique
CONSISTENCY_WITH_BIBLE: n/10 - critique
TOP FIXES: up to 6 concrete fixes with target numbers, most impactful first. Be honest and concise.
PROMPT
cat rev/claude_$tag.md
