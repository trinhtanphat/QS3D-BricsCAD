# Work claim — local V25 sanitized-summary output alias safety

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-v25-sanitized-output-alias`
- Registered: `2026-08-13T22:48:00+07:00`
- Baseline main SHA: `590dbfe947f4808e28f38681f1bf0e27314578de`
- Priority: owner-requested continue-all bug audit; prevent the shareable-summary CLI from destructively overwriting its raw qualification evidence when `--output` aliases `--input`.

## Reserved scope

Harden the local V25 sanitized-summary exporter so an output path that resolves to the same file as the input qualification report fails closed before any filesystem mutation. Extend the existing pure-Python sanitized-evidence preflight with a same-file alias regression that proves the source JSON remains byte-for-byte unchanged and no Markdown is published over it.

## Expected surfaces

- `scripts/export-local-v25-sanitized-summary.py`
- `scripts/preflight-local-v25-sanitized-evidence.py`
- this claim file for close-out

## Excluded scope

- `scripts/run-local-v25-qualification.ps1` execution semantics or step ordering.
- BricsCAD runtime behavior, private/customer DWGs, package/signing/updater/release workflows, `src/**`, `tests/**`, active Floor/Level, V25 version visibility, model-health, #987, #1005, or other LOCAL_ONLY lanes.
- GitHub Actions dispatch/re-run and licensed V25 runtime qualification.

## Validation plan

- Re-fetch exact exporter/preflight blobs from the claim baseline before implementation.
- Reject input/output identity using normalized resolved paths before `destination.parent.mkdir(...)` or `destination.write_text(...)`.
- Add a subprocess regression using one real temporary `qualification.json` as both `--input` and `--output`; require non-zero exit, a deterministic safety error, and byte-for-byte preserved JSON.
- Execute the exact pure-Python preflight locally from the fetched source where possible; no BricsCAD or GitHub Actions required.
- Read back the pushed `main` files/SHA and mark this claim `COMPLETED` only with truthful validation evidence.

## Completion condition

The sanitizer refuses destructive input/output aliasing before mutation, the regression locks preservation of raw evidence, the exact preflight passes, and the claim is closed without touching concurrently reserved product/runtime lanes.
