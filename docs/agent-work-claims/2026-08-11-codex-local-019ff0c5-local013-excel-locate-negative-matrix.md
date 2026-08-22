# Work claim — LOCAL-013 Excel Locate negative matrix

- Status: `COMPLETED`
- Agent: `codex-local-019ff0c5` (`/root`, local Windows + licensed BricsCAD V25 agent)
- Registered: `2026-08-11T19:48:24+07:00`
- Baseline main SHA: `ab11b45414ff50b8f9c2010abeb2f89bfe4331ad`
- Priority: `LOCAL-013 / P0` — complete the missing fail-closed Excel-to-CAD runtime evidence for the owner-requested B4D/ED2 Handle round-trip

## Reserved scope

Qualify the existing LOCAL-013 eligible-CAD B4D → newly generated ED2 workbook → Excel Locate lane on an exact clean candidate SHA. The runtime matrix covers one positive locate plus wrong drawing fingerprint, unknown semantic Element ID, zero-live stale Handle and partial-live Handle refusal, with exact PICKFIRST preservation and authoritative semantic-project non-mutation for every refusal. Keep the umbrella source gate aligned with the shared resolver boundary so exact qualification cannot fail on a stale file-local token check.

## Expected surfaces

- `scripts/test-bricscad-v25-brc-quantity-roundtrip.ps1` execution against a new ordinary `*.reference-copy.dwg` only.
- `QS3DB4D`, `QS3DBRCROUNDTRIPPROBE` and the shared modern ED2 Excel Locate resolver already present on the candidate SHA.
- `scripts/preflight.py` only for the minimal shared-resolver drawing-identity assertion required by the exact qualification runner.
- Gitignored private runtime artifacts under a new temporary run root; only sanitized aggregate marker/metadata may inform documentation.
- `docs/LOCAL-AGENT-INBOX.md` LOCAL-013 exact-SHA evidence/status and this claim file for close-out.

## Excluded scope

- No proprietary BLT source/API/binary inspection and no inference from opaque BRC proxy geometry.
- No claim that opaque BRC proxies have quantity parity; they remain review-only when public APIs expose no finite category-appropriate metric.
- No modification of the owner DWG or reference XLSX, no reuse/overwrite of an existing workbook, and no committed private drawing/workbook/path/Handle evidence.
- No LOCAL-003 Level Z-chain implementation or qualification reserved by the existing local claim.
- No Create Similar, Room Finish, modeless viewer, generated-source recognition, Core mutation-atomicity, release/signing/installer, or GitHub Actions work.

## Validation plan

- Re-fetch current `main`, require a clean exact candidate SHA, run aggregate source gates, Core smoke and V25 x64 Release build locally.
- Hash the owner reference DWG, create an ordinary disposable copy under a GUID temporary directory, and verify copy/original hashes before execution.
- Run the BRC round-trip runner with a known automation profile and a bounded extended timeout; require V2 sanitized evidence for 4/4 typed refusals, 4/4 PICKFIRST preservation, 4/4 semantic non-mutation, stale 0/1 and partial 1/2 live resolution, plus the positive located selection.
- Verify the copy and owner hashes remain unchanged, no sidecar or BricsCAD process remains, review only sanitized marker/metadata, then remove private artifacts recoverably.
- Do not dispatch GitHub Actions.

## Coordination

This reservation is runtime/evidence-only and does not overlap the active LOCAL-003 native Level Z-chain claim. It also does not take any source surface owned by the active remote claims. The ED2/Excel Locate source patch began before the registration protocol landed on `main`; this claim governs the new local qualification and documentation close-out after that policy became current.

## Completion condition

The exact clean candidate passes the positive and four-case negative Excel Locate matrix on licensed BricsCAD V25, sanitized LOCAL-013 evidence is pushed to current `main`, private artifacts are removed, no Actions are dispatched, and this claim is marked `COMPLETED` with the exact implementation/evidence SHA.

## Completion record

- Product implementation: `21f1ae2b` (`fix(ed2): harden Excel locate round-trip`).
- Exact runtime candidate: `813c51ffe6b357c86a1e2a3c93d7f2d515c057b2`.
- Validation: aggregate `447/447`, Core smoke `ALL PASS`, V25 x64 Release build `0 warnings / 0 errors`, licensed NETLOAD/Ribbon/Palette PASS, LOCAL-013 positive locate plus four typed negative refusals PASS.
- Safety evidence: PICKFIRST preserved `4/4`, semantic state unchanged `4/4`, stale resolution `0/1`, partial resolution `1/2`, original/copies hash-stable, no sidecar/process left, no proxy quantity/locate claim.
- Remaining product queue: exact-current workbook visual rendering remains `NOT_RUN`, so the product-level LOCAL-013 item remains `IN_PROGRESS`; this reserved negative-matrix lane is complete.
- GitHub Actions: not dispatched.
