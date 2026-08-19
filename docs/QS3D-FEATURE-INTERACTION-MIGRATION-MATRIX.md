# QS3D Feature Interaction Migration Matrix

**Issue / Lane-Key:** #3137 / `issue-3137`  
**Parent architecture:** #3113 / PR #3119  
**Baseline evidence:** `main@87175dfc69100896148ba773b671acdfd27e58c6`  
**Purpose:** give implementation agents one evidence-backed reference for how every current Workspace leaf participates in the registry-driven interaction architecture without inventing business behavior that is not established yet.

## 1. How to read this matrix

This document distinguishes three kinds of information:

- **ESTABLISHED** — directly visible in current `main`, an active canonical feature Issue, or the accepted parent architecture carrier.
- **TARGET** — explicitly required by the parent UIX architecture or an active feature Issue but not necessarily landed in `main` yet.
- **TBD** — product/business behavior is not established strongly enough to implement without another decision or stronger source evidence.

Agents must not convert a `TBD` cell into assumed behavior merely because a similar feature uses that workflow.

The target interaction vocabulary is the one defined by #3113 / PR #3119:

- on-select: `SelectOnly`, `ShowPrimaryInspector`, `ShowPrimaryAndSecondaryInspector`, `ShowEmptyStateUntilInstanceSelected`, `ActivateToolContext`;
- create recipe: `Direct`, `ChooseRecipe`, `FormThenCreate`, `FormThenPick`, `PickThenForm`, `Wizard`;
- normal persistent surfaces: zero, one or two inspector slots;
- transient surfaces: `RecipeChooser` and at most one blocking `ModalSheet`;
- `FloatingTool` is exceptional and is not the normal answer for Add parameters.

## 2. Current Workspace facts that apply to all leaves

At the recorded baseline, `WorkspacePanel.xaml` contains a fixed left `TreeView` whose semantic leaves use `Tag` values parsed as `ElementCategory`. The current tree includes:

- `Grid`;
- `Room`, `FloorFinish`, `Waterproofing`, `Skirting`, `WallFinish`, `CeilingFinish`, `Railing`;
- `Beam`, `Slab`, `Column`, `StructuralWall`;
- `ArchitecturalWall`, `GlassWall`, `WallPier`;
- `WallOpening`, `Door`;
- `Stair`, `Foundation`, `Earthwork`, `CustomQuantity`.

Current selection behavior is generic: selecting a tagged leaf sets `_categoryFilter`, filters the Family list and updates status. It does not itself create project data.

Current center workspace exposes generic Family/Type actions including `+ Thêm`, delete, capture selected CAD and create/update native 3D. The current generic `OnAddClick` creates a new Family for the selected category or duplicates the selected Family. Therefore **current generic Add behavior is evidence, not the final target business workflow for every feature**.

Current property editing is also shared: one grouped property list with Family/instance scope and reusable text/boolean/choice editors. That is an existing reusable surface and should be adapted by the UIX architecture rather than duplicated feature by feature.

## 3. Interaction matrix

| Feature / current tag | Current evidence | Target on-select | Target primary Add recipe | Alternate recipes | Create-time fields | CAD / host prerequisite | Post-create persistent surfaces | Capability/actions | Quantity / dependency / dirty expectations | Migration status | Confidence / unresolved product question |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **Lưới Trục / `Grid`** | Dedicated/special flow is explicitly excluded from the generic quick-schema lane #3107. Current tree leaf exists. | `SelectOnly` or dedicated Grid context; exact inspector behavior TBD. | **TBD — dedicated Grid flow.** Do not force generic Family form. | TBD. | TBD; #3107 explicitly says Grid is not a generic quick form. | Likely CAD/grid-specific input, but exact sequence is not established here. | 0/1 TBD. | Existing generic capture/3D actions are available at shell level, but target Grid capabilities must be declared explicitly. | TBD. | `legacy / special-flow` | **TBD:** what are the supported Grid creation modes and what data must be collected before CAD input? |
| **Phòng / `Room`** | Active #3095 explicitly requires direct Add, sequential `Phòng-1`, `Phòng-2`, selected-Room duplication basis and restored right Room/finish pane. | `ShowPrimaryAndSecondaryInspector` is the target-compatible reading of #3095: Room detail plus contextual finish/detail surface. | **`Direct`**. | Future boundary/detection modes may be alternate recipes, but none are established as required. | No mandatory generic Family chooser; safe defaults/context are sufficient for primary Add per #3095. | None for primary direct Add. Future detection recipe TBD. | **2 target-capable slots**: Room properties/detail + contextual finish/host information. | Add Room; edit properties; Room finish actions remain isolated; generic locate/selection actions may be capability-driven. | Room is a host for finish features; dependency/dirty semantics must remain explicit. | `partial / active predecessor #3095` | High for direct Add and Room pane; future alternate create modes remain TBD. |
| **Sàn Hoàn Thiện / `FloorFinish`** | Current tree leaf exists. #3107 owns explicit non-empty property schema/defaults. #3132 says it may default from selected Room and optionally request material/thickness before creation. | `ShowPrimaryAndSecondaryInspector` when an instance/host is selected is a target requirement from #3132. | **TARGET: host-derived direct-ish or `FormThenCreate`/`FormThenPick` depending required parameters.** | TBD; do not invent extra modes. | Material/thickness are explicitly representative fields in #3132; final schema comes from landed #3107 + shared renderer. | Selected Room host may provide defaults/context. Exact geometry capture path TBD. | 1–2: properties + host/dependency/quantity context as needed. | Add, edit parameters/material, regenerate when host is dirty, locate/delete subject to capability declaration. | Explicit host dependency on Room; dirty/regeneration behavior required by #3132. | `partial / schema predecessor active` | Medium-high. **TBD:** is primary creation direct-from-Room with optional edit, or must material/thickness be confirmed before mutation? |
| **Chống Thấm / `Waterproofing`** | Current tree leaf exists. #3107 owns explicit schema. #3132 says scope/material/thickness or pick-face input may be required according to recipe. | `ShowPrimaryAndSecondaryInspector` when contextual host information is useful. | **TARGET: `FormThenPick` or `PickThenForm`**, exact primary sequence TBD. | Possible host-derived mode vs pick-face mode, but exact supported recipe set is TBD. | Scope/material/thickness are established candidate inputs in #3132. | Room/face/host input is expected, but exact required source is recipe-specific. | 1–2: properties + host/source context. | Add, edit parameters/material, regenerate, locate/delete as supported. | Host dependency and dirty/invalid-host state required by #3132. | `partial / schema predecessor active` | Medium. **TBD:** primary workflow — choose scope first then pick, or pick host/face first and derive available scope? |
| **Chân Tường / `Skirting`** | Current tree leaf exists. #3107 owns explicit schema. #3132 says it can derive from Room perimeter and expose profile/height/material. | `ShowPrimaryAndSecondaryInspector` for properties + host/perimeter context when selected. | **TARGET: host-derived `FormThenCreate` or direct-ish recipe.** | TBD. | Profile/height/material are explicit representative fields. | Selected Room perimeter/host is expected. | 1–2: properties + host/dependency context. | Add, edit profile/height/material, regenerate from Room perimeter, locate/delete. | Room-perimeter dependency; dirty/regenerate required when host changes. | `partial / schema predecessor active` | Medium-high. **TBD:** are profile/height/material mandatory before creation or can defaults create directly? |
| **Hoàn Thiện Tường / `WallFinish`** | Current tree leaf exists. #3107 owns explicit non-empty quick schema. | `ShowPrimaryInspector`; secondary host/dependency slot only if needed. | **TBD**, likely schema/host-driven but not established strongly enough. | TBD. | Consume #3107 schema after landing; do not duplicate it. | Host wall/Room relationship is product-dependent and not fully specified by current evidence. | 1–2 TBD. | Property/material editing and supported regenerate/locate actions should be capability-driven. | Host/dependency semantics expected for finish behavior; exact host model TBD. | `partial / schema predecessor active` | **TBD:** is Wall Finish created from Room boundary, explicit wall faces, selected wall, or multiple supported recipes? |
| **Trần Hoàn Thiện / `CeilingFinish`** | Current tree leaf exists. #3107 owns explicit non-empty quick schema. | `ShowPrimaryInspector`; optional contextual host slot. | **TBD**. | TBD. | Consume landed #3107 schema. | Likely Room/ceiling host context, but exact prerequisite is not established. | 1–2 TBD. | Property/material, regenerate/locate as declared. | Host/dirty semantics required before migration but exact host type TBD. | `partial / schema predecessor active` | **TBD:** does Add derive from Room boundary/elevation, pick an existing ceiling face, or both? |
| **Lan Can / `Railing`** | Current tree leaf exists. #3107 owns explicit schema/default set. | `ShowPrimaryInspector` after feature/instance selection. | **TBD** — geometry-oriented create flow not established. | TBD. | Consume #3107 schema; create-time subset TBD. | Path/host/CAD selection requirement TBD. | 1 primary inspector; secondary only if host/path context proves necessary. | Add, property/profile edit, capture/path/native 3D where supported, locate/delete. | Quantity derived from semantic geometry; host/dirty relationship TBD. | `partial / schema predecessor active` | **TBD:** primary input is CAD path, picked host edge, parametric sketch, or chooser among these? |
| **Dầm / `Beam`** | Current tree leaf exists. #3107 says existing Beam schema must be preserved. Current shell supports generic Family create/duplicate, CAD capture and native 3D where capability supports category. | `ShowPrimaryInspector` with current Family/instance property semantics. | **TBD target**; current generic Add is Family create/duplicate, not proof of final Beam placement recipe. | Possible parametric vs CAD capture modes are not established as product requirements here. | Existing Beam schema is established but exact create-time subset is TBD. | Native geometry/CAD input may be relevant; exact primary sequence TBD. | 1 primary inspector; secondary only for host/quantity context if justified. | Family/type, capture CAD, native 3D, property edit; locate/delete. | Structural quantity from semantic geometry; dependency/dirty rules TBD. | `legacy / schema exists` | **TBD:** what does user expect `+ Add` to create — a Family/type only, a placed Beam, or a recipe chooser? |
| **Sàn / `Slab`** | Current tree leaf exists; existing Slab schema is protected by #3107. | `ShowPrimaryInspector`. | **TBD target**. | TBD. | Existing Slab schema; create-time subset TBD. | Boundary/CAD host input likely relevant but not established as canonical. | 1 primary inspector; secondary optional. | Family/type, capture, native 3D, property editing, locate/delete. | Quantity/dirty semantics tied to geometry; exact dependencies TBD. | `legacy / schema exists` | **TBD:** primary Add should start boundary drawing, capture a closed CAD boundary, create a type only, or offer a chooser? |
| **Cột / `Column`** | Current tree leaf exists; existing Column schema preserved by #3107. | `ShowPrimaryInspector`. | **TBD target**. | TBD. | Existing Column schema; create-time subset TBD. | Placement point/host level semantics likely relevant but not established here. | 1 primary inspector. | Family/type, capture, native 3D, properties, locate/delete. | Level/floor dependency and quantity semantics should be explicit when registry profile is defined. | `legacy / schema exists` | **TBD:** does Add place a Column immediately after point pick, open a form first, or remain Family/type creation? |
| **Vách / `StructuralWall`** | Current tree leaf exists. Current toolbar exposes wall junction/snap operations globally. Existing Wall-family schema is preserved by #3107. | `ShowPrimaryInspector`; contextual geometry helper may be secondary if needed. | **TBD target**. | Possible draw/capture recipes are not yet established. | Existing wall schema; create-time subset TBD. | Wall path/CAD input likely required for placed instances. | 1 primary + optional secondary geometry/junction context. | Wall junction analysis, snap preview/apply, capture, native 3D, properties, locate/delete subject to profile. | Junction and geometry dirty semantics should be explicit. | `legacy / schema exists` | **TBD:** which wall-specific toolbar actions belong to StructuralWall versus architectural wall variants? |
| **Tường Gạch / `ArchitecturalWall`** | Current tree leaf exists under `Tường KT`. Existing Wall-family schema is preserved by #3107. | `ShowPrimaryInspector`; optional geometry/host context. | **TBD target**. | TBD. | Existing wall schema; subtype-specific create fields TBD. | CAD path/room boundary/host source TBD. | 1–2 TBD. | Wall geometry actions only if semantically valid for this feature; avoid inheriting global buttons blindly. | Wall junction/dependency semantics TBD. | `legacy / schema exists` | **TBD:** canonical Add mode and which wall junction/snap operations apply. |
| **Vách Kính / `GlassWall`** | Current tree leaf exists and repository contains a dedicated `CurtainWallWindow`, which proves specialized legacy tooling exists but does not by itself define the new registry recipe. | `ShowPrimaryInspector` or `ActivateToolContext`; exact target TBD. | **TBD**; specialized workflow may require chooser/form/tool context. | TBD. | Dedicated curtain/glass parameters likely exist in legacy tool; migration must inventory them before schema design. | CAD/path/panel-host context TBD. | 1–2 TBD. | Specialized glass/curtain operations should be capability-driven rather than direct window creation. | Panel/grid/host dependency semantics TBD. | `legacy specialized` | **TBD:** migrate dedicated curtain-wall window into modal/inspector/tool primitives or retain a justified `FloatingTool`? |
| **Trụ Tường / `WallPier`** | Current tree leaf exists. | `ShowPrimaryInspector`. | **TBD**. | TBD. | TBD. | Wall host/placement context likely but not established. | 1 primary; optional host context. | Add/capture/native 3D/property actions only after explicit capability mapping. | Host-wall dependency likely; exact dirty/regenerate semantics TBD. | `legacy` | **TBD:** is WallPier a wall-hosted parametric object, CAD-captured object, or both? |
| **Lỗ Mở Vách / `WallOpening`** | Current tree leaf exists. #3107 owns explicit schema. Current shell exposes `Auto Host` for Door/Opening selection and states it safely associates selected Door/Opening with wall host without automatically cutting solid. | `ShowPrimaryAndSecondaryInspector` is appropriate target when host context is present. | **TBD target**, likely host/pick-driven. | TBD. | Consume #3107 schema; opening dimensions/offset fields must come from that schema, not duplicated guesses. | Wall host association is established; exact create input order TBD. | 1 primary + host/dependency secondary. | Auto Host, properties, locate/delete; geometry cutting must remain explicit/safe. | Strong wall-host dependency; invalid/missing-host states must be visible. | `partial / schema predecessor active` | **TBD:** pick wall first then enter opening parameters, or enter parameters then place on wall? |
| **Cửa Đi / `Door`** | Current tree leaf exists. #3107 owns explicit schema. Current shell `Auto Host` explicitly includes Door and wall-host association. | `ShowPrimaryAndSecondaryInspector` when host context selected. | **TBD target**, likely host/pick-driven. | TBD. | Consume #3107 schema; exact create-time required subset TBD. | Wall host is established. | 1 primary + host/dependency secondary. | Auto Host, properties, locate/delete, geometry update where supported. | Wall-host dependency explicit; host invalidation/dirty state must be actionable. | `partial / schema predecessor active` | **TBD:** primary Door placement interaction and whether family/type choice occurs before or after wall pick. |
| **Cầu Thang / `Stair`** | Current tree leaf exists. #3107 owns explicit schema. | `ShowPrimaryInspector`; complex contextual secondary only if necessary. | **TBD**; may legitimately require multi-step flow, but `Wizard` must not be assumed without evidence. | TBD. | Consume #3107 schema; identify create-time subset separately. | Level/path/geometry inputs likely but exact requirements TBD. | 1–2 TBD. | Properties, geometry/create actions, locate/delete as supported. | Floor/level dependency and quantity semantics should be explicit. | `partial / schema predecessor active` | **TBD:** can Stair fit one form + CAD input, or is a wizard genuinely necessary? |
| **Móng / `Foundation`** | Current tree leaf exists; existing Foundation schema is protected by #3107. | `ShowPrimaryInspector`. | **TBD target**. | TBD. | Existing Foundation schema; create-time subset TBD. | Placement/host/level input TBD. | 1 primary. | Family/type, capture/native 3D/properties, locate/delete where valid. | Structural/level dependency and quantity semantics TBD. | `legacy / schema exists` | **TBD:** foundation subtype placement recipes and whether isolated/strip/other modes require a chooser. |
| **Đào đắp / `Earthwork`** | Current tree leaf exists. #3107 owns explicit schema. | `ShowPrimaryInspector`; secondary source/measurement context may be useful. | **TBD**. | TBD. | Consume #3107 schema. | Terrain/region/measurement source TBD. | 1–2 TBD. | Properties, quantity/measurement review, regenerate from source if supported. | Strong derived-quantity/source provenance likely; exact dirty rules TBD. | `partial / schema predecessor active` | **TBD:** Earthwork creation is region-based, surface comparison, selected CAD solids, or multiple recipes? |
| **KL Tùy chỉnh / `CustomQuantity`** | Current tree leaf exists. #3107 owns explicit schema. Parent architecture allows exceptional long-running quantity review tools but does not say this feature requires one. | `ShowPrimaryInspector` or `ActivateToolContext`; exact target TBD. | **TBD** — may be form-driven rather than geometry creation. | TBD. | Consume #3107 schema; custom quantity expression/source fields must come from real schema/domain evidence. | Selection/source references TBD. | 1 primary; second derived/result context if useful. | Edit quantity definition, recalculate/regenerate, inspect sources/results, locate selected source where applicable. | Derived-data dirty/recompute semantics should be explicit. | `partial / schema predecessor active` | **TBD:** is Add defining a quantity rule, capturing selected objects, opening a result tool, or a small chooser among these? |

## 4. Cross-feature migration rules

### 4.1 Navigation identity

The current visible Vietnamese label and current `ElementCategory` tag are migration evidence, not the permanent semantic identity. #3124/#3129 must introduce/use stable `FeatureId` values and keep visible localization separate from dispatch identity.

### 4.2 Do not universalize the current Family Add handler

Current `OnAddClick` is a useful compatibility fallback because it creates/duplicates a `ProjectFamily`, but this matrix intentionally does **not** mark that behavior as the final primary Add recipe for every feature. The registry profile must decide whether the user is defining a Family/type, creating a placed business instance, collecting CAD input, or starting a specialized tool.

Room is the clearest established counterexample: #3095 requires direct Room creation and explicitly forbids the generic Family chooser.

### 4.3 Reuse the shared property system

The existing grouped Workspace property inspector and the category quick-schema work in #3107 are the source to adapt. UIX lanes must not create a separate renderer for FloorFinish, Waterproofing, Skirting, WallFinish, CeilingFinish, Railing, WallOpening, Door, Stair, Earthwork or CustomQuantity.

### 4.4 Host/dependency state is first-class

Features with semantic hosts must expose `MissingHost`, `Dirty` or equivalent actionable states rather than silently failing or showing blank panes. Clear host relationships already established by current work include:

- finish features -> Room context in #3132;
- Door / WallOpening -> wall host association in current Workspace `Auto Host` behavior;
- Room -> host/context source for room-finish generation.

Other suspected host relationships remain TBD until product/source evidence confirms them.

### 4.5 Persistent surface budget

Normal feature flow uses **0, 1 or 2** persistent inspectors. A third or fourth persistent pane is not a migration option. Complex information must use tabs/expanders inside the two slots or an explicitly justified exceptional tool.

### 4.6 Modal budget

A popup/modal exists only when a real decision or required value cannot be safely inferred. Direct features must stay direct. A two-to-five-mode choice uses `RecipeChooser`; one-form input uses `ModalSheet`; `Wizard` is reserved for genuinely multi-step flows.

## 5. Product questions that block exact recipe selection

These questions are intentionally centralized so later agents do not invent answers independently:

1. **Grid:** what exact create modes exist and what CAD input sequence is canonical?
2. **Floor Finish:** should default Room + safe defaults create immediately, or must material/thickness be confirmed before mutation?
3. **Waterproofing:** is the primary flow `FormThenPick` or `PickThenForm`, and are host-derived and face-pick separate recipes?
4. **Skirting:** are profile/height/material mandatory before creation or editable defaults after direct host-derived creation?
5. **Wall/Ceiling Finish:** which host source is canonical — Room boundary, selected host faces/elements, or multiple recipes?
6. **Railing:** what is the primary geometric input — CAD path, host edge, parametric sketch or recipe chooser?
7. **Beam/Slab/Column/Foundation:** should `+ Add` define Family/type only, place an instance, or dispatch between definition and placement recipes?
8. **Structural/Architectural walls:** which wall junction/snap actions belong to which feature and what is the canonical Add geometry flow?
9. **GlassWall:** should the legacy dedicated window be decomposed into standard inspectors/modal/recipe primitives, or is there a justified detachable long-running tool?
10. **WallPier:** what is the required wall-host and placement model?
11. **Door/WallOpening:** does placement pick host first or collect type/parameters first?
12. **Stair:** can the flow remain one form plus CAD input, or is a wizard genuinely necessary?
13. **Earthwork:** what source model drives calculation and regeneration?
14. **CustomQuantity:** does Add define a rule, capture selected sources, open result review, or expose multiple recipes?

## 6. Minimum completeness fields for future registry descriptors

A migrated feature is not `migrated` until its descriptor/profile can answer all of the following without label-based switches:

- stable `FeatureId`;
- navigation group/order/localization key;
- on-select behavior;
- one primary action;
- primary create recipe when create is supported;
- alternate recipes, if any;
- create-time schema or explicit `Direct` declaration;
- CAD/host prerequisites;
- inspector request count (0/1/2) and providers;
- capability/action set;
- dependency/dirty/regenerate policy;
- empty/error/missing-host behavior;
- legacy compatibility mapping during migration.

Any unknown required field must fail visibly in completeness validation rather than producing a blank center/right Workspace state.

## 7. Status summary at this baseline

- **Room:** active dedicated behavior is defined by #3095; do not migrate until that lane lands.
- **Grid:** special flow; generic quick schema intentionally does not own it.
- **Beam / Column / Slab / Wall / Foundation families:** existing schemas are established; exact target create recipes remain product decisions.
- **FloorFinish / Waterproofing / Skirting / WallFinish / CeilingFinish / Railing / WallOpening / Door / Stair / Earthwork / CustomQuantity:** explicit category schemas are being delivered by active #3107; consume the landed result rather than duplicating schema definitions.
- **All features:** registry-driven identity, action dispatch and surface orchestration are not considered migrated until the corresponding UIX foundation/integration lanes land.
