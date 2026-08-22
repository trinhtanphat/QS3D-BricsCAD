# LOCAL Quantity Review — Cubicost-like closed-loop acceptance

Issue: #3500  
Umbrella: #3142  
Runtime dependencies: #72 / #1669  
Source owner/session: `gpt56sol / quantity-review-closed-loop-closeout-20260822`

## Purpose

This runbook is the licensed/local acceptance tail for the already-landed QS3D Quantity Review workflow:

`Model/CAD <-> Quantity Tree <-> Quantity Explanation/Evidence <-> exact BREP face/deduction review <-> Excel <-> Model`

The source implementation is intentionally one canonical pipeline. Do not create a second quantity engine, BREP calculator, workbook authority, or persistent presentation layer merely to execute this matrix.

Remote/static/build/CI success is **not** `LOCAL_PASS`. A local agent records `LOCAL_PASS` only after executing the applicable rows in a licensed BricsCAD host on the exact tested SHA and preserving sanitized evidence.

## Preconditions

1. Use a clean checkout of the exact intended merged/source-ready SHA recorded in `docs/LOCAL-AGENT-INBOX.md` / issue #3500.
2. Use licensed BricsCAD V25 x64 first. Where the local qualification lane also owns V26, repeat the same product-facing rows against the V26 build because V26 shares the V25 Quantity Review C#/XAML source.
3. Build/load the plugin using the repository-approved local qualification path. Record BricsCAD build, exact QS3D Git SHA, plugin ProductVersion and a sanitized plugin hash when available.
4. Work only on a disposable drawing/project copy. Keep customer/private DWGs, raw Handles, ProjectIds, drawing fingerprints, machine paths, license data and proprietary DLLs out of Git.
5. Prepare at least one semantic QS3D element with live native `Solid3d` geometry. A Foundation/raft-like rectangular element is preferred because it makes concrete and four-side formwork evidence easy to inspect.
6. For the reference arithmetic row, use a deterministic synthetic fixture when available whose expected values are `V = 0.450 m³` and four side faces of `0.300 m²` each, total `1.200 m²`. If the exact fixture is unavailable, record the actual deterministic values and prove internal gross/deduction/net arithmetic instead of fabricating the reference numbers.

## Acceptance matrix

### Q1 — deterministic Quantity Tree

- Open **DIỄN GIẢI KHỐI LƯỢNG / Quantity Insight**.
- Verify the semantic hierarchy is visibly `Floor -> Type/Category -> Name/Family -> Element`.
- Select the test element leaf and confirm its displayed quantities match the current project result.
- Change to a parent Name/Family, Type/Category and Floor node and confirm the descendant scope is deterministic and contains only the intended semantic elements.

PASS requires no guessed grouping and no cross-project/DWG row reuse.

### Q2 — Model -> Quantity synchronization

- Select the test element's authoritative/current CAD object in BricsCAD.
- Verify Quantity Insight highlights the matching semantic quantity row.
- Clear the CAD selection and verify the row-selection highlight clears without mutating quantity/project state.
- Select a foreign/non-semantic object and verify QS3D does not invent a semantic quantity match.

PASS requires current live Handle resolution and exact active-DWG/project affinity.

### Q3 — Quantity -> Model locate/zoom

- Click the semantic element row with `Click = 3D` enabled, or use **Định vị**.
- Verify only current live CAD geometry belonging to that semantic row is selected and the viewport zooms to it.
- Delete/stale one required source/generated Handle, refresh as appropriate, then repeat on a disposable state.

PASS requires stale/missing/foreign provenance to fail closed instead of selecting a similarly named object.

### Q4 — concrete explanation

For the test element, inspect **THỂ TÍCH • GỘP - TRỪ = CÒN**.

- Verify `V gộp` is the live exact-BREP gross volume.
- Where there is no valid intersection, deduction is zero and `V còn = V gộp`.
- Where a valid opening/intersection exists, verify the deduction row identifies the actual cause and `V còn = V gộp - deduction`.
- On the reference fixture, verify `0.450 m³` when that fixture is used.

PASS requires current live geometry; do not accept bounding-box volume or a stale cached value as exact evidence.

### Q5 — formwork by exact face

Inspect **VÁN KHUÔN THEO MẶT • GỘP - TRỪ = CÒN**.

- Verify exact stable face rows such as `SOLID-01/FACE-01`, `.../FACE-02`, etc. are shown with face type and gross/net area.
- Confirm top/bottom/non-applicable faces are excluded according to the current quantity rule and side faces contribute as expected.
- On the reference rectangular fixture, verify four contributing side faces of `0.300 m²` each and `S còn = 1.200 m²` when that fixture is used.

PASS requires the displayed sum to equal the contributing exact-face evidence after deductions.

### Q6 — click exact face -> only that native BREP face

- Click `SOLID-01/FACE-03` heading or its `S gộp` / `S còn` value.
- Verify only the corresponding native BREP face highlights.
- Verify the whole `Solid3d` is not left as the implied/PICKFIRST selection for this face action.
- Click `SOLID-01/FACE-02` and verify Face #3 is unhighlighted before Face #2 highlights.
- Select another tree/detail row and verify the old face highlight clears.

PASS requires real native subentity highlight, not persistent entity color/material changes.

### Q7 — stale face topology fails closed

- While a face row is visible, change/regenerate the disposable geometry so the BREP fingerprint/topology no longer matches the displayed evidence.
- Invoke the old face action.

PASS requires refusal and cleanup of stale face highlight. It must not guess a face by stale index after topology changed.

### Q8 — deduction target + cause + exact transient region

- Use a concrete or formwork deduction row with a real current intersection/contact.
- Click the deduction action.
- Verify the target and cause geometry are the intended live entities.
- Verify the actual reconstructed intersection/contact BREP region is shown as a transient highlight and the viewport fits that region.
- Change the selected tree row and verify the transient is erased/disposed.

PASS requires exact current-region evidence; no persistent Boolean/face/material mutation is allowed.

### Q9 — evidence export parity

- With one exact element/detail selected, click **Xuất evidence**.
- Open the produced workbook using the repository-approved inspection tool when available.
- Verify exported concrete/formwork explanation IDs, gross/net rows, exact face references and deduction source/target/intersection references correspond to the same explanation currently shown in Quantity Insight.

PASS requires export of the reviewed canonical evidence graph, not an independently recalculated second answer.

### Q10 — Quantity Insight -> Excel

- Select an Element leaf, then repeat with Name/Family, Type/Category and Floor scopes.
- Click **Xuất Excel**.
- Verify the canonical ED2 workbook contains readable `CHI_TIET` and `TONG_HOP` sheets and that detail rows carry stable semantic identity/current CAD provenance as defined by the existing exporter.
- Confirm the selected tree node maps only its descendant elements into the Selection export scope.

PASS requires the full live Handle set to resolve before PICKFIRST changes; a partial stale scope must refuse without leaving a partial selection.

### Q11 — Excel -> Model traceback

- Click **Truy ngược Excel** and choose a valid current `CHI_TIET` row.
- Verify the exact current BricsCAD element is selected/located using semantic ElementId + current Handle + drawing fingerprint provenance.
- Exercise at least these negative cases on disposable workbook/copies: wrong drawing fingerprint, unknown ElementId, stale/missing Handle, and partial multi-Handle resolution.

PASS requires every negative case to preserve the prior PICKFIRST selection and refuse without guessing by name/family/category.

### Q12 — document/project isolation and cleanup

- With a face highlight and/or deduction transient active in drawing A, switch to drawing B.
- Verify no face/transient/selection state leaks into B.
- Attempt stale A-bound actions while B is active and verify fail-closed behavior.
- Return to A, refresh, and verify normal actions can be re-established from current state.
- Close/hide/unload the Quantity Insight panel and verify native face/transient cleanup.

PASS requires exact document affinity and no cross-DWG mutation.

### Q13 — save / close / cold reopen

- Record the current element identity and quantity/evidence summary using sanitized identifiers.
- Save the disposable drawing/project, close the document/application as required by the local qualification harness, and cold reopen it.
- Recalculate/refresh Quantity Insight.
- Verify intended semantic element identity, concrete/formwork values and source trace are preserved or deterministically rebuilt according to current project rules.
- Repeat valid Excel traceback against a workbook intended to remain current after reopen; if the workbook is stale by design, verify explicit refusal instead of silent rebinding.

PASS requires no duplicate/stale generated ownership and no quantity truth divergence caused solely by cold reopen.

### Q14 — no persistent review presentation mutation

- After exact-face and deduction review, save and reopen the disposable drawing.
- Verify no face color, subentity material, temporary Boolean/intersection solid or other review-only presentation state was persisted into the DWG.

PASS requires review highlighting to remain transient/read-only.

## Minimum sanitized evidence

Record one table or structured log containing:

- exact tested Git SHA;
- BricsCAD product/version/build and x64;
- plugin ProductVersion and sanitized hash when available;
- PASS/FAIL for Q1-Q14, marking truly not-applicable rows explicitly;
- the synthetic/reference fixture class used, without private path/project identifiers;
- aggregate quantity values needed to demonstrate arithmetic, without raw customer data;
- proof that wrong-DWG/stale/provenance cases refused and prior selection was preserved;
- save/cold-reopen and multi-DWG result;
- confirmation that no private/customer DWG, raw Handle list, ProjectId, drawing fingerprint, license information, proprietary DLL or unsanitized screenshot/log was committed.

If a row fails and the failure proves an ordinary source-safe defect, record the smallest sanitized reproduction on #3500/#1669/#72 as appropriate and hand it back to a remote source lane. After a new source fix is committed/pushed, rerun only the invalidated local rows against the new exact SHA.

## Completion boundary

- `SOURCE/CI: PASS` means source guards/build/tests passed for the stated candidate.
- `LOCAL_RUNTIME: PENDING_LOCAL_AGENT` means Q1-Q14 have not yet been executed completely in the licensed host for that candidate.
- `LOCAL_PASS` may be recorded only by a compatible local agent after the real matrix above is executed on the exact tested SHA.
