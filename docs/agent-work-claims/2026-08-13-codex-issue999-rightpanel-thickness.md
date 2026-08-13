# Work claim — V25 RightPanel separator Thickness compile fix

- Status: `ACTIVE`
- Agent: `codex-remote-issue999-rightpanel-thickness-20260813` (`/root/fix_rightpanel_thickness`)
- Registered: `2026-08-13T15:22:59+07:00`
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

## Completion condition

The claim-first focused guard is merged normally to current `main` on top of the retained source-fix commit, the actually executed local validations are recorded without manufacturing runtime/CI evidence, issue `#999` is closed, and this claim is marked `COMPLETED` with exact SHAs.
