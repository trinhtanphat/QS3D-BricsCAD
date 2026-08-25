# LOCAL-011 partial licensed-runtime handoff

- Carrier issue: `#3935`
- Lane-Key: `issue-3935`
- Canonical branch: `agent/local003/issue-3935-local011-v25-qualification`
- Tested source SHA: `5c9217d1f7b5701bb23cbf1f22d9cf97948b4077`
- Current source/static reconciliation SHA: `20d029859ddd69ad25b8db40111b297bbfaa374a`
- Resolved source blocker: `#3955` via merged PR `#3960`
- Resolved source blocker: `#3959` via merged PR `#3961`
- Environment: Windows x64; BricsCAD Ultimate V25.2.10; V25 `Release/net48`
- Outcome: `PARTIAL_RUNTIME / READY_RUNTIME_RETRY`

## Result boundary

This lane has real licensed BricsCAD V25 evidence, but it does **not** claim `LOCAL_PASS`. Two canonical Curtain probes passed on the earlier exact tested SHA, while a repeated Slab-mesh native-ownership failure prevented the representative Rebar mesh rows and therefore prevented a full 21-row LOCAL-011 result. The source fix is now merged and the exact merged SHA builds and passes static gates, but the repaired path has not yet been rerun in licensed V25. The local worker did not modify production source after discovering the failure.

## Exact-SHA evidence

| Gate or runtime scope | Result | Sanitized proof |
| --- | --- | --- |
| V25 adapter and ignored diagnostic-harness build | `PASS` | Both x64 builds completed with 0 warnings and 0 errors. |
| Curtain P08 seven-boundary atomic-failure probe | `PASS` | Seven injected phases; baseline 63 generated objects; exact valid replacement 87 objects; whole-batch semantic/native state and source geometry preserved for every injected abort; old valid sets removed and new sets complete; health issues 0. |
| Curtain P09 post-commit warning probe | `PASS` | Two injected post-commit failures; committed fingerprint and UI replacements survived; old sets removed and new sets complete; baseline/clean/fingerprint/UI generated counts 30/34/34/38; UI health issues 0. |
| Sequential generated-owner diagnostic | `FAIL` | The diagnostic reached `rebar_slab_mesh` after the earlier owner cases, then the initial valid Slab mesh build failed while native ownership was being attached. Earlier cases from this failed aggregate phase are not promoted to independent LOCAL-011 PASS rows. |
| Slab mesh fresh native ownership | `FAIL` | `GeneratedRebarNativeOwnershipService.MarkFreshGeneratedHandles` re-resolved a just-appended live entity, but BricsCAD V25 reported it as not new in the current transaction and the fail-closed guard rejected it. Reproduced in two consecutive licensed runs; raw handle omitted. |
| Process/private-state cleanup for P08 and P09 | `PASS` | Each runner verified owned-process exit, script cleanup, drawing-lock cleanup, sidecar/backup absence and restoration of the disposable drawing copy. |
| Focused LOCAL-011 source guards on `0f0169ab2` | `PASS_STATIC` | 16 focused guards passed for runner/failure-matrix contracts, existing-project mutation context, modeless detached/document/project lifetime, BBS/BQ safety, Rebar Mesh stale-save atomicity, and Grid/Curtain/Rebar exact-set ownership. Static evidence is not licensed-runtime evidence. |
| Clean-checkout orchestration-alias gate on `0f0169ab2` | `BLOCKED_TIMING` | The focused gate returned content `PASS` in 225.621 seconds, exceeding the aggregate child limit of 180 seconds. The immediately following interchange selector returned `PASS` in 41.902 seconds. Independent evidence was added to source issue `#3959`; candidate fix is PR `#3961`. |
| Post-merge orchestration-alias gate on `4129f92bc` | `PASS_STATIC` | After PR `#3961` merged, the clean focused gate passed in 78.580 seconds (below 180 seconds) and its tracked-only regression passed in 1.742 seconds. |
| Canonical aggregate on `273f172e1` | `PASS_STATIC` | Clean detached checkout: `scripts/preflight.py` passed, then all 1040 discovered feature gates passed in 352.470 seconds. No BricsCAD process was launched. |
| Exact merged source/build on `20d029859` | `PASS_STATIC_BUILD` | Clean detached checkout: root preflight passed; installed-reference V25 `Release|x64` and the ignored LOCAL-011 harness both built with 0 warnings / 0 errors. Exact plugin SHA-256: `9CBE1EEA1BA9DD8528152B7EE06E700B057924BD3F99A0B3D31E9EFB00C1522E`. No BricsCAD process was launched. |
| Generated-ownership fix guards on `20d029859` | `PASS_STATIC` | Foundation mesh, live appended-entity ownership, polygonal Slab adapter and all eight Rebar replacement-family ownership guards passed. |
| Canonical aggregate on `20d029859` | `PASS_STATIC` | Clean detached checkout: all 1042 discovered feature gates passed in 195.228 seconds. No BricsCAD process was launched. |

The P08/P09 markers retain their own narrow `LOCAL_002` qualification boundary and do not independently qualify LOCAL-011. They are used here only as exact-SHA native rollback and post-commit supporting evidence.

## Resolved blockers and remaining scope

Issue `#3955` carried the sanitized native-ownership defect. PR `#3960` is merged at `20d029859ddd69ad25b8db40111b297bbfaa374a`; the exact merged adapter and LOCAL-011 harness compile cleanly and the focused ownership guards pass. This resolves the source-side blocker only. Representative Rebar stale/exact replacement, malformed/duplicate Rebar metadata across the required mesh families, RebarMesh modeless stale Save, and the final complete generated-owner matrix still require licensed runtime proof on that exact SHA.

Issue `#3959` carried the independently reproduced aggregate preflight timing defect. PR `#3961` is now merged; LOCAL-011 verified the fixed focused gate below the production timeout and then passed the full clean aggregate on `273f172e1`. This blocker is resolved, but those static results do not qualify the licensed runtime rows.

The following independent work remains queued for the same lane: Curtain ownership P06, Undo/save/cold-reopen P11, multi-DWG/modeless P12, the full Door/Room/BBS/BQ/Recognition/Workspace/RebarMesh lifecycle matrix, and the canonical `scripts/run-local-v25-local-011.ps1` 21-row report. The required source fix, build and pre-runtime aggregate are now healthy; runtime work remains paused only until the currently registered single-host owner sends literal `HOST_RELEASED` after cleanup.

## Resume condition

After literal `HOST_RELEASED`, independently verify stable zero V25 PIDs and restored loader state, then rerun the generated-owner/modeless harness and Curtain P06/P08/P09/P11/P12 probes against exact source `20d029859ddd69ad25b8db40111b297bbfaa374a` before finishing the canonical 21-row runner. A full `LOCAL_PASS` requires all 21 rows on one exact clean SHA; the earlier P08/P09 results above must not be promoted beyond their recorded supporting scope.

Raw DWG copies, handles, nonces, hashes, scripts and machine paths remain under ignored local artifact roots and are intentionally not committed.
