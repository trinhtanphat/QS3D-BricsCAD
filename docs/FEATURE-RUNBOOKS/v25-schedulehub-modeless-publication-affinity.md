# V25 Schedule Hub modeless publication affinity

## Scope
This contract covers `QS3DSCHEDULES`, launcher ownership, and the document-bound `ScheduleHubWindow` refresh/command/status lifecycle.

## Safety contract
- Capture one managed BricsCAD `Document` and its non-zero native database identity at command admission.
- Revalidate exact managed-wrapper reference plus native database identity before destructive owner replacement, after close boundaries, before construction/show, after `ShowModelessWindow`, and before user-visible launcher status publication.
- Publish a new Schedule Hub through pending-first exact ownership: reserve before host show; transfer to published ownership only after `IsLoaded` and exact-owner proof.
- A failed/vetoed close must retain the loaded pending or published owner. Do not clear static ownership until terminal close is observed.
- The `Closed` callback must capture a stable exact-owner token, not a mutable local that is nulled after publication.
- Reuse is allowed only for the same exact managed `Document` and native database generation.
- The bound `ScheduleHubWindow` retains its admission native database identity and revalidates that same generation before refresh and command dispatch. MDI wrapper identity alone is not sufficient.
- Local window status may explain a stale/background source drawing, but global `PaletteCoordinator` publication is allowed only while the exact bound document/native generation is active.
- Redact native exception details from launcher, local window, editor, and global palette UI.
- Do not retry/replay native or project mutations after affinity failure. `SendStringToExecute` is dispatched only after exact bound-generation admission.

## Deterministic validation
Run:

```powershell
python scripts/preflight-v25-schedulehub-modeless-publication-affinity.py
python scripts/preflight-document-bound-modeless-lifetime.py
```

Then run aggregate feature source guards, deterministic smoke, and the admitted locked-reference BricsCAD V25 compile on the exact candidate SHA.

## Runtime classification
`REMOTE_SAFE`: source/static guards, deterministic smoke, and locked/admitted-reference V25 compilation.

`LOCAL_ONLY / NO_RESULT`: licensed BricsCAD MDI A→B/A→B→A/no-document switching while `Close()`, `ShowModelessWindow()`, or `SendStringToExecute` pumps host work; native close veto/failure residue; disposed wrapper timing; duplicate/rooted window observation; visible activation/status behavior.

Hosted CI must never be reported as `LOCAL_PASS`.
