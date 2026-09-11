# V25 Audit Log modeless publication affinity

## Scope
This contract covers `QS3DAUDIT` modeless Audit Log ownership and publication. The Audit Log is document-bound and may survive command completion, so every reuse/replacement decision must remain tied to one exact managed BricsCAD `Document` wrapper and its live native database generation.

## Safety contract
- Capture the command-entry managed `Document` and its non-zero native database identity before modeless lifecycle work.
- Revalidate exact managed-wrapper reference plus native database identity after any prior-candidate cleanup, after published-window replacement, before same-document reuse, before candidate construction/show, after `ShowModelessWindow`, and before status publication.
- Same native database identity alone is insufficient for reuse: the published owner also retains a weak exact managed-Document wrapper and must match it by reference.
- Preserve the existing unpublished/publication-in-flight/cleanup-in-flight bookkeeping. A failed or vetoed candidate close must remain remembered; never clear residue merely to allow another window.
- If document affinity drifts before authoritative publication, close the unpublished candidate through the existing residue-aware path and publish no stale success/error status.
- Resolve the read-only project/audit-count snapshot only while the admitted document generation remains authoritative; never create a project as a side effect of opening Audit Log.
- Terminal published-window release clears both native database identity and managed-wrapper ownership.
- Do not replay or retry native/modeless publication after affinity failure.

## Deterministic validation
Run:

```powershell
python scripts/preflight-v25-auditlog-modeless-publication-affinity.py
python scripts/preflight-document-bound-modeless-lifetime.py
python scripts/preflight-manager-modeless-loaded-publication-admission.py
```

Then run aggregate feature guards, deterministic smoke, package-integrity validation, and the admitted locked-reference BricsCAD V25 plugin compile on the exact candidate head.

## Runtime boundary
`REMOTE_SAFE`: source/static guards, deterministic managed smoke, package-integrity checks, and locked-reference V25 compilation.

`LOCAL_ONLY / NO_RESULT`: real licensed BricsCAD MDI A→B→A/no-document switching while `Close()` or `ShowModelessWindow` pumps host work, disposed native wrapper timing, actual close-veto/failure residue, duplicate-window/rooting observation, visible modeless behavior, and final UI acceptance.
