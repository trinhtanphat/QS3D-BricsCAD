# BricsCAD V25 Direct Draw document-generation affinity

Issue: #6818  
Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring  
Ownership-Key: `v25-direct-draw-document-generation-affinity-v1`

## Failure mode

The P0 Direct Draw commands (`QS3DDRAWWALL*`, `QS3DDRAWBEAM*`, `QS3DDRAWSLAB*`, and `QS3DDRAWCOLUMN*`) keep interaction state across BricsCAD `Editor.GetPoint` / `GetDouble` calls. Those calls can pump the host UI. A managed `Document` wrapper can remain reference-equal while the native `Database` behind it is replaced or reloaded.

Before #6818, prompt freshness validated the managed active document, Model Space, drawing units, and UCS, but did not pin `Document.Database.UnmanagedObject`. Stale points/UCS/defaults could therefore survive a same-wrapper native database replacement and be handed to a successor database generation.

## Contract

The P0 command boundary now captures a non-zero native database identity before prompt-bearing work. The exact managed document and native identity are revalidated:

- after point prompt returns and after the final point sequence;
- after advanced numeric prompts before authoring continues;
- before project mutation and source creation;
- after `LockDocument()` and before source transaction commit;
- around semantic capture/native build boundaries where stale `ObjectId` or generated-handle use would otherwise cross generations.

A generation mismatch fails closed before further native/semantic mutation. Rollback CAD cleanup, implied-selection cleanup, and `ProjectContextCoordinator.Forget(document)` are generation-bound so a stale operation cannot mutate or clear successor state.

Post-commit UI is best-effort. Palette refresh, selection, regen, success/warning status, and operation-failure publication are suppressed when the captured generation is no longer current. A successful native/semantic authoring result is not reported as rolled back merely because UI affinity changed after commit.

`DirectDrawRepeatedCommands.cs` remains a separate ownership lane. `ExecuteDirect` keeps a default generation parameter so the repeated-command carrier remains source compatible; #6818 does not claim or edit the repeated-command path.

## REMOTE_SAFE verification

Run the focused source guard:

```text
python scripts/preflight-v25-direct-draw-document-generation.py
```

Then run the repository aggregate feature-source guard, deterministic smoke suite, package-integrity checks, and the admitted/locked-reference BricsCAD V25 plugin build through the canonical Shared/Hybrid CI workflow on the exact candidate SHA.

Remote/static GREEN proves source contracts, deterministic behavior covered by the repository harness, policy gates, and compilation against admitted V25 references. It does not prove licensed native BricsCAD runtime behavior.

## LOCAL_ONLY licensed BricsCAD matrix

Classify these as `LOCAL_ONLY` unless they are actually executed in a licensed BricsCAD V25 runtime:

1. Start a P0 Direct Draw point sequence in DWG A, then switch to DWG B while the prompt is active. The stale command must not create CAD, semantic elements, solids, selection, regen, or status in B.
2. Keep the same managed `Document` wrapper but reload/replace its native database while a `GetPoint` or `GetDouble` prompt is active. The stale generation must fail closed before source/project/native mutation.
3. Replace the native database between source-lock acquisition and transaction commit. The post-lock/pre-commit generation fences must prevent committing into a successor generation.
4. Change document/native generation after successful native/semantic authoring but before palette/selection/regen finalization. The committed work stays committed; successor UI receives no stale selection/status/regen and no false failure is reported.
5. Force an authoring exception while generation is still current. Existing CAD/project rollback semantics must remain intact. If generation has already changed, rollback must not erase CAD or forget project state in the successor generation.

Do not record `LOCAL_PASS` without actual evidence from those licensed runtime scenarios.