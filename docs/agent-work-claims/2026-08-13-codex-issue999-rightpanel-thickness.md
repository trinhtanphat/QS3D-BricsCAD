# Work claim — V25 RightPanel separator Thickness compile fix

- Status: `COMPLETED`
- Agent: `codex-remote-issue999-rightpanel-thickness-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T15:22:59+07:00`
- Completed: `2026-08-13T15:29:34+07:00`
- Baseline main SHA: `a0953639f96c4ca51ab332798aab3a8d3223a93a`
- Priority: GitHub issue `#999` blocked the exact-SHA V25 build needed by the already-reserved `LOCAL-004` qualification lane. Concurrent commit `a0953639f96c4ca51ab332798aab3a8d3223a93a` landed the one-line source correction while this claim was being prepared, so this reservation adopts that winning fix and owns only the missing focused regression, validation and issue closeout.

## Reserved scope

Guard the already-landed four-edge RightPanel separator margin so the invalid two-argument WPF `Thickness` construction cannot silently recur. Validate the exact corrected source locally and close the sanitized source-blocker handoff.

## Expected surfaces

- `scripts/preflight-rightpanel-dark-host-chrome.py`
- read-only validation of `src/QS3D.BricsCAD.V25/UI/RightPanel.DarkHostTheme.cs`
- this claim file
- GitHub issue `#999`

## Excluded scope

- `LOCAL-004` Source Reconcile production source, probe, runner, gate, result documentation or runtime qualification
- RightPanel commands, event handlers, responsive sizing, XAML, shared theme redesign or visual behavior beyond preserving the intended separator margin
- V26, private/customer drawings, BricsCAD runtime execution, packaging, release, signing, installer or GitHub Actions

## Validation plan

- Run the focused RightPanel dark-host preflight and the aggregate static preflight that auto-discovers it.
- Build `src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj` in `Release|x64` against installed V25 managed references when accessible, without launching BricsCAD.
- Re-fetch current `origin/main`, reconcile intervening work, inspect the final diff and verify claim/source merge ancestry.

## Coordination

The completed RightPanel dark-host claim introduced the affected source and no longer reserves it. Concurrent unclaimed commit `a0953639f96c4ca51ab332798aab3a8d3223a93a` is retained verbatim as the source fix; this lane will not duplicate or rewrite it. Open PR `#975` changes `RightPanel.CompactShell.cs`, not this partial or its gate. The active `LOCAL-004` claim is the downstream runtime consumer and is explicitly excluded from this ordinary source-safe correction.

## Result

- Source fix retained without duplication: `a0953639f96c4ca51ab332798aab3a8d3223a93a` (`fix: use valid separator margin thickness`).
- Claim-only PR `#1000` merged as `e4a0814f298c86fe1103a572d82c64a149c6e46c`.
- Focused regression commit `fa3f530a9ea95ce220661acfb1a89fc437cc8219` merged by PR `#1002` as `2d636df43036eff03b6dacbf1dddbc9ed2b13be6`.
- GitHub issue `#999` closed after exact-main validation.

## Validation actually executed

- Exact clean `main` SHA `2d636df43036eff03b6dacbf1dddbc9ed2b13be6` passed `scripts/preflight-rightpanel-dark-host-chrome.py`.
- The aggregate auto-discovery runner passed all `730` feature preflight gates.
- Installed-reference `QS3D.BricsCAD.V25` `Release|x64` build succeeded with `0 warnings / 0 errors` against `C:\Program Files\Bricsys\BricsCAD V25 en_US`; BricsCAD was not launched.
- Final diff contains only the four-line focused static guard; no production source or `LOCAL-004` file was changed by this lane.
- No GitHub Actions were dispatched, re-run or cancelled by this lane. No native runtime PASS is claimed.

## Completion condition

Satisfied: the claim-first focused guard is merged normally to current `main` on top of the retained source-fix commit, exact source/claim/regression merge SHAs and actual local validation are recorded, issue `#999` is closed, and no runtime/CI evidence is manufactured.
