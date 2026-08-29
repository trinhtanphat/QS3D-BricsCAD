# Issue #4598 — Workspace PropertyList first-layout containment

- **Status:** ACTIVE / SOURCE FIX IN PROGRESS / LICENSED RERUN PENDING
- **Lane-Key:** `issue-4598`
- **Canonical owner/session:** `account:trinhtanphat|session:01a046ab-96a9-7702-ab82-79220b0d6ad5`
- **Canonical carrier:** `agent/trinhtanphat-01a046ab/issue-4598-property-list-startup`
- **Baseline:** `origin/main@fb6012eaba04a208beaf9ed715040d963d6d2a1b`
- **Related runtime defect:** #4173; this lane is a narrow successor split because the active historical ModelTree carrier is already owned and the licensed post-#4268 reproduction identifies a separate grouped `PropertyList` first-layout candidate.

## Scope

Contain only the grouped `WorkspacePanel.PropertyList` before the first BricsCAD WPF layout:

1. pin local Standard/non-virtualized/physical-scrolling values after `InitializeComponent`;
2. apply the values before `BindViewModel()` adds the `PropertyGroupDescription`;
3. add a source guard that rejects late-layout hooks and global Theme policy changes.

Excluded: ModelTree/RoomFinish source ownership, raft workflow behavior, generic Foundation/Móng đơn Add routing, package/version work, and unrelated draft #4382 changes.

## Same-candidate V26 build prerequisite

Current `origin/main` V26 Release build reached a compiler `CS0649` hard stop because the shared
`UpdateCenterWindow` reads V25 preview scheduling fields that V26 intentionally never assigns.
No active reservation owned those symbols. This carrier supplies only explicit default state
(`false` / `null`) so the V26 compilation preserves the existing disabled-preview behavior; no
updater flow or package contract is broadened.

## Runtime observation that triggered this lane

On licensed BricsCAD V25.2.10, clean exact `44dad134cc7021973c4cdc32eb55558e8350de99` completed product and probe `NETLOAD` but terminated before the Workspace baseline. Windows .NET Runtime Event 1026 recorded `System.InvalidOperationException` at `VirtualizingStackPanel.SetVirtualizationState -> GetOwners -> MeasureOverrideImpl`. The Móng đơn matrix did not begin; this is not a `LOCAL_PASS`.

## Validation plan

- prove the dedicated preflight red before the implementation, then green with the existing ModelTree/Room-finish guards;
- run Core smoke plus V25/V26 Release builds and automatic exact-head CI;
- build a package from the exact pushed source SHA;
- reserve the shared licensed host and use physical mouse/UI interaction in V25 and V26, including Workspace baseline, Móng đơn dedicated dialog, native placement/3D, edit/regeneration, save and cold reopen.
