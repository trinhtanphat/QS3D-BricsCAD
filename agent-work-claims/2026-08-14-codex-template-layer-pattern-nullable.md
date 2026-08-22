# Work claim — Template layer-pattern nullable compile guard

- Status: `ACTIVE`
- Agent: `/root/fix_curtain_method_gates`
- Registered: `2026-08-14T15:14:06+07:00`
- Baseline main SHA: `427d029ad834197a43ddfa302e36128334af5ae4`
- Priority: current-main Core/V25 compile blocker

## Verified regression

Commit `a2850033a65fe7fe18c6681a596e33b3331dcc05` correctly rejects blank and whitespace-padded persisted layer-mapping patterns, but `RequiredCanonicalLayerMappingPattern` evaluates `raw.Trim()` in a compound condition after `string.IsNullOrWhiteSpace(raw)`. The target nullable compiler does not narrow `raw` through that helper call and reports `CS8602`.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`: add only an explicit null branch to the existing canonical pattern guard, preserving every accepted/rejected value.
- existing `tests/QS3D.Core.SmokeTests/TemplateLayerMappingPatternCanonicalitySmoke.cs`: validation only; edit only if the existing focused coverage cannot exercise the repaired source.
- this claim and `2026-08-12-1403-chatgpt-web-gpt56sol-layer-mapping-pattern-canonicality.md` for ownership handoff/closeout only.

## Coordination

The older layer-pattern canonicality claim remains marked `ACTIVE`, but its source/smoke implementation is already integrated at `a2850033a65fe7fe18c6681a596e33b3331dcc05` and no open PR remains. This compile-only follow-up owns only the explicit null guard at that integrated hunk and does not alter pattern/category canonicality, recognition behavior or the focused smoke contract.

## Validation and exclusions

- Run the existing layer-pattern canonicality smoke through the Core smoke executable.
- Build Core and installed-reference BricsCAD V25 `Release|x64` with zero warnings/errors.
- Run aggregate preflight when current-main independent gates allow, recording unrelated failures.
- No P10/LOCAL automation or evidence; no BricsCAD/native adapter behavior; no private data, V26/release/signing or GitHub Actions.

Completion means the one-line behavior-preserving compile fix is merged through a normal PR, this claim is closed, and an exact merged-main SHA is returned to `/root` for P10 rebase.
