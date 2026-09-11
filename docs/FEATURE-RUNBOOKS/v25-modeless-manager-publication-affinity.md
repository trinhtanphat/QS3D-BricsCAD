# V25 modeless manager publication affinity

## Scope
C03 BricsCAD V25 UI/modeless lifecycle for Family Manager, Level Manager, and Zone Manager publication.

## Defect
Each command admitted the current `MdiActiveDocument` once, then could close/replace an existing owner and call `Application.ShowModelessWindow(...)` without proving that the same managed document wrapper and native database generation were still active. Native/modeless close and host publication may pump MDI work. A stale command could therefore destroy the valid owner, construct a replacement against a now-background document, or publish/announce that stale modeless generation.

## Contract
- Capture the native database identity from the admitted document.
- Require exact managed-wrapper plus native-database identity before destructive replacement.
- Revalidate immediately after any pending/published owner close and before replacement window construction.
- Revalidate after candidate ownership is installed and immediately before host publication.
- Revalidate after `ShowModelessWindow` returns and before transferring `_published` authority.
- On affinity drift, close the unpublished candidate best-effort and do not publish stale/background status.
- If native close fails and the candidate remains loaded, retain pending ownership rather than orphaning a modeless window.
- Gate success/error/editor publication to the same exact active document generation.
- Preserve existing pending/published close refusal semantics and never replay host publication after an irreversible show.

## Deterministic validation
- `python scripts/preflight-v25-modeless-manager-publication-affinity.py`
- aggregate discovered feature source guards
- repository preflight / deterministic smoke
- admitted-reference BricsCAD V25 compile in protected Shared CI

## Runtime classification
`REMOTE_SAFE`: source/static guard, aggregate preflight, deterministic smoke, admitted-reference V25 compilation.

`LOCAL_ONLY / NO_RESULT` until executed in licensed BricsCAD V25: MDI A→B/A→B→A switching while close or `ShowModelessWindow` pumps host work, disposed native wrapper timing, candidate close failure/residue, and duplicate-window/rooting observation.

Hosted CI or V25 compilation is not `LOCAL_PASS`.
