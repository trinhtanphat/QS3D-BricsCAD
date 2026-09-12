# V25 BLT legacy-import document-generation affinity

## Scope

This carrier hardens `QS3DBLTIMPORT` in `src/QS3D.BricsCAD.V25/BltLegacyCommands.cs`. It does not change BLT evidence arithmetic, Quantity engine behavior, MCP transport/runtime, installer, or release flows.

## Failure mode

`QS3DBLTIMPORT` captures the active managed `Document`, then crosses `DrawingUnitWorkflow.EnsureResolved(...)`, an interactive/host-pumping boundary. The active MDI document can change, or the managed wrapper can survive while its native `Database` generation is replaced. Continuing to scan the captured database or hand snapshots into `SemanticCaptureService.CaptureSnapshot(...)` after that point can mutate semantic/project state for a stale or background drawing.

Success/error publication can also target the superseded document generation. A stale UI publication must never make a completed semantic mutation look like a CAD/project failure.

## Production contract

At command entry, capture the exact managed `Document` and a non-zero `document.Database.UnmanagedObject` identity. Treat both as the generation authority for the operation.

Revalidate exact managed/native generation:

1. before crossing into the workflow;
2. immediately after unit-resolution host pumping and before scanning;
3. after the scan before semantic mutation begins;
4. immediately before every `SemanticCaptureService.CaptureSnapshot(...)` mutation handoff;
5. after each successful semantic capture before continuing;
6. before success/status publication.

Generation loss fails closed. Do not continue semantic mutation against a stale/background drawing. Stale diagnostics are suppressed.

Public exception publication is redacted to the exception type. Do not publish raw exception messages from this boundary.

Status/output after successful semantic mutation is best-effort. Failure to write UI output must not retroactively report the already-completed semantic/project mutation as failed.

## REMOTE_SAFE validation

Valid hosted evidence includes:

1. `python scripts/preflight-v25-blt-legacy-import-document-affinity.py`;
2. repository generic and auto-discovered feature source guards;
3. V25 package-integrity checks and deterministic smoke tests;
4. trusted/admitted locked-reference BricsCAD V25 plugin compilation;
5. static review of generation fences, persistence handoff, exception redaction, and UI publication ordering.

Remote compile/preflight evidence validates source/API compatibility only. It is not a licensed BricsCAD runtime PASS.

## LOCAL_ONLY qualification

The following remain `LOCAL_ONLY / NO_RESULT` until exercised in a real licensed BricsCAD V25 runtime:

- MDI A→B and A→B→A switching during unit resolution or semantic persistence;
- same managed `Document` wrapper with native database reload/replacement;
- disposed document/database wrappers at generation checks;
- host pumping/reentrancy inside semantic/project persistence;
- document-generation change between semantic commit and output publication;
- visible project/palette/model behavior after import.

Never infer `LOCAL_PASS` from Shared/Hybrid CI or admitted-reference V25 compilation.

## Self-review checklist

- Exact managed-document and native-database generation affinity.
- Unit-resolution boundary fenced before subsequent database scan.
- No semantic mutation handoff after generation loss.
- Revalidation around each repeated semantic capture.
- Existing semantic/project transaction ownership preserved.
- Source legacy CAD remains read-only.
- Stale success/error publication suppressed.
- Public exception text redacted.
- UI/output failure cannot turn a completed mutation into false failure.
- No new subscriptions, modeless roots, or disposal obligations.
- V25 API compatibility verified with admitted locked references.
