# Work claim — Semantic Tag Remove PICKFIRST

- Status: `BLOCKED`
- Agent: `chatgpt-gpt56sol-semantic-tag-remove-pickfirst-20260811-2248`
- Registered: `2026-08-11T22:48:00+07:00`
- Source merged: `2026-08-11T22:51:00+07:00`
- Updated: `2026-08-11T22:54:00+07:00`
- Baseline main SHA: `5bccb132a11babd4d5b69ca13ecf6f34d9a374f0`
- Merge PR: `#511`
- Merge SHA: `b8caaa544d35254b80671cc9a6717f35f2aa0ec3`
- Priority: remove redundant repicking from explicit Semantic Tag removal

## Remote/source status — DONE

- `QS3DTAGREMOVE` now declares `CommandFlags.Modal | CommandFlags.UsePickSet`.
- Exactly one implied/PICKFIRST generated Semantic Tag or authoritative source is consumed directly.
- Zero implied selections preserve the explicit `Editor.GetEntity(...)` fallback.
- Multiple implied selections fail closed before canonical project binding/destructive removal.
- `ResolveTagOwner(...)` still validates generated owner slot and authoritative source uniqueness before removal.
- `SemanticTagRemovalService.Remove(...)` remains the only destructive implementation.
- No project bootstrap path was added.
- Static contract: `scripts/preflight-semantic-tag-remove-pickfirst.py`.
- Focused handoff detail: `docs/SEMANTIC-TAG-REMOVE-PICKFIRST-2026-08-11.md`.

## Only remaining blocker

`LOCAL-006 — native documentation objects` already owns the Semantic Tag remove runtime lifecycle, but its canonical scenario predates this new PICKFIRST path. Repo policy requires the canonical local inbox to carry the changed runtime delta.

The current connector has no bounded hunk/patch writer and `docs/LOCAL-AGENT-INBOX.md` is large/actively changing, so replacing the whole file from partial reads would risk deleting concurrent evidence. This claim therefore remains `BLOCKED` rather than falsely `COMPLETED`.

A patch-capable/local writer must update only `LOCAL-006`, preserving all existing evidence, to add:

- exactly one preselected generated Semantic Tag MText or authoritative source must skip the second remove picker;
- zero implied selection must preserve the explicit picker fallback;
- multiple implied selections must fail closed before canonical bind/destructive removal;
- unrelated generated QS3D output must remain rejected by owner-slot validation;
- ESC from explicit picker remains no-bind/no-removal/no semantic mutation;
- sidecar/project/document drift remains fail closed under the existing remove matrix;
- exact tested SHA/evidence must remain sanitized.

After that bounded canonical inbox edit, mark this claim `COMPLETED`. Live V25 execution remains `PENDING_LOCAL / DO_NOT_RETRY_REMOTE` until real evidence exists.

## Coordination

The source implementation surfaces are **released**; this blocked close-out reserves only the bounded `LOCAL-006` inbox delta and this claim status. Future Tag Remove source work may proceed under a new claim but must preserve/update this pending local matrix.
