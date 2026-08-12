# Agent Work Claim — BLT quantity preset discoverability

- Status: `COMPLETED`
- Agent: ChatGPT Web / GPT-5.6 Sol
- Registered: 2026-08-12 08:34 +07:00
- Baseline `main`: `de79c116f1eb10f60780148d9cc040e70a92d5aa`
- Priority: user-requested BLT3D calculation-settings completion

## Confirmed gap

The canonical BLT quantity compatibility preset and `QS3DSETUPBLT` command were already merged on `main`, but the command was absent from `StartCenterCommandCatalog` and `docs/COMMANDS.md`. A user therefore needed to already know the hidden command name to discover the staged preset workflow.

## Reserved scope

- Register the existing `QS3DSETUPBLT` command in Start Center under the quantity/setup workflow with wording that makes explicit that the preset is staged for review and persists only after Save.
- Document the existing command in `docs/COMMANDS.md` without changing its runtime behavior.
- Add a focused static preflight/source regression that pins command registration + documentation and does not require BricsCAD runtime.

## Explicit exclusions

- Do not alter `QuantityCalculationBltCompatibilityPreset`, its exact 28 category / 784 directed-rule payload, or Core quantity math.
- Do not change native `CreateDefault()` semantics.
- Do not change Quantity Settings persistence, backup rotation, future-schema handling, stale-save guards, modeless lifecycle, or manual user-created rule defaults.
- Do not dispatch GitHub Actions or claim licensed BricsCAD V25/V26 runtime PASS.

## Completed implementation

- Start Center registration: `cfbf2cffc509dee3386706f14ff86576ed8ba4bb`
- Canonical command reference: `a41b81ea8b1251ad51cb5606bb645fc40f3b3c77`
- Focused static preflight: `315b15af15b50279f4edb8d3613f35f67366b033`

## Verification

- Read back current `main` at `52befb4bc5b83f72cbaf749dd29d15c3f99a9252`: Start Center contains exactly the intended `QS3DSETUPBLT` entry, `docs/COMMANDS.md` documents the staged-draft/save behavior, and `scripts/preflight-blt-preset-discoverability.py` is present.
- Ancestry compare shows `cfbf2cffc509dee3386706f14ff86576ed8ba4bb` and `315b15af15b50279f4edb8d3613f35f67366b033` are ancestors of that `main` snapshot with `behind_by: 0`.
- The Python gate was added and statically read back but was not executed through the GitHub connector. GitHub Actions and licensed BricsCAD V25/V26 runtime were not run or claimed PASS.

## Completion condition

Satisfied: Start Center and the canonical command reference expose the already-merged `QS3DSETUPBLT` workflow, a focused source gate protects discoverability, and the implementation writes are present on `main` with exact SHAs above.
