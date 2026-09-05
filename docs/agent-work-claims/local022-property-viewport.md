# Embedded property viewport correction — #5840

Lane-Key: issue-5840
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:local022-01a06ce3-20260905
Canonical carrier: agent/local022-01a06ce3-20260905/issue-5840-property-viewport
Ownership-Key: workspace-embedded-properties-viewport-clipping

Base: `d9e95f02a9652e0d8780cb91ec00f2037ab0af9f`.

Licensed V25 LOCAL-022 allocation28 (harness `c6805259b`, product
`f0146aacef0b398bc71e8e278e6f7675432c1f17`) measured a 120px PropertyList
inside a DockPanel with only79px remaining after the title/scope/search header.
Its internal scroll viewport extended beyond the allocated visible area, so
normal ScrollIntoView did not guarantee the H2 editor was physically reachable.
The runner's independent hit guard correctly withheld input; zero complete UI
phases passed. All private/profile/protected-state cleanup and exact autostart
restoration passed. No MCP execution or installed-release replacement occurred.

The fix releases the inner list minimum to0 in the final embedded layout pass.
The whole pane's120px minimum,56*/44* proportions, dedicated-mode behavior,
editable controls and host clipping remain unchanged. Existing startup, repair
and dedicated-restoration paths all reuse that final layout method.

`scripts/test-workspace-property-viewport.ps1` extracts the full production
layout method plus its actual ancestor/restore helpers into a real STA WPF
fixture. Before correction it failed: list120 exceeded available/slot79.6.
After correction it passes short/tall, tall-to-short resize, repeated repair
and dedicated-to-embedded restoration. H2, final and first editor rectangles
must fit the visible scroll/pane intersection after normal ScrollIntoView.
The auto-discovered preflight runs that fixture and mutation-checks the former
positive embedded minimum even when the dedicated branch remains correct.
Three historical guards now preserve the pane minimum rather than demanding
the defective child minimum. No native CAD assembly or input is used in WPF tests.

Licensed acceptance remains pending: exact committed/pushed matching V25/V26
packages, V25 physical H2 edit/regeneration/repeated placement/Enter/Esc/save/
cold-reopen first, full cleanup, then V26 on the same source. Source/WPF/CI PASS
must not be presented as LOCAL_PASS. Qualification evidence stays on #5718;
the full #4034/#72 matrices remain open beyond this correction.
