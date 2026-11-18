# Model-testing optimization migration

Base: local origin/model-testing b157a0b (2026-11-10). Online freshness was not verified.
Branch: model-testing-optimization. No push performed.

Migrated with three-way merges against main 36d5c95:
- ACT1 repeat fitting, simulator secondary-button support and editor diagnostic shortcut.
- ACT1 fitting proximity highlight and release hint.
- Current-target guidance, distance, marker and arrival task instructions.
- ACT2 inspect-and-return completion, part outlines, socket feedback, checklist and recovery.
- Staff controls collapsed by default.
- Simulator Page Up/Down vertical translation.
- Two malformed editor meta files repaired.

Preserved model-testing scene, models, prefabs, materials, RegionalActions positioning fix and package versions.
Preserved ACT2 grab-audio crash mitigation; new reattachment feedback is silent too.
Excluded old working-copy automatic model-import, material, XR and editor-version changes.

Validation: clean three-way merges; git diff --check; simulator JSON parsed; scene, RegionalActions and package files match base byte-for-byte.
Pending: Unity compilation and runtime checks in this new project, all seven ACT2 part targets, release after switching hands, buttons, warning recurrence, sequential playthrough without Skip and headset testing. Previous screenshots do not validate this new scene.
