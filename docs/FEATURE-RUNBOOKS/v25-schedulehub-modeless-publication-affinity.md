# V25 Schedule Hub modeless publication affinity

## Scope
This contract covers `QS3DSCHEDULES` and the document-bound `ScheduleHubWindow` publication lifecycle.

## Safety contract
- Capture one managed BricsCAD `Document` and its non-zero native database identity at command admission.
- Revalidate exact managed-wrapper reference plus native database identity before destructive owner replacement, after close boundaries, before construction/show, after `ShowModelessWindow`, and before user-visible status publication.
- Publish a new Schedule Hub through pending-first exact ownership: reserve before host show; transfer to published ownership only after `IsLoaded` and exact-owner proof.
- A failed/vetoed close must retain the loaded pending or published owner. Do not clear static ownership until terminal close is observed.
- The `Closed` callback must capture a stable exact-owner token, not a mutable local that is nulled after publication.
- Reuse is allowed only for the same exact managed `Document` and native database generation.
- Redact native exception details from palette/editor UI; stale/background generations must not publish failure or success status.
- Do not retry or replay native/project mutations after affinity failure.

## Deterministic validation
Run:

```powershell
python scripts/preflight-v25-schedulehub-modeless-publication-affinity.py
python scripts/preflight-document-bound-modeless-lifetime.py
```

Then run aggregate feature source guards, deterministic smoke, and the admitted locked-reference BricsCAD V25 compile on the exact candidate SHA.

## Runtime classification
`REMOTE_SAFE`: source/static guards, deterministic smoke, and locked/admitted-reference V25 compilation.

`LOCAL_ONLY / NO_RESULT`: licensed BricsCAD MDI A→B/A→B→A/no-document switching while `Close()` or `ShowModelessWindow()` pumps host work; native close veto/failure residue; disposed wrapper timing; duplicate/rooted window observation; visible activation/status behavior.

Hosted CI must never be reported as `LOCAL_PASS`.
