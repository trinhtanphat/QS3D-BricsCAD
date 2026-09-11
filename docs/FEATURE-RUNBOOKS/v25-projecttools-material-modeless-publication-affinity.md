# V25 Project Tools / Material Catalog modeless publication affinity

## Scope
This contract covers `QS3DPROJECTTOOLS` and `QS3DMATERIALS` modeless publication. Both surfaces are document-bound; Material Catalog also binds one existing canonical project snapshot.

## Safety contract
- Capture one managed BricsCAD `Document` and its non-zero native database identity at command admission.
- Revalidate exact managed-wrapper reference plus native database identity before destructive owner replacement, after owner-close boundaries, before window construction/publication, and after `ShowModelessWindow` before authority transfer.
- A stale/background generation must never replace the current published owner or publish success/error UI for another drawing.
- Material Catalog must preserve its existing non-creating project lookup and pending-first exact-owner publication semantics.
- If affinity drifts after a candidate is reserved or shown, close the unpublished candidate best-effort; clear pending ownership only after terminal close so a loaded residue cannot be silently forgotten.
- Preserve same-document activation, loaded-window admission, Closed-handler release, close-veto behavior, redacted failure UI, and Project Tools read-only behavior.
- Do not retry/replay native or project mutation after affinity failure.

## Deterministic validation
```powershell
python scripts/preflight-v25-projecttools-material-modeless-publication-affinity.py
python scripts/preflight-material-projecttools-manager-single-instance-veto-safe.py
python scripts/preflight-manager-modeless-loaded-publication-admission.py
python scripts/preflight-document-bound-modeless-lifetime.py
python scripts/preflight-material-catalog-open-project-lifecycle.py
python scripts/preflight-material-catalog-project-lifecycle.py
python scripts/preflight-material-catalog-publication-redaction.py
python scripts/preflight-modeless-editor-document-lifetime.py
python scripts/preflight-project-tools.py
```
Then run aggregate source guards / deterministic smoke and the admitted locked-reference V25 compile on the exact candidate.

## Runtime boundary
`REMOTE_SAFE`: source/static guards, managed smoke and locked-reference V25 compilation.

`LOCAL_ONLY / NO_RESULT`: real licensed BricsCAD MDI A→B→A/no-document switching while Close/window construction/`ShowModelessWindow` pumps host work, disposed native wrapper timing, close-veto/residue observation, duplicate modeless rooting and visible status behavior.
