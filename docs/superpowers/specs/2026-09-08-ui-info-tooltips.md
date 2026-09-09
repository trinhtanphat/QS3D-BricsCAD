# Agent Center + Update Center Info Tooltip Design

## Problem

QS3D Agent Center and Update Center currently render several explanatory paragraphs inline. On the dark desktop surfaces this makes the two windows visually dense and pushes primary actions/status farther apart. The operational values themselves (transport state, version, progress, Tunnel ID, update action) remain useful at a glance and must not be hidden.

## User-approved behavior

- Replace verbose explanatory copy with compact, round `i` info controls.
- Mouse hover over an info control shows the complete original text in a wrapped WPF tooltip.
- Keyboard focus on the same control also opens the tooltip; losing focus closes it.
- Keep short labels where removing all context would be ambiguous (for example `Runtime API key`, `Tiếp theo`, `Chi tiết`, `Build / DLL`).
- Keep operational status, version numbers, progress, buttons, Tunnel ID and update state visible.
- Apply the treatment to both Agent Center and Update Center.

## Agent Center targets

Compact these explanatory surfaces while preserving their original text as the tooltip source:

- top Agent Center explanatory subtitle;
- connection transport description;
- connection-status semantics description;
- dynamic onboarding detail while keeping a short `Tiếp theo: ...` cue visible;
- verbose Runtime API key storage/fallback label;
- Secure Tunnel security/credential note;
- Quick Tunnel warning.

Do not compact live status rows such as MCP state, endpoint, Tunnel ID, or provider paths.

## Update Center targets

Compact these explanatory surfaces while preserving their original text as the tooltip source:

- runtime build/DLL identity/path;
- state-card detail paragraph (the state heading remains visible);
- `Cập nhật khi đóng BricsCAD` helper paragraph;
- full release-note text area (the `Ghi chú phát hành` heading remains visible).

Do not compact current/latest version, release picker, progress, errors/status headings, or primary actions.

## Architecture

Add one UI-only bootstrap/decorator file under the shared V25 source root. A module initializer registers a WPF `Window.Loaded` class handler. It acts only when the concrete window type is `McpAgentControlCenter` or `UpdateCenterWindow`, leaving every other window untouched.

The decorator keeps each original text element in the visual tree but collapses it, inserts a compact replacement beside/in its slot, and binds the tooltip text and accessibility help text back to the original element. This preserves dynamic updates without copying transport/updater state into the helper. Agent Center may rebuild pages after tab/provider button clicks, so the decorator schedules one bounded re-apply after clicks in that window. Attached dependency-property markers prevent duplicate decoration.

## Accessibility / interaction

- Info controls are real focusable WPF `Button` elements, not decorative glyphs.
- `AutomationProperties.Name` identifies each info control.
- `AutomationProperties.HelpText` is bound to the original full text.
- Tooltips use a short initial delay, long enough show duration, wrapped text and bounded width.
- Keyboard focus explicitly opens the tooltip and losing focus closes it.
- The custom button template preserves a visible circular border and keyboard focus border change.

## Compatibility and non-goals

- No new package/dependency.
- Shared source must compile for the existing V25 .NET Framework and V26 .NET Windows targets.
- No changes to MCP transport selection/readiness, credentials, tunnel process control, update checks, release selection, package verification, installation, restart, or file I/O.
- No workflow files are modified.
- This change is presentation-only; it does not claim licensed BricsCAD runtime qualification.

## Verification

A feature preflight fails closed unless the UI-only helper contains both-window targeting, hover/focus tooltip mechanics, accessibility bindings, dynamic Agent Center re-apply, all requested copy matchers, and no transport/update mutation tokens. The preflight is intentionally committed before production code so its first execution is RED because the helper does not yet exist. CI/PR checks remain the final integration gate before merge.
