# Live sheet STT2-STT5 source fixes

Status: SOURCE_FIXED
Owner: chatgpt-web-gpt56sol
Started: 2026-08-14 12:03 +07:00
Closed: 2026-08-14 12:48 +07:00

## Scope

- STT2: `QS3DDRAWBEAM` must preserve the current viewport; no automatic switch to 3D and no automatic zoom after drawing a beam.
- STT3: Family Manager custom-property key is optional in UI; preserve standard properties and existing Family duplication/copy behavior.
- STT4: harden `CHI TIẾT CẤU KIỆN` quantity-detail path against unsafe list indexing that can throw `ArgumentOutOfRangeException`.
- STT5: fix `QS3DSETUP` / `QuantitySettingsWindow` WPF construction failure caused by root `StaticResourceExtension` lookup before the merged theme resources are available.

## Ownership boundary

Only files required for STT2-STT5 plus focused regression guards/tests. Curtain/#1105/#1106 and LOCAL_ONLY/runtime-qualification lanes were not modified.

## Completion evidence

- Source fix: `2cb50b15cf778dbeb60950a076987ab3a20089c6` (`fix: close live sheet STT2-STT5 source bugs`).
- Focused regression gate: `d8bfbf32e495ffcb49622fde6849bdeed5f59464` (`test: guard live sheet STT2-STT5 regressions`).
- `scripts/preflight-live-sheet-stt2-stt5.py` is automatically discovered by `scripts/preflight-all.py` through the existing `preflight-*.py` convention.
- Read-back on `main` confirms: Beam keeps post-commit refresh/regen/status while skipping `QS3DVIEW3D`; blank Family custom-property keys return before domain mutation and Duplicate remains wired to `ProjectFamilyService.Duplicate`; quantity-detail binding suppresses SelectionChanged re-entrancy and uses `FirstOrDefault()` instead of blind index zero; `QuantitySettingsWindow` root uses `DynamicResource` for `Bg0Brush` and `TextBrush` before `Window.Resources` merges `Theme.xaml`.
- No force-push was used.
- No Curtain/#1105/#1106 or LOCAL_ONLY source/runtime lane was changed by this claim.

## Qualification boundary

This claim is SOURCE_FIXED. Native licensed BricsCAD V25 acceptance for these UI/runtime cases is not inferred from source/preflight evidence and remains a separate runtime qualification step if required by the release policy.
