# Work claim — Auto Room canonical numeric alias fixture

- Status: `ACTIVE`
- Agent: `codex-autoroom-numeric-alias-fixture-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T21:55:00+07:00`
- Baseline main SHA: `7eaff2079e28dc9137738810cc065c4db334a654`
- Priority: unblock full deterministic Core smoke after the completed #982 Auto Room fallback regression used a non-CAD synthetic handle prefix.

## Reserved scope

Reconcile only the numeric alias fixture in `tests/QS3D.Core.SmokeTests/WorkspaceCurtainOwnerSelectionSmoke.cs`: use a supported CAD Handle spelling (`0x00D2`) for stored boundary handle `D2`, preserving the intended numeric canonicalization assertion.

## Expected surfaces

- `tests/QS3D.Core.SmokeTests/WorkspaceCurtainOwnerSelectionSmoke.cs`
- this claim for completion evidence

## Excluded scope

- No production ownership/resolver changes, P10 automation/probe files, Source Reconcile/`LOCAL-004`, issue `#987`, workflow/release, BricsCAD runtime, or GitHub Actions changes.
- Do not broaden accepted CAD Handle syntax to the synthetic `HANDLE:` prefix.

## Validation plan

- full Core Release build and deterministic smoke;
- focused Auto Room and Workspace/Curtain selection gates;
- aggregate preflight and `git diff --check`;
- latest-main sync and exact one-file diff review before push/merge; no force-push or branch deletion.

## Coordination

Current ACTIVE/BLOCKED claims and open PRs were rechecked at the baseline; none reserves this fixture. The completed #982 source contract and production Auto Room fallback remain unchanged.

## Completion condition

Claim-only PR is merged before editing, the one-token fixture correction is merged, full Core smoke and source preflights are recorded truthfully, and this claim is marked `COMPLETED` in a separate closeout.
