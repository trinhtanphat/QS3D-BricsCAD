# Agent Work Claim — BLT quantity preset discoverability

- Status: `ACTIVE`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Registered: 2026-08-12 08:34 +07:00
- Baseline `main`: `de79c116f1eb10f60780148d9cc040e70a92d5aa`
- Priority: user-requested BLT3D calculation-settings completion

## Confirmed gap

The canonical BLT quantity compatibility preset and `QS3DSETUPBLT` command are already merged on `main`, but the command is absent from `StartCenterCommandCatalog` and `docs/COMMANDS.md`. A user therefore needs to already know the hidden command name to discover the staged preset workflow.

## Reserved scope

- Register the existing `QS3DSETUPBLT` command in Start Center under the quantity/setup workflow with wording that makes explicit that the preset is staged for review and persists only after Save.
- Document the existing command in `docs/COMMANDS.md` without changing its runtime behavior.
- Add a focused static preflight/source regression that pins command registration + documentation and does not require BricsCAD runtime.

## Explicit exclusions

- Do not alter `QuantityCalculationBltCompatibilityPreset`, its exact 28 category / 784 directed-rule payload, or Core quantity math.
- Do not change native `CreateDefault()` semantics.
- Do not change Quantity Settings persistence, backup rotation, future-schema handling, stale-save guards, modeless lifecycle, or manual user-created rule defaults.
- Do not dispatch GitHub Actions or claim licensed BricsCAD V25/V26 runtime PASS.

## Completion condition

Start Center and the canonical command reference both expose the already-merged `QS3DSETUPBLT` workflow, a focused source gate protects discoverability, all writes are present on current `main`, and this claim is closed with exact commit SHAs.
