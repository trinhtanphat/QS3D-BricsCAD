# V25 structural post-commit document-generation affinity

Issue: #6525  
Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring

## Scope

This carrier hardens the post-commit UI phase of `QS3DBEAMREBAR3D` and `QS3DREBARSTIRRUP3D`. Native CAD/project mutation remains owned by the existing builders and mutation context. The change does not alter MCP, installer/release, or Quantity behavior.

## Invariants

- Capture the originating managed `Document` and its native `Database.UnmanagedObject` generation before the structural authoring workflow proceeds.
- After builder mutation returns, UI refresh is allowed only while the originating managed document is still the active document and still exposes the exact captured native database generation.
- Revalidate generation before palette refresh, after palette refresh before `Editor.Regen()`, and again before status/output publication. Host/UI pumping must not publish into a stale generation.
- Palette refresh must use the same exact-generation fence as Regen/status output.
- Once native/project mutation has committed, a later UI refresh failure does not roll back or relabel the committed mutation as failed.
- Post-commit UI failures are redacted to exception type only; raw exception messages are not emitted.
- A stale document/generation suppresses post-commit UI output rather than writing through a stale editor wrapper.

## REMOTE_SAFE qualification

Run the focused source preflight:

```text
python scripts/preflight-v25-structural-postcommit-document-affinity.py
```

The normal Shared CI must also pass auto-discovered feature guards, deterministic smoke/package-integrity stages, trusted/admitted BricsCAD V25 reference validation, and the locked-reference V25 plugin compile on the exact candidate head.

Remote/static GREEN proves the source contract and compile compatibility only. It does not prove licensed native host behavior.

## LOCAL_ONLY qualification

A licensed BricsCAD V25 runtime is required to qualify:

- MDI A -> B and A -> B -> A switching while a structural authoring command reaches post-commit UI synchronization;
- document reload/replacement where the managed document lifetime and native database generation diverge;
- palette refresh and `Editor.Regen()` host pumping/reentrancy;
- disposed/stale editor/database wrappers;
- visible palette/status behavior after successful commit and after generation invalidation.

Do not claim `LOCAL_PASS` without executing those scenarios in a real licensed V25 runtime. Missing local runtime does not invalidate REMOTE_SAFE source/compile evidence when repository policy allows merge on remote gates.
