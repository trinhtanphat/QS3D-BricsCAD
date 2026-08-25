# LOCAL-011 partial licensed-runtime handoff

- Carrier issue: `#3935`
- Lane-Key: `issue-3935`
- Canonical branch: `agent/local003/issue-3935-local011-v25-qualification`
- Tested source SHA: `5c9217d1f7b5701bb23cbf1f22d9cf97948b4077`
- Current source/static reconciliation SHA: `0f0169ab23e71ea382614a5f89f167542ae6bed0`
- Source-defect handoffs: `#3955` (generated mesh ownership) and `#3959` (bounded aggregate gate)
- Candidate source fixes: PR `#3960` and PR `#3961` (open; neither is part of the tested runtime SHA)
- Environment: Windows x64; BricsCAD Ultimate V25.2.10; V25 `Release/net48`
- Outcome: `PARTIAL_RUNTIME / BLOCKED_SOURCE_FIX`

## Result boundary

This lane has real licensed BricsCAD V25 evidence, but it does **not** claim `LOCAL_PASS`. Two canonical Curtain probes passed on the exact tested SHA, while a repeated Slab-mesh native-ownership failure blocks the representative Rebar mesh rows and therefore blocks the full 21-row LOCAL-011 result. The local worker did not modify production source after discovering that failure.

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

The P08/P09 markers retain their own narrow `LOCAL_002` qualification boundary and do not independently qualify LOCAL-011. They are used here only as exact-SHA native rollback and post-commit supporting evidence.

## Blocker and remaining scope

Issue `#3955` carries the sanitized native-ownership defect. PR `#3960` is the source owner's candidate fix for both Slab and Foundation append-time ownership, but it is not merged into the tested runtime SHA and therefore is not qualified here. Until a fix is integrated and rebuilt, these LOCAL-011 rows cannot pass: representative Rebar stale/exact replacement, malformed/duplicate Rebar metadata across the required mesh families, RebarMesh modeless stale Save, and the final complete generated-owner matrix.

Issue `#3959` independently carries the aggregate preflight timing defect, with PR `#3961` as the open candidate fix. LOCAL-011 reproduced the gate at 225.621 seconds from a clean detached `0f0169ab2` checkout, so the canonical runner cannot pass its required pre-runtime aggregate gate on that SHA even though the gate's content result is `PASS`.

The following independent work remains queued for the same lane: Curtain ownership P06, Undo/save/cold-reopen P11, multi-DWG/modeless P12, the full Door/Room/BBS/BQ/Recognition/Workspace/RebarMesh lifecycle matrix, and the canonical `scripts/run-local-v25-local-011.ps1` 21-row report. An aggregate preflight attempt at the earlier `10ced6bef` reconciliation SHA timed out in the full-tree orchestration-alias scan and reached the interchange selector only after the aggregate budget expired; after syncing `0f0169ab2`, the new aggregate output/timeout regressions and all 16 focused LOCAL-011 guards passed, while the clean focused timing above confirmed the remaining `#3959` blocker. Runtime use of the single licensed V25 host is coordinated with the other registered local lanes rather than run concurrently.

## Resume condition

After the `#3955` and `#3959` fixes are integrated, refresh the task branch to the newest intended exact SHA, pass the canonical aggregate from a clean checkout, rebuild the V25 adapter, rerun the generated-owner and modeless harness, then finish the canonical 21-row runner. A full `LOCAL_PASS` requires all 21 rows on one exact clean SHA; the P08/P09 results above must not be promoted beyond their recorded supporting scope.

Raw DWG copies, handles, nonces, hashes, scripts and machine paths remain under ignored local artifact roots and are intentionally not committed.
