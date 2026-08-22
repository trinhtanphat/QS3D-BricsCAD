# Live sheet STT2-STT5 source fixes

Status: ACTIVE
Owner: chatgpt-web-gpt56sol
Started: 2026-08-14 12:03 +07:00

## Scope

- STT2: `QS3DDRAWBEAM` must preserve the current viewport; no automatic switch to 3D and no automatic zoom after drawing a beam.
- STT3: Family Manager custom-property key is optional in UI; preserve standard properties and existing Family duplication/copy behavior.
- STT4: harden `CHI TIẾT CẤU KIỆN` quantity-detail path against unsafe list indexing that can throw `ArgumentOutOfRangeException`.
- STT5: fix `QS3DSETUP` / `QuantitySettingsWindow` WPF construction failure caused by root `StaticResourceExtension` lookup before the merged theme resources are available.

## Ownership boundary

Only files required for STT2-STT5 plus focused regression guards/tests. Do not touch Curtain/#1105/#1106 or LOCAL_ONLY/runtime-qualification lanes owned by other workers.

## Completion

Patch source and focused regressions, commit/push to `main`, then update this claim to SOURCE_FIXED with the resulting commit SHA/read-back evidence.
