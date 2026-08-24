# Work claim — #3820 LOCAL-014/P03 advanced prompt cancellation

- Status: `ACTIVE / LOCAL_ONLY`
- Agent: `codex-local-worker` (`/root`)
- Issue: `#3820`
- Lane-Key: `issue-3820`
- Branch: `agent/codex/issue3820-local014-p03-adv-cancel`
- Registered: `2026-08-25T04:37:58+07:00`
- Baseline `origin/main`: `2589d00dd0842cfe9a529edeb3dfcd235e0d36cb`
- Priority: `LOCAL-014 / P1 / P03`

## Reserved runtime cell

Qualify the existing production `QS3DCONVERT2DADV` command on licensed BricsCAD V25.2.10 x64 by reaching each of its three numeric prompts and cancelling with physical ESC delivered only to the exact process launched by the guarded local runner:

1. cancel Thickness;
2. accept Thickness, then cancel Height;
3. accept Thickness and Height, then cancel BottomOffset.

Each case uses an eligible Model Space LINE in a disposable non-customer DWG. It must return to `CMDACTIVE=0`, preserve the source and native object counts, and create no project/cache/sidecar, semantic element, generated Solid3d, audit mutation, backup/lock/script residue, or cross-DWG mutation.

## Identity and safety requirements

- Candidate must be a clean pushed SHA descended from the registered baseline.
- Build the canonical V25 `Release|x64` output against installed BricsCAD references and require portable-PDB SourceLink identity for that exact SHA.
- Require zero pre-existing BricsCAD processes, exact launched-PID input, fixed DemandLoad restoration, unchanged disposable fixture hash, graceful host cleanup, restored environment values and ignored raw evidence.
- Publish only sanitized aggregates, exact Git SHA, host/product identity, plugin/Core hashes and cleanup results.

## Excluded scope

No production source edits, preview-context drift, forced mid-batch rollback, Undo/Redo, save/reopen, multi-DWG, V26, private/customer DWGs, release operation or GitHub Actions. P01/P02 are already bounded PASS and will not be rerun. Any runtime product defect is handed to a separate remote/source issue; this local lane does not patch ordinary source.

## Completion boundary

A PASS qualifies only `P03_ADV_PHYSICAL_ESC_PROMPTS`. It does not promote overall LOCAL-014, whose drift, rollback, Undo/Redo, save/reopen and multi-DWG matrix remains pending.
