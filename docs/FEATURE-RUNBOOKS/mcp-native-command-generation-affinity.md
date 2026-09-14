# MCP native-command database-generation affinity

## Invariant

A queued native BricsCAD command owns one exact managed `Document` and one exact non-zero native `Database.UnmanagedObject` generation from reservation through dispatch and lifecycle completion.

A managed wrapper is not sufficient identity. BricsCAD can replace/reload the native database behind the same wrapper, so native-command coordination must fail closed when that generation changes.

## Admission and dispatch

`McpCadMutationCoordinator` captures the active document's native database identity while arming the command barrier in application context. Before queueing, the reservation must still match the caller document, normalized command, and captured native generation.

Immediately before native dispatch it revalidates both `MdiActiveDocument` identity and the exact native generation. A drifted document/generation is rejected before `BeginDispatch()` and before `enqueue()`.

## Callback ownership

Command lifecycle callbacks are accepted only for the authoritative pending reservation, the exact managed sender, the expected command, and the captured native database generation. A callback observed after native database replacement is ignored and writer ownership remains quarantined rather than being released against successor state.

## Failure semantics

Generation drift is not retried or replayed. No second writer is opened, and no successor database state is mutated. Existing partial-subscription cleanup rules remain authoritative: unresolved native event detach keeps the writer quarantined.

Real same-wrapper database replacement, MDI timing, and native command execution require licensed BricsCAD runtime evidence and remain `LOCAL_ONLY / NO_RESULT` unless explicitly executed.
