# V25 Curtain Wall Hub modeless publication affinity

## Scope
This contract covers `QS3DCURTAIN` and its document-bound `CurtainWallWindow` pending/published lifecycle.

## Safety contract
- Bind command admission to one managed BricsCAD `Document` and its non-zero native database identity.
- Revalidate exact managed-wrapper + native database generation before replacement, after destructive close boundaries, before/after `ShowModelessWindow`, before pending promotion, and before status/editor publication.
- Preserve pending-first publication: the candidate is strongly owned before host show and promoted only when the same candidate/document/native generation remains authoritative.
- A close veto/failure must retain ownership of any still-loaded pending/published window. Never clear static ownership before terminal `!IsLoaded` proof.
- On a retained failure candidate, later invocations first attempt terminal cleanup rather than publishing a duplicate window.
- `Closed` must release only the exact owned instance.
- Exception text stays redacted from palette/editor UI.
- Do not replay native or project mutations after affinity loss.

## Deterministic validation
```powershell
python scripts/preflight-v25-curtainhub-modeless-publication-affinity.py
python scripts/preflight-document-bound-modeless-lifetime.py
```
Then run aggregate feature guards, deterministic smoke, and the admitted locked-reference BricsCAD V25 compile on the exact candidate SHA.

## Runtime classification
`REMOTE_SAFE`: source/static guards, deterministic smoke, admitted locked-reference V25 compile.

`LOCAL_ONLY / NO_RESULT`: licensed BricsCAD MDI A→B/A→B→A/no-document switching while native `Close()` / `ShowModelessWindow()` pumps; close veto/failure residue; disposed wrappers; duplicate/rooted window observation; visible activation/status behavior.

Hosted CI is never `LOCAL_PASS`.
