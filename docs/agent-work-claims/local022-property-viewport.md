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

## Licensed checkpoint, 2026-09-05

Exact pushed source `87aff7fec452f9a8dd9f641ef84d143edc73514d` now has
V25.2.10 `LOCAL_PASS_BOUNDED` from allocation `ui-property-fit-v25-29`,
harness `6976ea8f599d5a3147123ac874b873116a0de8f0`, RunId
`53fafd79a2b5456fb9226a43405aa47b`. All three physical UI/save/cold-reopen
phases passed, including exact H2 hit/input, old-element native regeneration,
former-output erasure, third placement/Esc and persisted identity/digest.
Observed list79px/viewport77px fit the remaining pane. Full cleanup and exact
autostart restoration passed; the installed release and MCP remain untouched.

V26's first allocation30 passed Add/Cancel/six values but could not admit
viewport points because native Tips/Properties panels consumed the drawing
area. Its cleanup passed, zero phases qualified. A new V26 runner allocation31
hides only those native panels in a disposable profile before qualification;
V26 PASS is not yet claimed. Product packages remain frozen at `87aff7fec`.

Source-local all1652 discovered preflights and exact-head protected branch
33954182384 / PR33954225983 preflight/core succeeded. The source carrier is
now reconciled non-force with main `5088ab7328d4560ec009d65041d7dce8b57820e2`;
only unrelated release/commercial changes arrived, with no viewport-source
overlap. New-head protected checks are required; old CI does not qualify the
reconciled head and runtime evidence remains bound to its original source.
