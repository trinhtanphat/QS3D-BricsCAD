# BLT3D verified feature + command inventory

**Issue / Lane-Key:** #3099 / `issue-3099`  
**Research baseline:** `main@a323d25a9f5720eb99b2533d9605298366dd3735`  
**Date:** 2026-08-19  
**Scope:** public clean-room product/workflow research only. No proprietary BLT source, binaries, private documentation, assets, or reverse engineering were used.

## Purpose

This note narrows the broader `docs/BLT3D-BIM5D-BENCHMARK.md` research into an evidence-first inventory of BLT3D/BLT SOFTWARE capabilities and its publicly documented command surface.

The word **command** is used deliberately in three different senses:

1. **Host command** — a literal AutoCAD command named by public documentation, such as `NETLOAD`.
2. **Public UI command/action** — a menu item, button, tab, or action name explicitly shown in the vendor's public usage instructions, such as `Calculate` or `Export -> Report`.
3. **Inferred/private command token** — a BLT-specific AutoCAD command-line name that is not published in the evidence. These are **not inventoried as facts** and must not be guessed.

## Evidence policy

| Evidence class | Meaning | Treatment in this note |
|---|---|---|
| **VENDOR-DIRECT** | Current BLT SOFTWARE public website text | May establish what the vendor publicly states; marketing/runtime claims remain vendor claims |
| **PUBLIC-SECONDARY** | Public third-party course/forum/search-index material | Context only unless independently corroborated by vendor material |
| **NOT ESTABLISHED** | No public source found in this lane | Do not turn into a BLT3D capability or command claim |

Primary source used for the current product/usage surface:

- BLT SOFTWARE public website: <https://www.thangblt.com/>

Context-only public sources reviewed but **not used to upgrade a BLT3D claim**:

- Kết Cấu Blue, historical BLT/BLT_QS shopdrawing course page: <https://ketcaublue.com/khoa-hoc-4/>
- VOZ historical thread mentioning a BLT_QS update video: <https://voz.vn/t/bim-revit-super-thread.80557/page-18>

Those historical references concern `BLT`/`BLT_QS`, not enough by themselves to prove current `BLT3D` behavior.

## High-confidence public inventory

### Product/runtime envelope

| Item | Public evidence | Status / caution |
|---|---|---|
| Product name `BLT3D` | Vendor lists BLT3D in its current product family | **VENDOR-DIRECT** |
| Primary use | Vendor describes 3D construction quantity calculation | **Vendor claim**, not independent runtime validation |
| BIM wording | Vendor says BLT3D supports BIM | **Vendor claim**; BIM depth/schema fidelity not established |
| Host/platform | Download section is labeled `Windows (AutoCAD)` | **VENDOR-DIRECT** |
| Windows versions | Windows 10/11 64-bit listed | **VENDOR-DIRECT** |
| AutoCAD | AutoCAD 2024+ listed | **VENDOR-DIRECT** |
| Native/runtime wording | ObjectARX and .NET 8 Runtime listed | **VENDOR-DIRECT**; exact internal architecture beyond that wording is not established |
| Current page version | `2.1.0` shown as latest | **VENDOR-DIRECT** as of research date |
| Trial | 30-day trial listed | **VENDOR-DIRECT** |
| Update payload wording | Vendor update instructions say to download three files: `arx`, `bridge`, `dll` | **VENDOR-DIRECT**; exact filenames/contracts are not published in the indexed text |
| Reload action | Vendor says open AutoCAD and `NETLOAD` again after copying update files | **VENDOR-DIRECT** host-command evidence |

### Model/drawing input and interchange

| Capability | Public evidence | Confidence boundary |
|---|---|---|
| DWG input | Usage instructions list DWG under import | **VENDOR-DIRECT claim**; fidelity/version matrix not published |
| DXF input | Usage instructions list DXF under import | **VENDOR-DIRECT claim** |
| IFC input | Product and usage wording mention IFC | **VENDOR-DIRECT claim**; IFC version/entities/property mapping not established |
| Revit input/integration | Product wording says Revit/IFC integration; usage text lists Revit in import formats | **VENDOR-DIRECT claim**; direct RVT parsing vs API/exchange route is not established |
| Unit awareness | Import tip asks users to ensure drawing/model units such as mm/cm/m are clear | **VENDOR-DIRECT workflow evidence** |
| Model display after import | Instructions say the software processes and displays the model after `Open` | **VENDOR-DIRECT claim**; rendering/fidelity not independently tested |

### Quantity configuration

The public usage instructions explicitly describe a `Settings` area with the following configuration concepts:

- unit choices including `m3` and `m2` wording;
- calculation method choices described as by floor, by wall, or by volume;
- decimal precision;
- material setup;
- unit-price setup.

These establish the **public configuration vocabulary**, not the exact quantity-rule engine or correctness of calculations.

### Object selection and classification

The vendor instructions publicly name these example object categories for selection/marking:

- wall;
- floor/slab;
- column;
- beam;
- foundation.

The same instructions state that users can select individual objects or groups and that the software automatically recognizes/classifies objects. The selection/grouping workflow is publicly documented; **automatic recognition/classification quality is a vendor claim and was not runtime verified**.

### Quantity calculation and results

The publicly documented workflow is:

```text
select / mark objects
  -> Calculate
  -> review Results
  -> adjust object/settings if needed
  -> Recalculate
```

The vendor says the result table includes fields described as:

- item name;
- unit;
- count/quantity count;
- quantity/volume;
- amount/value (`thanh tien`).

This is sufficient to establish a publicly described **quantity -> commercial-value result workflow**, but it does not prove cost-code/WBS versioning, formal estimating, claim certification, or 5D schedule linkage.

### Review/edit loop

The usage instructions explicitly describe:

- opening/reviewing the `Results` tab;
- zooming into a part for detailed checking;
- selecting an object and editing parameters;
- deleting and marking an object again;
- running `Recalculate` to refresh results.

This is important workflow evidence because BLT3D is not presented only as one-shot automatic takeoff; the public description includes an inspect/correct/recalculate loop.

### Reporting/export

The vendor instructions explicitly describe `Export -> Report`, followed by report type/format selection and a final `Export` action.

Publicly named report scopes include:

- summary;
- detail by item;
- by floor.

Publicly named output formats include:

- Excel;
- PDF;
- Word.

The site also advertises detailed reports and easy export/printing. Exact spreadsheet schemas, templates, formulas, formatting fidelity, and round-trip behavior are not established.

### Project/data lifecycle

The public website advertises:

- creating a new project;
- entering project metadata such as project name, location, project type, customer;
- choosing a save location;
- data/project storage;
- later retrieval/editing.

The website's claim that storage is "safe" is marketing language; no security, backup, encryption, transaction, concurrency, or recovery design is published in the evidence reviewed here.

## Publicly evidenced command/action surface

This table contains **only literal names or action labels present in the public instructions**.

| Surface | Literal public token/action | What the public text says it does | Evidence classification |
|---|---|---|---|
| AutoCAD host command | `NETLOAD` | Reload the BLT managed component after an update | **Literal host command** |
| File menu | `File -> New Project` | Start a new project | **Literal UI path** |
| New-project action | `Create` | Create project after entering metadata | **Literal UI action** |
| Main menu | `Import` | Choose a drawing/model to import | **Literal UI action** |
| File picker | `Open` | Open selected drawing/model | **Literal UI action** |
| Configuration tab | `Settings` | Configure units, method, precision, materials, unit prices | **Literal UI tab** |
| Selection tools | select / mark object | Mark wall/floor/column/beam/foundation objects; individual or group selection | **Public action description; exact button/token not published** |
| Toolbar | `Calculate` | Calculate quantities for marked items | **Literal UI action** |
| Results tab | `Results` | Review calculated results | **Literal UI tab** |
| Review action | zoom | Inspect a part in detail | **Public action description; exact command token not published** |
| Recalculation | `Recalculate` | Refresh results after adjustments | **Literal UI action** |
| Export menu | `Export -> Report` | Enter reporting/export flow | **Literal UI path** |
| Final export | `Export` | Save the selected report format | **Literal UI action** |
| Installer/update actions | `Next`, `Install`, `Finish`, `Launch BLT SOFT WARE` | Installation flow named by public instructions | **Literal install UI wording**, not BLT3D modeling commands |
| Activation | `Kich hoat` / trial choice | First-run activation/trial flow described by vendor | **Public workflow wording** |

### Important command-line conclusion

The current public/indexed evidence reviewed in this lane **does not establish any BLT-specific AutoCAD command-line token** such as a command literally named `BLT3D`, nor does it publish a canonical list of BLT-prefixed command names.

Therefore this note intentionally does **not** invent command tokens from button names, product names, old `BLT_QS` references, screenshots, or assumptions about ObjectARX/.NET registration.

The only literal AutoCAD host command established by the current vendor instructions is `NETLOAD`, and `NETLOAD` is an AutoCAD command used to load/reload a managed add-in; it is not a proprietary BLT command name.

## Capability map with evidence strength

| Capability/workflow | Evidence | Classification |
|---|---|---|
| 3D quantity takeoff | Vendor product description + calculate workflow | **Vendor claim with direct workflow text** |
| BIM support | Vendor product description | **Vendor claim** |
| DWG/DXF import | Vendor usage instructions | **Vendor claim** |
| IFC import/integration | Vendor product + usage instructions | **Vendor claim** |
| Revit import/integration | Vendor product + usage instructions | **Vendor claim; route/fidelity ambiguous** |
| Object selection | Vendor usage instructions | **Direct workflow evidence** |
| Group selection | Vendor usage instructions | **Direct workflow evidence** |
| Object recognition/classification | Vendor usage instructions | **Vendor claim** |
| Wall/floor/column/beam/foundation examples | Vendor usage instructions | **Direct vocabulary evidence** |
| Unit configuration | Vendor usage instructions | **Direct workflow evidence** |
| Calculation method configuration | Vendor usage instructions | **Direct workflow evidence; rule semantics incomplete** |
| Decimal precision | Vendor usage instructions | **Direct workflow evidence** |
| Material configuration | Vendor usage instructions | **Direct workflow evidence** |
| Unit-price configuration | Vendor usage instructions | **Direct workflow evidence** |
| Automatic quantity calculation | Vendor usage instructions | **Vendor claim + command workflow evidence** |
| Result table | Vendor usage instructions | **Direct workflow evidence** |
| Amount/value in result | Vendor usage instructions | **Direct workflow evidence; not proof of full 5D costing** |
| Model/result zoom review | Vendor usage instructions | **Direct workflow evidence** |
| Parameter correction | Vendor usage instructions | **Direct workflow evidence** |
| Delete/re-mark object | Vendor usage instructions | **Direct workflow evidence** |
| Recalculation | Vendor usage instructions | **Direct workflow evidence** |
| Summary/detail/floor reports | Vendor usage instructions | **Direct workflow evidence** |
| Excel export | Vendor usage instructions | **Direct workflow evidence** |
| PDF export | Vendor usage instructions | **Direct workflow evidence** |
| Word export | Vendor usage instructions | **Direct workflow evidence** |
| Project storage/retrieval/editing | Vendor product/usage wording | **Vendor claim/workflow wording** |
| AutoCAD 2024+ host | Vendor download section | **Vendor claim** |
| ObjectARX | Vendor download section | **Vendor runtime wording** |
| .NET 8 Runtime | Vendor download section | **Vendor runtime wording** |
| `NETLOAD` update/reload | Vendor update instructions | **Literal host-command evidence** |

## Marketing claims that must remain labeled

The vendor website also advertises statements such as:

- very high/absolute accuracy wording;
- up to 99.9% accuracy;
- reducing calculation time by 70%;
- fast/advanced processing;
- safe storage;
- continuous updates;
- broad suitability across building/industrial/infrastructure/geotechnical work.

These are **marketing/vendor claims**. This lane found no independent benchmark, test corpus, runtime capture, methodology, error bounds, or audit evidence sufficient to promote them to verified performance/correctness facts.

## Ambiguities and inconsistencies in the public page

### Revit is not a proven file-format contract

The public page uses both "Revit/IFC integration" wording and lists "Revit" among import formats. It does not establish:

- supported `.rvt` versions;
- whether native RVT is parsed directly;
- whether a Revit API/add-in/export step is required;
- which categories/properties/geometry survive exchange;
- linked-model/worksharing behavior.

QS3D planning must therefore use the conservative statement **"vendor advertises Revit integration/import wording"**, not "BLT3D has verified native RVT support".

### IFC support lacks a published compatibility matrix

No public evidence reviewed here establishes IFC2x3 vs IFC4/IFC4.3 behavior, entity coverage, property-set mapping, units, classifications, openings, quantities, georeferencing, or round-trip fidelity.

### Installation text mentions macOS while the current product download is Windows/AutoCAD

The general installation instructions mention a `.dmg` path for macOS, while the current download/runtime section is explicitly `Windows (AutoCAD)` and lists ObjectARX/.NET 8/AutoCAD 2024+.

This is an internal public-page wording inconsistency. It is **not sufficient evidence to claim current BLT3D macOS/AutoCAD-for-Mac support**.

### "Automatic classification" is not a published taxonomy

The site says objects can be recognized/classified automatically but does not publish the category ontology, rule priorities, confidence model, override semantics, mapping to BIM classifications, or failure behavior.

## Features/commands NOT established by current public evidence

The following should remain `NOT ESTABLISHED` for BLT3D unless stronger primary evidence is found:

- a literal BLT3D AutoCAD command-line command list;
- command aliases/shortcuts/hotkeys for BLT-specific tools;
- Ribbon panel names or exact palette/window names beyond the generic labels above;
- explicit rebar quantity engine, bar bending schedules, hooks/laps/couplers;
- explicit MEP quantity engine;
- 4D schedule/activity linkage;
- progress measurement/certification workflow;
- payment claim workflow;
- earned-value/cash-flow forecasting;
- cost-code/WBS versioning;
- formal estimate/budget version control;
- change-order/contract workflow;
- native BCF/IDS/bSDD support;
- exact IFC schema/version compatibility;
- exact RVT version compatibility;
- API/SDK/public automation interface;
- scripting/plugin extension API;
- multi-user collaboration/concurrency model;
- cloud sync/storage architecture;
- database technology/schema;
- encryption/signing/security model;
- deterministic audit trail/provenance format;
- exact geometry deduction rules;
- standard/measurement-rule packs;
- independent performance or correctness benchmark;
- a distinct BLT product named `BIM5D`.

## Historical BLT/BLT_QS context — explicitly separated from BLT3D

Public historical/secondary material shows that earlier `BLT`/`BLT_QS` branding was used in AutoCAD-oriented shopdrawing/quantity contexts, including steel quantity workflows. That material can be useful for lineage research, but it is insufficient to assert that the current BLT3D product implements the same commands, modules, or behavior.

In particular:

- do not convert a historical `BLT_QS` video/course title into a current BLT3D command;
- do not infer rebar/shopdrawing features in current BLT3D solely from older BLT/BLT_QS usage;
- do not infer licensing/packaging/runtime identity across old and current generations.

## Clean-room implications for QS3D

This inventory is a requirements/UX reference, not a clone specification.

For `QS3D-BricsCAD`, the useful public workflow signals are:

1. **project-oriented takeoff** rather than isolated geometry commands;
2. **import/configure/select/calculate/review/recalculate/report** as a coherent loop;
3. **object grouping/classification** as part of takeoff usability;
4. **model-linked result review** and zoom/check behavior;
5. **explicit units and calculation settings** before quantity computation;
6. **materials/unit prices and amount output** as a bridge from quantity to estimating;
7. **summary/detail/floor reporting** and common office formats;
8. **hosted CAD integration** rather than assuming a standalone CAD engine.

The repository product boundary remains authoritative: QS3D here is a BricsCAD V25/V26 hosted plugin. BLT3D's publicly advertised AutoCAD/ObjectARX runtime is competitor evidence only and does not change QS3D's BricsCAD host architecture.

## Suggested handoff to related research lanes

- **#3102 interoperability:** use the DWG/DXF/IFC/Revit rows above as competitor claims, then map what QS3D can legally and technically support with explicit adapters/provenance.
- **#3103 gap matrix:** compare current `main` to the evidenced workflow loop and mark each item `already present`, `partial`, `missing`, or `not desired/out of boundary`.
- **#3101/#3104 5D modeling:** do not treat the BLT result "amount" field or unit-price setting as proof of a full 5D schedule/cost domain.
- **future command-parity research:** requires stronger public primary evidence (manual/help/video with readable command names) before adding any BLT-specific AutoCAD command token.

## Research conclusion

The strongest public evidence establishes a BLT3D workflow centered on project creation, model/drawing import, quantity settings, object selection/grouping, calculation, result review/correction/recalculation, and report export. It also establishes vendor claims for BIM, DWG/DXF/IFC/Revit handling and an AutoCAD 2024+/ObjectARX/.NET 8 Windows runtime envelope.

The **publicly evidenced literal action surface** includes `File -> New Project`, `Create`, `Import`, `Open`, `Settings`, `Calculate`, `Results`, `Recalculate`, `Export -> Report`, `Export`, plus the AutoCAD host command `NETLOAD` for reload/update. No BLT-specific AutoCAD command-line token list was established, so none is invented here.
