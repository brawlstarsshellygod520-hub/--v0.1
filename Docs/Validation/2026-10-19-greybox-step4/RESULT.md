# Step4 validation result

2026-10-20. Status: Ready with limitations for map replacement and step4 only.
Unity 6000.5.0b9, Windows Editor, Built-in rendering / Legacy Input. No new packages.

| Criterion | Evidence | Result |
| --- | --- | --- |
| Actual supplied geometry, independent collision and Visual meshes | install.txt, source-inspection.json, native LevelOne assets | PASS |
| Saved scene references and idempotent install | UserLevelOneBuilder.BatchInstallAndValidate / BatchRefreshExport, exit0 | PASS |
| Five doors, main/rear passage, household sources, fixed Camera, NPC exit | scene-playmode.txt, 40 checks, exit0; Logs/step4-final-scene.log | PASS |
| WASD, axe/hose exclusivity, repeat hits, F, 13m, focus/pause, visual independence | tools-playmode.txt, 37 checks, exit0; Logs/step4-tools.log | PASS |
| Legacy residential input and handling | legacy-handling.txt, 27 checks, exit0; Logs/step4-legacy-handling.log | PASS |
| Visual review | 00-user-layout-overview.png, 03-water-spray.png, 06-axe.png reviewed from actual Camera.Render | PASS for Greybox |
| Source safety | Source .blend and archived copy SHA256 identical: CAB169D02A78BC7AF1C7416B88591E382C4444C438F7DE659429730AD6993351 | PASS |
| Baseline preservation | baseline.json: 289 existing files; only LocalPlayerInput.cs and TutorialGreybox.unity changed; 287 unchanged | PASS |
| Physical keyboard/mouse / Player build / multiplayer | Not run, no claims | NOT RUN |
| Tutorial stages5–9, damage/destruction | Outside authorized step4 scope | NOT IMPLEMENTED |

Runtime tests use the same TickCommand path as device polling, enabled input with ReadDevices=false. They exercise actual CharacterController and physics, not hardware input. All required referenced assets/meta files imported by Unity. Existing CS0618 warnings remain; final tested code has no compiler errors or runtime exceptions.

The source used is the saved attachment, not unsaved changes in the user's open Blender window. Original source and window were never saved over or closed. Playable copy is scaled14x, walls/doors cutaway.8m, doors open90 degrees; ground added. Bathroom cutter depth and tiny doorway-bottom strips were repaired in export only.

Failed checks are retained: scene-first-failure.txt / import-cap-failure.png (bad cap), scene-door-failure.txt (shallow bathroom opening), scene-npc-failure.txt and Logs/step4-npc-diagnostic.log (door-bottom strip). Final rerun passed after correcting geometry, not disabling collision or bypassing NPC logic. Logs/step4-threshold-refresh.log and step4-scene-pass.log also preserve the intermediate transform-update-order failure; only step4-final-scene.log is the passing final map run.

The gameplay cable remains a simplified straight-length constraint and can cross walls. No source capacity, door destruction, formal animation or UI added. Build Settings remain ResidentialFire only; open TutorialGreybox explicitly in Editor.
