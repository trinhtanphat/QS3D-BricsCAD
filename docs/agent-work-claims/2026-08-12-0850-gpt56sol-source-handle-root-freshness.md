# Work claim — Source Handle root freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-source-handle-root-freshness-20260812-0850`
- Registered: `2026-08-12T08:50:00+07:00`
- Baseline main SHA: `953bc91e46bfbcbb2e089080e1d647f6529c74ac`

## Defect

`SourceHandleResolver.Resolve` builds its project element index before it enumerates caller-provided root IDs. A lazy root sequence can change `ProjectState` while yielding an ID, leaving the resolver to query a stale index and silently omit a valid Locate root.

## Reserved scope

- `src/QS3D.Core/Services/SourceHandleResolver.cs`
- focused Core smoke regression
- this claim file

Guard `ProjectState.ChangeVersion` across root-ID materialization and build the element index only after stable input materialization. Preserve existing input bounds, dependency validation, handle precedence and ordinary Locate behavior. Do not change BricsCAD UI/runtime or ownership semantics.

## Completion

Complete only after source + focused regression are on current `main`, exact SHAs are recorded here, and this claim is marked `COMPLETED`. No GitHub Actions or BricsCAD runtime qualification.