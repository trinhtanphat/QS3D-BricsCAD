# V25 Door/Opening Schedule modeless publication affinity

## Scope

This runbook qualifies the source-side lifetime and document-generation contract for `QS3DDOORSCHEDULE` and its `DoorOpeningScheduleWindow` modeless surface.

## Root cause

The older launcher retained one published window through `_window`, `_document`, and `_nativeDatabaseIdentity`, but it did not reserve an unpublished candidate before `Application.ShowModelessWindow(...)` and did not revalidate the exact managed `Document` plus native database generation after destructive close/show boundaries. A close failure/veto or MDI switch while host/UI work pumped could therefore leave a loaded candidate without durable ownership or publish a window for a stale document generation.

The window itself retained only the managed `Document`. Its active-document test did not bind the admitted native database identity, and error handlers could publish raw exception messages into local/global UI.

## Source contract

The launcher now:

- captures the exact managed `Document` and non-zero native database identity;
- rejects background/stale generations before construction;
- uses pending-first ownership before any modeless host show;
- binds `Closed` to an immutable exact-owner token;
- retains a loaded pending/published owner when native `Close()` fails or is vetoed;
- revalidates exact managed/native generation after destructive close preparation and after `ShowModelessWindow`;
- publishes only a loaded candidate that still owns the exact pending slot;
- releases ownership only after terminal unload;
- suppresses stale/background global status and redacts native exception details.

The window now binds the same native database generation at construction and requires that exact generation for refresh/export/global-status operations. The XLSX dialog remains followed by a fresh `BuildCurrentRows` authority check, so dialog message pumping cannot silently authorize export from a newly background/stale document generation.

## REMOTE_SAFE validation

Run the focused source guard:

```text
python scripts/preflight-v25-door-opening-schedule-modeless-publication-affinity.py
```

Then run the repository aggregate feature guards, deterministic smoke tests, trusted/admitted BricsCAD V25 reference validation, and the V25 plugin build against the locked reference generation through normal Shared CI.

REMOTE_SAFE evidence proves source/static ordering, deterministic contracts, and compile compatibility only.

## LOCAL_ONLY licensed qualification

Real BricsCAD V25 qualification remains `LOCAL_ONLY / NO_RESULT` until executed on an exact candidate. Exercise at minimum:

1. A opens Door/Opening Schedule; invoking again reuses the exact owner without duplicate windows.
2. A → B while the modeless surface is open; stale A refresh/export/global-status actions fail closed.
3. A → B → A with the original managed/native generation retained; only the exact admitted generation may resume.
4. Close/replace while native `Close()` pumps messages; no stale publication or second owner appears.
5. Native close veto/failure leaves the loaded owner retained and blocks duplicate creation until terminal close.
6. Switch/close during `ShowModelessWindow`; unpublished candidates are either terminally closed/released or retained as pending residue, never forgotten.
7. Save-file dialog plus MDI switching before export; post-dialog generation validation prevents stale export.
8. Document close/dispose and native-wrapper access failure; callbacks/status paths fail closed without leaking exception details.
9. Normal refresh/export behavior remains correct for the bound active document and no implicit project is created.

Hosted CI, static guards, and V25 compilation must never be recorded as `LOCAL_PASS`.
