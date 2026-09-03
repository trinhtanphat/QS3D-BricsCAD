# Rebar 3D Hub failed-publication lifecycle

Canonical carrier: Issue #5003 / Lane-Key `issue-5003`.

Runtime disposition: repository-safe implementation and hosted validation are REMOTE_SAFE. Licensed BricsCAD V25 native-window qualification remains LOCAL_ONLY and must be tied to one exact pushed SHA.

## Source contract

`QS3DREBARHUB` is intentionally active-document dynamic: the hub itself is not bound to one source DWG, while its click handlers resolve `MdiActiveDocument` at click time. The launcher must therefore preserve global single-instance semantics without losing ownership when BricsCAD host publication fails.

The launcher owns exactly two states:

- `_pending`: candidate created but not yet proven published;
- `_published`: loaded hub eligible for activation/reuse.

Before constructing a replacement, any pending owner must reach terminal close. A published but unloaded owner must also be terminally released. Cleanup failure is fail-closed: replacement construction is refused while authoritative ownership remains.

Publication order is fixed: construct -> register matching `Closed` release -> `_pending = window` -> `ShowModelessWindow` -> prove `IsLoaded` -> prove exact pending owner -> clear `_pending` -> assign `_published`. Raw host exception text is never surfaced to editor or Palette status.

## Deterministic repository validation

Run:

```text
python scripts/preflight-rebar-hub-publication.py
python scripts/preflight-rebar-hub.py
python scripts/preflight-document-bound-modeless-lifetime.py
```

Shared CI must also pass the auto-discovered feature-source guards and protected V25 compile for the exact candidate.

## LOCAL_ONLY licensed V25 matrix

Use the exact merged-or-candidate SHA prepared by the repository lane. Do not convert static evidence into `LOCAL_PASS`.

- RH01 normal launch: one hub opens and remains loaded.
- RH02 repeated invocation: loaded hub is activated; no second hub is constructed.
- RH03 close/reopen: closing the published hub releases ownership and one replacement opens.
- RH04 host-show exception: injected publication failure leaves no unowned visible candidate; retry is blocked if close is non-terminal.
- RH05 non-loaded host return: candidate remains authoritative until terminal cleanup; no duplicate construction on retry.
- RH06 cleanup failure/recovery: close exception/refusal fails closed; after terminal close, exactly one replacement may open.
- RH07 stale callback isolation: a delayed `Closed` callback from an older instance cannot clear a newer pending/published owner.
- RH08 active-DWG switching: with the same loaded hub, action dispatch follows `MdiActiveDocument` at click time and the hub itself remains unbound to a stale document wrapper.
- RH09 error redaction: host exception message sentinel does not appear in editor/Palette; only stable exception type/category text is shown.
- RH10 cold cleanup: after final close, no Rebar Hub window remains and subsequent launch produces one clean instance.

Record host build, exact SHA, scenario result and cleanup evidence. Licensed runtime PASS may be claimed only from an actual compatible BricsCAD execution.