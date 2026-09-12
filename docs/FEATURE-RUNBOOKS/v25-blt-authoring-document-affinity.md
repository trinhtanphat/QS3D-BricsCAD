# V25 BLT structural-authoring document-generation affinity

## Scope

This carrier hardens the interactive BLT structural-authoring commands in `src/QS3D.BricsCAD.V25/BltToolCommands.cs` that retain native `ObjectId` values across BricsCAD editor prompts before mutating the database:

- `QS3DBLTPILELOWER`
- `QS3DBLTLEANCONCRETE`
- `QS3DBLTFOUNDATIONEXCAVATE`

MCP endpoint/transport behavior is not part of this feature contract.

## Failure mode

An editor prompt is a host-pumping boundary. The active MDI document can change, or the managed `Document` can survive while its native `Database` generation is replaced. Retained `ObjectId` values belong to the originating native database generation and must never be handed to a write transaction after that generation stops being authoritative.

A second failure mode exists after a successful native transaction commit: `Editor.Regen()` or status publication can encounter a stale/disposed UI context. That UI failure must not be surfaced as if the committed CAD mutation failed or was rolled back.

## Production contract

At authoring-command entry, capture both the exact managed `Document` instance and its non-zero `Database.UnmanagedObject` identity. Revalidate both against `Application.DocumentManager.MdiActiveDocument` after interactive prompts and immediately before the retained-`ObjectId` native mutation handoff. Fail closed with the fixed stale-generation diagnostic if either identity changes.

After transaction commit, Regen/status publication is best-effort and generation-safe: revalidate before Regen and again before publishing status. Stale or disposed UI context suppresses publication; it does not retroactively fail a committed transaction.

The generic BLT `Run` helper remains available for non-authoring commands. Authoring-specific generation fencing is isolated in `RunAuthoring` so MCP transport/configuration semantics are not used as the authority for CAD generation ownership.

Public exception publication is type-only/redacted. Do not expose raw exception messages from the authoring command boundary.

## REMOTE_SAFE validation

The following evidence is valid without a licensed BricsCAD process:

1. `python scripts/preflight-v25-blt-authoring-document-affinity.py`
2. Repository generic/feature preflights and deterministic source/package smoke.
3. BricsCAD V25 plugin compilation against the repository-admitted locked reference generation.
4. Static review of transaction ownership, stale-generation fences, post-commit UI ordering and exception redaction.

A green remote compile/preflight is evidence for source/API compatibility only. It is not a native runtime PASS.

## LOCAL_ONLY qualification

The following require a real licensed BricsCAD V25 runtime and must remain `LOCAL_ONLY / NO_RESULT` until actually exercised there:

- MDI A→B and A→B→A switching during `GetEntity` / numeric prompts.
- Same managed `Document` wrapper with native database reload/replacement.
- Disposed native database/document wrappers at generation checks.
- Document switch/reload while acquiring `LockDocument()`.
- Post-commit `Editor.Regen()` host pumping and stale UI publication.
- Visible geometry/result validation and retained-`ObjectId` behavior in BricsCAD.

Never infer `LOCAL_PASS` from Shared/Hybrid CI or admitted-reference V25 compilation.

## Self-review checklist

- Exact managed-document and native-database generation affinity.
- Revalidation after host-pumping prompts and immediately before native mutation.
- Retained `ObjectId` values never cross a stale generation boundary.
- Transaction commit/rollback ownership unchanged.
- Post-commit Regen/status cannot turn a committed CAD mutation into false failure.
- Stale-generation diagnostics are suppressed rather than sent to a different drawing.
- Public exceptions are redacted.
- No new subscriptions, modeless roots or disposal obligations are introduced.
- V25 API compatibility is validated by admitted-reference compile.
