# QS3D documentation map

This index separates durable product/runtime contracts from feature notes, plans and historical handoffs. Prefer the durable references below when deciding current behavior; use dated audit/handoff files as evidence and history rather than as a second source of truth.

## Start here

| Need | Canonical reference |
| --- | --- |
| Product/hosting boundary | [`PRODUCT-BOUNDARY.md`](PRODUCT-BOUNDARY.md) |
| Data/source authority | [`SOURCE-OF-TRUTH.md`](SOURCE-OF-TRUTH.md) |
| Architecture | [`ARCHITECTURE.md`](ARCHITECTURE.md) |
| Commands/workflows | [`COMMANDS.md`](COMMANDS.md) |
| Project setup | [`PROJECT-SETUP.md`](PROJECT-SETUP.md) |
| Health/static gates | [`HEALTH-AND-PREFLIGHT.md`](HEALTH-AND-PREFLIGHT.md) |
| CI policy | [`../CI_POLICY.md`](../CI_POLICY.md), [`CI.md`](CI.md) |
| Local BricsCAD V25 qualification | [`LOCAL-V25-QUALIFICATION.md`](LOCAL-V25-QUALIFICATION.md) |
| V25 install/runtime setup | [`V25-INSTALL.md`](V25-INSTALL.md) |
| Multi-agent registration | [`AGENT-WORK-REGISTRATION.md`](AGENT-WORK-REGISTRATION.md), [`../AGENTS.md`](../AGENTS.md) |
| Implementation snapshot | [`IMPLEMENTATION-STATUS.md`](IMPLEMENTATION-STATUS.md) |

## Major workflow references

- Direct authoring: [`DIRECT-DRAW-WORKFLOW.md`](DIRECT-DRAW-WORKFLOW.md), [`PLAN-TO-3D-WORKFLOW.md`](PLAN-TO-3D-WORKFLOW.md).
- Quantity/BQ: [`NATIVE-BQ-TABLE-P0.md`](NATIVE-BQ-TABLE-P0.md), [`QUANTITY-REPORT-REVISION-REVIEW.md`](QUANTITY-REPORT-REVISION-REVIEW.md), [`WALL-QUANTITY-TAKEOFF.md`](WALL-QUANTITY-TAKEOFF.md).
- Schedules: [`SCHEDULES.md`](SCHEDULES.md), [`SEMANTIC-SCHEDULES.md`](SEMANTIC-SCHEDULES.md).
- Rebar 3D: [`REBAR-3D.md`](REBAR-3D.md), [`REBAR-3D-MODE-SPEC.md`](REBAR-3D-MODE-SPEC.md).
- Start Center/UI: [`UI-START-CENTER-2026-08-11.md`](UI-START-CENTER-2026-08-11.md), [`UIUX-SPEC.md`](UIUX-SPEC.md).
- Secure update/release design: [`SECURE-UPDATES.md`](SECURE-UPDATES.md) plus the release/runbook documents referenced from that file.

## Documentation hygiene

Use these rules when adding or updating Markdown:

1. **Update a canonical document instead of cloning it.** New dated files are appropriate for audit evidence, migration records or handoffs, not for redefining stable product truth.
2. **Keep README concise.** Root `README.md` is the product entry point; detailed command lists and implementation narratives belong under `docs/`.
3. **Separate source truth from runtime proof.** Source/static coverage must not be described as BricsCAD V25 qualification unless the exact SHA has licensed-host evidence.
4. **Prefer links over duplicated inventories.** Command, CI, qualification and agent rules should each have one maintained source.
5. **Do not delete historical handoffs merely to reduce file count.** They may be required for provenance or agent coordination; archive/consolidate only when their references and purpose have been checked.

## Historical and dated material

Files named `REVIEW-*`, `AUDIT-*`, `HANDOFF-*`, `PLAN-*` or containing explicit dates generally describe a point-in-time review, implementation plan or coordination record. They can be useful evidence, but when they disagree with current source or a canonical contract above, re-check current `main` and update the canonical document rather than treating the historical note as authoritative.

## Runtime qualification boundary

Repository preflight and Core smoke tests can establish source contracts and deterministic non-CAD behavior. Native geometry, UI, DemandLoad/NETLOAD, proprietary API compatibility, signed package/update behavior and representative-DWG workflows remain local BricsCAD V25 gates unless there is explicit evidence for the exact candidate SHA.
