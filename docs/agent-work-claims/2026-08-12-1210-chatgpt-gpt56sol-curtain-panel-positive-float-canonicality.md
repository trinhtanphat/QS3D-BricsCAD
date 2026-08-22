# Work claim — Curtain Panel positive float canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-curtain-panel-positive-float-canonicality-20260812-1210`
- Registered: `2026-08-12T12:10:00+07:00`
- Completed: `2026-08-12T12:13:00+07:00`
- Claim commit: `f89b4eef947b641da380c9f49170227573abfe53`
- Source fix commit: `f8a95938643587026122abeadd56db85b144d7cd`
- Focused smoke commit: `40e0d256ad974719cca24f80af510d9b93516848`
- Integration PR: `#866`
- Main integration SHA: `c805b9ab0333e7054eaf75e211c49b06cec9afee`
- Priority: P1 generated-output health parity

## Confirmed defect

Both production Curtain Panel writers persist `GeneratedCurtainPanelDepthM`, `GeneratedCurtainPanelSourceLengthM`, and `GeneratedCurtainPanelHeightM` with exact invariant round-trip (`R`) formatting. `GeneratedCurtainPanelHealthService.Positive(...)` only broad-parsed these writer-owned snapshots and checked finite `> 0`, so numeric aliases such as explicit plus, padding, or trailing-zero spellings could remain health-clean.

## Integrated contract

- Existing missing/malformed/non-finite/non-positive Warning codes remain field-specific and unchanged.
- After successful positive validation, the stored token must exactly equal `value.ToString("R", CultureInfo.InvariantCulture)` ordinally.
- Writer-noncanonical aliases emit Error `CURTAIN_PANEL_FLOAT_METADATA_NON_CANONICAL`.
- Area, sagitta, integer, handle, mode, fingerprint, stale, writer/native and persistence behavior were not changed by this lane.

## Regression evidence

`tests/QS3D.Core.SmokeTests/GeneratedCurtainPanelPositiveFloatCanonicalitySmoke.cs` is auto-registered and covers aliases for DepthM/SourceLengthM/HeightM, canonical controls, and invalid-precedence cases.

PR #866 was reviewed as exactly two changed files and squash-merged with expected head `c2ed2746144c94ba353d8e659588ce3889cc11fb` as `c805b9ab0333e7054eaf75e211c49b06cec9afee`.

## Validation boundary

Source and focused regression were integrated/read back through GitHub. No GitHub Actions/full local .NET build/executable smoke or licensed BricsCAD V25/V26 runtime PASS is claimed without execution.
