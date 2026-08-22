# Work claim — Geometry-backed quantity explanation

- Status: `COMPLETED`
- Agent: `gpt56sol-quantity-geometry-explainer-20260814-0849`
- Registered: `2026-08-14T08:49:00+07:00`
- Completed: `2026-08-14T12:03:00+07:00`
- Baseline main SHA: `01215685c429e54f657735be7bfc79f526aee53c`
- Priority: User-requested implementation of the attached “Diễn giải khối lượng chi tiết” specification; the existing quantity-detail panel explained report arithmetic but did not reserve an exact-geometry intersection/contact lane.

## Delivered scope

Implemented the V25 quantity explanation lane for a selected QS3D element:

- exact `Solid3d` boolean intersection after BoundingBox-only candidate pruning;
- residual subtraction semantics so overlapping causes are not double-deducted from net volume while individual cause rows remain visible;
- CAD-agnostic quantity geometry contracts with dependency/fingerprint/dirty evidence and specification tolerances;
- BREP face enumeration and gross/deduction/net formwork areas with `Bottom`, `Side`, `End`, `Top`, and `Other` classification;
- face-only contact probing using a tolerance-offset solid plus exact target intersection;
- multi-`Solid3d` face identities scoped by component/global index so coincident faces from different solids do not cross-match;
- SI unit normalization from drawing `INSUNITS`;
- native clone/residual disposal on all completion paths;
- quantity-detail UI projection with `V gộp`, `Trừ giao`, `V còn`, per-face `S gộp`, `Trừ`, `S còn`, fingerprint/dependency/diagnostic evidence;
- deduction-row click-through that selects the target + cause live CAD handles and invokes `QS3DZOOMSELECTED`;
- V25 BREP project/reference validation for `TD_MgdBrep.dll`;
- a standalone `preflight-quantity-geometry-explainer.py`, auto-discovered by `scripts/preflight-all.py`.

## Mainline evidence

The user explicitly authorized direct merge/commit/push to `main`, so the final implementation was landed directly on main rather than being held behind a PR.

Key mainline commits/evidence:

- `8eea882b75c99fba8c186e36044fdb4124a709ac` — bind exact geometry values into the quantity-detail metrics.
- `f42171d3f9dab336fa8874547a3016271977546d` — fix contact-plane matching so an offset probe's inward cut face cannot masquerade as the original target face and erase a face-contact deduction.
- `5b3a09733302ed7f135f9876345fe93350abb445` — lock quantity geometry source contracts and the strict contact-plane regression guard.
- `2191050d573075dfe07efa4e57741ce1384bf706` — coordinated V25 release guard requires `TD_MgdBrep.dll`.

The latest verified main descendant before claim close was `ad0a1a48ec102b027cbc80e7954a8afe1fde7269`; compare evidence shows it is 17 commits ahead of `5b3a09733302ed7f135f9876345fe93350abb445` with zero commits behind, so all quantity fixes above remain in main history.

## Validation evidence

- QS3D Core CI run `31771581071` / run number 44: `preflight` = SUCCESS and `core` = SUCCESS.
- In that run, Manual-only policy, Generic source guard, All discovered feature source guards (including `preflight-quantity-geometry-explainer.py`), PowerShell syntax validation, Core build, and deterministic Core smoke tests all completed successfully.
- V25 integration run `31771501089` was dispatched with `run_runtime=false` on the self-hosted `[windows, x64, bricscad-v25]` runner. At claim close the `plugin` job (`94678222968`) is still queued because the self-hosted runner has not accepted it yet; this is retained as explicit native-host evidence rather than being marked PASS without execution.

## Native acceptance note

Remote-safe implementation is complete and mainlined. Exact BricsCAD-native acceptance for BREP runtime behavior remains represented by queued run `31771501089`. The implemented row interaction selects/zooms target + cause handles; a dedicated transient translucent intersection solid and exact BREP sub-face GS highlight are not claimed as completed because those require a reliable native subentity/transient mapping probe on BricsCAD V25.

## Coordination

This lane did not modify the shared smoke registration reserved by concurrent work and did not touch unrelated Curtain schedule, QSDB, LOCAL-003/004, release-version, or general NETLOAD lifecycle source. Temporary one-shot CI dispatchers were removed after dispatch.
