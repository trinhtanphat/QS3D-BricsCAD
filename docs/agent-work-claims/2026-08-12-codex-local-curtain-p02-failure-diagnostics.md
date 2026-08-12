# Work claim — LOCAL-002/P02 sanitized runtime failure diagnostics

- Status: `ACTIVE`
- Agent: `codex-local-curtain-p02-failure-diagnostics-20260812` (`/root/audit_preflight_latest`)
- Registered: `2026-08-12T11:36:53+07:00`
- Baseline main SHA: `db781d8042f2a1764d4c55576c6458f565764bcb`
- Priority: `LOCAL-002 / P0 / P02` — make the first licensed opening-clipping failure actionable without exporting runtime exception detail or private identity.

## Confirmed runtime observation

The guarded P02 runner was executed locally against clean exact SHA `af0aec7f` with the V25 build/static gate passing and an ordinary synthetic disposable copy. The command reached its result marker in about eight seconds, but the marker contained only `status=FAIL`, `command=QS3DCURTAINOPENINGPROBE` and `error_code=CURTAIN_PANEL_OPENING_RUNTIME_FAILED`. The disposable DWG hash remained unchanged, the launched process exited, no sidecar remained and the private script was deleted; only ordinary DWG lock-file residue was reported. No raw exception, path, Handle, semantic ID or fixture content is available or needed for this source-hardening lane.

Source audit proves the failure occurred inside the `QS3DCURTAINOPENINGPROBE` catch boundary after the script reached that command. The current marker cannot distinguish missing/rolled-back `QS3DCURTAIN3D` output from partial-case planning, complete-empty state, native extent matching, Health, Locate or publication failure. `QS3DCURTAIN3D` intentionally reports and returns on its own failure boundary, so the later probe must classify the observed postcondition rather than assume a successful prior build. No production Curtain defect is confirmed from the coarse marker alone.

## Reserved scope

- `src/QS3D.BricsCAD.V25/CurtainPanelOpeningRuntimeProbeCommands.cs` — add stable allowlisted phase and failure-class tracking to the automation-only probe/failure marker; make no raw exception data observable.
- `scripts/test-bricscad-v25-curtain-panel-openings.ps1` — validate the sanitized failure schema/allowlists and surface only the controlled phase/code on a failed local rerun.
- `scripts/preflight-curtain-panel-opening-runtime-probe.py` — lock the phase taxonomy, failure schema, privacy boundary and runner validation.
- `docs/CURTAIN-NATIVE-PANELS.md` and the bounded `LOCAL-002` section of `docs/LOCAL-AGENT-INBOX.md` only if needed to state the first-run result and exact rerun contract.
- this claim for close-out.

The production Curtain builders/planners/topology, Direct Draw commands, generated Health services, ownership/XData and Level-placement implementation remain read-only. A production fix requires a concrete failure phase plus reproducible source/runtime evidence and a separately published claim expansion before editing those surfaces.

## Intended diagnostic contract

- Failure marker keeps the existing automation command, nonce, boundary and `production_local002_qualified=false`, and adds only one stable phase token plus one coarse failure-class token selected from source-owned allowlists.
- Phase tokens distinguish authorization/project/output discovery, Door and complete-empty source/plan/output/metadata/planned/native checks, scenario assertions, disjoint ownership, Health, Locate and atomic result publication. They contain no dynamic text.
- Failure-class tokens distinguish controlled state/data/IO/overflow/unexpected rejection without serializing exception type names, messages, stack traces, inner exceptions or values.
- The runner rejects missing, duplicate, unknown or extra identity/path/content fields and prints only the allowlisted phase/code before failing. PASS schema and aggregate evidence remain unchanged except for an explicit version bump if required by the marker contract.
- The next local rerun must use a fresh disposable copy and empty outside-repository artifact directory against the final clean exact `main` SHA/DLL. It remains diagnostic until every existing P02 PASS assertion succeeds.

## Excluded scope

- No BricsCAD launch or rerun in this source batch; no private/customer/BLT artifact read.
- No attempt to infer a production clipping/build defect from the current coarse marker.
- No edits to production planning/building/Health/ownership/Level semantics, no P03-P12 work and no promotion to `LOCAL_PASS`.
- No GitHub Actions dispatch, release, signing, installer or package publication.

## Validation plan

- Merge this claim-only reservation before implementation and re-fetch active claims.
- Parse the PowerShell runner; run the focused P02, Curtain native/orchestration/P01-runtime/runtime-health, Level-Curtain and Direct Draw opening gates.
- Build the V25 `Release|x64` adapter against installed managed references without starting BricsCAD.
- Run `scripts/preflight.py`, `git diff --check`, privacy review and aggregate comparison only as proportionate source evidence; classify moving-main failures truthfully.
- Deliver by normal PR/squash merge without force-push or Actions, record exact SHAs, and leave P02/LOCAL-002 `PENDING_LOCAL` for the clean exact-SHA rerun.

## Coordination

The active Curtain Panel token-canonicality claim owns only `GeneratedCurtainPanelHealthService` plus its focused Core smoke; this lane consumes final integrated Health behavior and does not edit those files. Current Level and neighboring local work do not reserve this automation-only probe/runner. If a newer claim reserves the same P02 diagnostics or any expected file before implementation, stop and reconcile first.

## Completion condition

The claim is visible on `origin/main`; sanitized phase/failure diagnostics, runner validation and static privacy guard are merged; focused gates and V25 compile pass without BricsCAD; the claim is `COMPLETED`; and a final clean-main SHA is handed back for one local diagnostic rerun with P02 and overall LOCAL-002 still `PENDING_LOCAL`.
