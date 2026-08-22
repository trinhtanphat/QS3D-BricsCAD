# QS3D Model Health review UI — 2026-08-11

## Goal

Make the existing document-bound Model Health viewer practical on large projects without changing any diagnostic rule. The window remains a snapshot/review surface: diagnostic issues are produced before the window opens, and this UI only helps the user find, filter and locate those immutable results.

## Added review workflow

The Health header keeps the total Error / Warning / Info summary. A new triage band adds:

- case-insensitive search across issue `Code`, `ElementId` and `Message`;
- severity filter: All / Error / Warning / Info;
- visible-versus-total count;
- unchanged row double-click and `Định vị` behavior.

Filtering replaces only `IssueGrid.ItemsSource` with a filtered list derived from the issue snapshot captured at construction time. It does not rerun Health and does not change semantic/CAD state.

## Snapshot safety

The existing source-DWG and semantic snapshot contract remains authoritative. Before locate, Model Health still requires:

- the same active BricsCAD `Document`;
- the same `ProjectId`;
- the same `UpdatedUtc`;
- the same `ChangeVersion`;
- the same drawing fingerprint.

Activation also checks freshness. If the current project is absent/reloaded/changed, the window marks the snapshot stale, disables the issue grid plus search/severity controls, changes the visible-count badge to `STALE`, and instructs the user to close and rerun Health.

This prevents a filtered historical row from being located against a newer project state.

## Non-goals

This UI does not:

- add or modify Core diagnostic rules;
- repair an issue automatically;
- regenerate geometry or quantities;
- mutate project dirty state;
- dispatch arbitrary CAD commands;
- weaken active-DWG or stale-snapshot refusal behavior.

## Regression contract

`scripts/preflight-model-health-review-ui.py` statically guards:

- well-formed Model Health XAML;
- search, severity and visible-count controls;
- in-memory filtering over the constructor issue list;
- case-insensitive matching;
- current ProjectId/timestamp/change-version/fingerprint freshness checks;
- active-DWG requirement and locate callback;
- stale-snapshot filter lockout;
- absence of Health reruns, project creation and semantic mutation in the review window.

Native BricsCAD V25 rendering, keyboard/focus behavior, large issue-list responsiveness and HiDPI/text-clipping remain LOCAL_ONLY runtime evidence.