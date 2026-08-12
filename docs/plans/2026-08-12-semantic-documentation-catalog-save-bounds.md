# Semantic Documentation catalog save bounded-enumeration plan — 2026-08-12

## Goal

Make `SemanticDocumentationCatalogStore.Save()` honor the existing 10,000-view and 10,000-sheet planner capacity contracts while consuming lazy persistence inputs, without changing planner, schema, editor or native CAD behavior.

## Confirmed defect

Claim commit: `7b5b0aeb12b8bbbeb030fc5ce61c7acf175fb863`.

Post-claim source was re-fetched from moving `main` at `c3884320b1dac05d9312175800a458fef19e077b`. The store still executed unbounded materialization before planner validation:

```text
var viewDefinitions = MaterializeViews(views);
var sheetDefinitions = MaterializeSheets(sheets);
var viewPlans = SemanticViewPlanner.BuildCatalog(project, viewDefinitions);
SemanticSheetPlanner.BuildCatalog(sheetDefinitions, viewPlans);
```

`MaterializeViews()` and `MaterializeSheets()` consumed their complete sources. By contrast, `SemanticViewPlanner.BuildCatalog()` and `SemanticSheetPlanner.BuildCatalog()` already stop at the first item beyond 10,000. A huge or non-terminating lazy source could therefore consume memory/time without bound in the store before the intended capacity guard was reached.

## Existing contracts to preserve

- View catalog capacity remains exactly 10,000.
- Sheet catalog capacity remains exactly 10,000.
- Capacity failures use the existing planner-compatible messages.
- Null view/sheet entries within accepted capacity remain explicit argument failures.
- View/sheet planner validation, duplicate identity checks, finite geometry rules and placement ownership remain unchanged.
- Empty catalog save continues to remove existing metadata only when needed.
- Identical serialized payload remains a true no-op.
- XML schema/version, canonical serialization order and 1 MiB payload limit remain unchanged.
- Real persistence mutation continues to `Touch()` immediately before metadata mutation.

## Implementation

### 1. Bound view materialization

`MaterializeViews()` buffers at most 10,000 definitions. Before processing each yielded item:

- if 10,000 definitions are already buffered, the current item is the 10,001st and the method throws `Semantic view catalog supports at most 10000 views.`;
- otherwise the existing null guard runs and the item is appended.

The store never requests view item 10,002 after oversize cardinality is known.

### 2. Bound sheet materialization

Apply the same one-pass contract to `MaterializeSheets()` using the existing sheet capacity message `Semantic sheet catalog supports at most 10000 sheets.`.

### 3. Adversarial Core regression

`SemanticDocumentationCatalogSaveBoundedEnumerationSmoke` exercises both lazy inputs independently:

- infinite unique views with a sentinel if item 10,002 is requested;
- infinite unique sheets with a sentinel if item 10,002 is requested;
- exact capacity exception at yield 10,001;
- unchanged `ProjectState.ChangeVersion`;
- absent documentation catalog metadata on rejection.

The smoke is registered through a dedicated module initializer to avoid shared test-registration hotspots.

### 4. Static preflight

`preflight-semantic-documentation-catalog-save-bounds.py` requires:

- store-local capacities matching existing planner contracts;
- in-enumeration capacity checks before null validation/add;
- bounded view/sheet materialization before any `project.Touch()`;
- exact planner-compatible error messages;
- both 10,001/10,002 adversarial smoke cases and module registration.

## Moving-main integration

- Work branch: `agent/documentation-catalog-save-bounds-20260812`.
- Branch baseline: `c3884320b1dac05d9312175800a458fef19e077b`.
- Refresh moving `main` before PR and again before merge.
- Compare `SemanticDocumentationCatalogStore.cs` and the new lane files specifically for concurrent overlap.
- If a concurrent winner changed the reserved store source, re-read and reconcile rather than overwrite.
- Otherwise merge a focused PR and close the claim with exact evidence.

## Validation policy

This lane is deterministic Core persistence behavior. GitHub Actions are manual-only and are not dispatched. Source/static regression is the remote evidence available here; executable smoke/preflight PASS and licensed BricsCAD V25 runtime PASS are not claimed unless actually run.
