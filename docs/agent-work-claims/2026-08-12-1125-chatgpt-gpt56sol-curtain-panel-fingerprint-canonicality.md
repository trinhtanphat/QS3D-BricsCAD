# Work claim — Curtain Panel fingerprint canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-curtain-panel-fingerprint-canonicality-20260812-1125`
- Registered: `2026-08-12T11:25:00+07:00`
- Completed: `2026-08-12T11:30:00+07:00`
- Integration PR: `#820`
- Source commit: `73197d73e398557c9a8b3fcc931df7cf7f14c91d`
- Regression commit: `bd3d073cf2f67ae3a714a8ef93b1d5440a37f27a`
- Reviewed head: `dcc0541c8b4575d2be70a09f7ceeaa9d332092da`
- Main integration SHA: `d820eb4adb80aef34069d89be6123683cb5b7993`
- Priority: P1 generated-output health parity

## Confirmed defect

`CurtainWallPanelFingerprint.Compute(...)` emits a 64-character lowercase SHA-256 digest using `x2`. `GeneratedCurtainPanelHealthService.Fingerprint(...)` previously trimmed the persisted snapshot and only checked length/hex shape, so writer-noncanonical aliases such as uppercase or surrounding whitespace remained health-clean. The sibling Curtain Frame health provider already made equivalent writer-owned fingerprint aliases fail-visible.

## Completed contract

- Existing missing/invalid fingerprint warning semantics and precedence are preserved.
- An otherwise valid 64-hex digest must now match its exact lowercase writer-owned spelling ordinally with no surrounding whitespace.
- Uppercase/padded aliases emit Error `CURTAIN_PANEL_CONFIG_FINGERPRINT_NON_CANONICAL`.
- Invalid/missing snapshots retain `CURTAIN_PANEL_CONFIG_FINGERPRINT_INVALID` without also being mislabeled as canonical aliases.
- Fingerprint computation, integer/handle/mode/build-state/floating metadata, stale logic, native materialization and BricsCAD runtime behavior were not changed.
- Focused auto-registered smoke covers uppercase, padded, exact-lowercase, invalid-shape and missing-value controls.

## Integration evidence

- Writer readback confirmed `CurtainWallPanelFingerprint.Compute(...)` returns lowercase SHA-256 via `x2`.
- Current-main comparisons repeatedly isolated the feature branch net diff to exactly `GeneratedCurtainPanelHealthService.cs` plus `GeneratedCurtainPanelFingerprintCanonicalitySmoke.cs` while concurrent agents advanced unrelated areas.
- The branch was synchronized by fast-forward-only ref moves using current-main-derived trees; no force-push was used.
- PR #820 became mergeable on reviewed head `dcc0541c8b4575d2be70a09f7ceeaa9d332092da` and was squash-merged with expected-head locking as `d820eb4adb80aef34069d89be6123683cb5b7993`.

## Validation boundary

No GitHub Actions were dispatched. No local .NET build/full executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed from this connector-only integration.
