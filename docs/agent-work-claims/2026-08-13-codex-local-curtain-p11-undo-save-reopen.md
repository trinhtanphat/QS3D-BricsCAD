# Work claim — LOCAL-002/P11 Curtain Undo, save/reopen and rebuild

- Status: `COMPLETED`
- Agent: `codex-local-root` (`/root`, local Windows + licensed BricsCAD V25 worker)
- Registered: `2026-08-13T15:10:00+07:00`
- Baseline main SHA: `36db4723b311b5f40fbde8983877d6965ef0ed9b`
- Priority: `LOCAL-002 / P0 / P11` — this is the next compatible licensed-only Curtain cell after bounded P01-P09 evidence; P10 is source-blocked and handed to remote issue `#982`.

## Reserved scope

Prepare and execute one bounded, automation-only BricsCAD V25 qualification for `docs/CURTAIN-NATIVE-PANELS.md` P11 using a repository-generated disposable drawing copy and legacy/no-Level LINE GlassWall data.

The local scenario will:

1. create one normal GlassWall/Curtain host-frame-panel set plus one unrelated unmarked native sentinel;
2. capture canonical semantic metadata, generated owner slots, native counts/extents and live/config fingerprints without publishing Handles or semantic IDs;
3. execute native Undo and Redo around the completed `QS3DCURTAIN3D` command and test coherence at both boundaries;
4. save the DWG plus canonical QSDB sidecar, close the launched process, reopen cold in a second isolated BricsCAD V25 process and verify the same ownership/fingerprint/Health state;
5. rebuild from the reopened project/source and prove the complete old generated set is replaced, the new set is healthy, and the unrelated native sentinel remains unchanged;
6. verify exact-SHA/plugin identity plus process, private-script, drawing-lock and temporary sidecar/backup cleanup before accepting aggregate-only evidence.

If licensed runtime exposes a product defect, this local lane will publish only the minimum sanitized phase/code and hand the remote-safe source fix to a non-local agent. It will not repair normal product source under the local-worker capability lock.

## Expected files and surfaces

- new `src/QS3D.BricsCAD.V25/CurtainPanelUndoReopenRuntimeProbeCommands.cs`;
- new `scripts/test-bricscad-v25-curtain-panel-undo-reopen.ps1`;
- new `scripts/preflight-curtain-panel-undo-reopen-runtime-probe.py`;
- `docs/CURTAIN-NATIVE-PANELS.md` for the exact runner contract and bounded outcome;
- `docs/LOCAL-AGENT-INBOX.md` only when recording a completed exact-SHA result or sanitized source-blocking handoff;
- this claim file for completion status/evidence.

The probe may reuse existing public production commands and existing CAD/Core health/ownership services, but it must not add a second Curtain planner or generated-ownership model.

## Exclusions

- No production Curtain builder, transaction, planner, ownership, Health, persistence or lifecycle fix is reserved.
- No P10 Workspace/Family selection fix; that remote-safe source bug is tracked by GitHub issue `#982`.
- No P12 multi-DWG/modeless Curtain Hub qualification.
- No Level-reference matrix owned by active `LOCAL-003`; the P11 fixture remains legacy/no-Level.
- No private/customer/BLT drawing, proprietary API/binary inspection, installer, signing, V26, release or GitHub Actions operation.
- No broad Core/static/CI cleanup if unrelated failures are encountered.

## Validation and evidence

- Fetch/rebase current `origin/main` before every implementation and evidence commit.
- Run the focused new static gate plus existing P01/P05/P06/P08/P09/native-panel/runtime-health gates.
- Parse the guarded PowerShell runner under Windows PowerShell 5.1.
- Build `QS3D.BricsCAD.V25` `Release|x64` against the installed BricsCAD V25 managed references with zero warnings/errors.
- Run the exact clean-SHA licensed V25 scenario only from a fresh ordinary `*.curtain-undo-reopen-probe-copy.dwg`, an initialized profile, an empty artifact directory outside the repository and no pre-existing BricsCAD process/sidecar.
- Marker/metadata must contain only allowlisted phase/status, counts, booleans, exact Git/plugin/host identity and hashes; never paths, profiles, Handles, IDs, XData values, drawing content, exception messages/types/stacks or customer data.
- Do not dispatch GitHub Actions.

## Coordination

The current ACTIVE `LOCAL-003` claim explicitly excludes Curtain P01-P12 implementation; this claim avoids its Level placement and runtime fixture. Open UI PR `#975` is also excluded. General source fixes discovered by P11 remain available to remote agents after a sanitized handoff.

## Completion condition

The claim is complete when the guarded probe/runner/gate is integrated on current `main`, exact-SHA V25 evidence is recorded truthfully as either the full bounded P11 PASS or a sanitized source-blocking FAIL, cleanup is verified, P10/P12 and overall LOCAL-002 remain correctly scoped, and this claim is marked `COMPLETED` without operating GitHub Actions.

## Completion evidence

- Probe/runner/gate preparation merged through PR `#984`; pickset-boundary corrections merged through PRs `#985` and `#986`; native drawing-backup cleanup merged through PR `#988`.
- Exact clean candidate `484a9c686a10edd4f04d7edf135031b2328c8c29` built `Release|x64` against BricsCAD V25 managed references with `0 warnings / 0 errors`; adapter SHA-256 was `4EDB6F3B06B0AC7744CEE760AEE576CE4EC5AF689400C6940CE5BDB721050F9B` and ProductVersion ended in the exact candidate SHA.
- BricsCAD `25.2.10` returned sanitized source-blocking result `native_undo / SEMANTIC_NATIVE_DIVERGENCE`. Undo coherence was false; Redo, cold reopen and ownership-scoped rebuild coherence were true. Reopened and rebuilt counts were both 1 host / 10 frames / 15 panels, Health issues were zero, source and unrelated sentinel were preserved, the old set was removed and the replacement was disjoint.
- Process, private-script, QSDB, native drawing-backup and drawing-lock cleanup were independently true. The disposable synthetic DWG was restored byte-for-byte to SHA-256 `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`.
- Remote product correction is handed to issue `#987`; P10 remains independently tracked by `#982`. P11 and overall LOCAL-002 remain source-blocked `PENDING_LOCAL`. No GitHub Actions, private/customer drawing or production Curtain fix was performed by this local claim.

### Final licensed qualification

- The final grouped Undo/Redo runner passed on exact merged SHA `58dbc1b80d45774593303856285dd69f81c7e1a2` with BricsCAD `25.2.10`, matching adapter ProductVersion and adapter SHA-256 `201AF99477BD4AA96111A69BF89F883F5870DDFFE26F5621C5AEB476B0D455C1`.
- Undo coherence, Redo coherence, cold reopen and ownership-scoped rebuild all passed. Undo removed the generated after-set, restored the semantic before-state and preserved the source/unrelated sentinel. Reopen and rebuild each produced 1 host / 10 frames / 15 panels; the old generated set was removed, the replacement set was disjoint and Health issues were zero.
- The guarded before/restored hash was `CEC1350FB2207542AEECD96A790A198A6C9CC9E99A9F875871F367554B3D967E`; all launched-process, private-script, sidecar, drawing-lock, native-backup and drawing-restoration cleanup checks passed, leaving zero BricsCAD processes.
- P11 is therefore bounded `LOCAL_PASS`. Overall LOCAL-002 deliberately remains `PENDING_LOCAL` for broad H.1 and Family-editor qualification. Issue `#987` is historical `CLOSED` and was not reopened. No GitHub Actions or private/customer data was used.
