# Work claim — Grid Annotation numeric handle identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-grid-annotation-handle-identity`
- Registered: `2026-08-12T13:47:00+07:00`
- Baseline main SHA: `11c267129c5ee75bfbb686e63f0e2fb36d99658f`
- Priority: P0 — generated Grid annotation health must use numeric CAD Handle identity consistently.
- Task Key: `CORE-GRID-ANNOTATION-HANDLE-IDENTITY`

## Confirmed defect

`GeneratedGridAnnotationHealthService` validates each non-empty token as hexadecimal, but then keys duplicate/count and SourceHandles checks by trimmed raw text. Therefore provider-valid aliases such as `A` and `0A` are treated as different generated entities even though BricsCAD resolves them to the same numeric CAD Handle. This can hide duplicates, inflate the expected six-entity count, and miss generated-vs-source alias collisions.

The shared `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` already defines the reviewed positive numeric CAD-handle identity used elsewhere.

## Non-overlap check

Latest exact history search returned no Grid Annotation numeric-handle identity lane. Curtain Frame/Panel, Beam Stirrup, Slab/Wall/Foundation Mesh and semantic SourceHandle work are separate claims and remain out of scope.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs`
- one focused Core smoke regression
- this claim file

Do not change handle textual validity rules, ownership metadata, sizing metadata, Grid naming, native annotation generation, command wrappers, or BricsCAD runtime code.

## Intended contract

- Only after a token passes the existing hexadecimal validity rule, use `GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(...)` for local duplicate/count and SourceHandles identity checks.
- `A` and `0A` count as one generated CAD object.
- Existing empty/invalid-token and whitespace canonicality diagnostics remain unchanged.
- Existing `0x` validity behavior remains unchanged.
- Distinct numeric handles remain distinct.

## Completion condition

Focused regression proves numeric aliases fail visible as duplicate/count/source conflicts without changing invalid/canonical metadata behavior; merged source + smoke are read back from current `main`, ancestry is verified, and this claim is closed with exact commit SHAs.
