# QS3D Feature Interaction + Surface Orchestration

**Parent:** #3113 / PR #3119  
**Purpose:** define how each left-menu item may expose different business actions, different `+ Add` flows, popup/property input, and zero/one/two persistent detail surfaces without turning Workspace into a collection of feature-specific windows.

## 1. Problem statement

The difficult part of the QS3D left menu is not the tree itself. A selected item may mean a completely different workflow:

- selection may only filter/show existing instances;
- selection may immediately show one inspector/detail pane;
- selection may need a second contextual pane such as host/dependency/quantity information;
- `+ Add` may create directly;
- `+ Add` may need a small mode chooser;
- `+ Add` may need a parameter form before creation;
- a form may be followed by CAD picking, or CAD picking may happen first and the form second;
- some complex flows may require a wizard;
- after creation, the user may edit properties in one or two persistent panels;
- a few long-running tools may deserve a detachable/floating window.

If every feature implements these decisions with its own WPF event handlers and windows, Workspace complexity grows with the number of business features. The architecture must therefore model **interaction** as data/contracts, not as another giant category switch.

## 2. Core rule

> A menu item selects a `FeatureId`. The selected feature resolves one `InteractionProfile`. The profile declares actions, create flow, required input, and requested surfaces. Generic Workspace services render and orchestrate those declarations.

The menu must never directly decide whether a popup, form, second pane or floating window should appear.

## 3. Standard Workspace grammar

Normal Workspace remains visually stable:

```text
LEFT                         CENTER                               RIGHT
Navigation / feature tree -> Instances + primary actions -> Inspector slot 1
                                                       \-> Inspector slot 2 (optional)
```

Transient input may appear over the Workspace only when required:

```text
+ Add
   -> optional compact recipe chooser
   -> optional modal/schema form
   -> optional CAD input
   -> create/update
   -> persistent inspector(s)
```

This grammar is deliberately constrained. Different business workflows are allowed, but they must compose from the same small set of interaction primitives.

## 4. InteractionProfile

Architectural shape:

```csharp
public sealed record FeatureInteractionProfile(
    FeatureId FeatureId,
    OnSelectBehavior OnSelect,
    FeatureActionId PrimaryAction,
    IReadOnlyList<FeatureActionId> SecondaryActions,
    IReadOnlyList<CreateRecipeId> CreateRecipes,
    CreateRecipeId? PrimaryCreateRecipe,
    InteractionSurfaceProfile Surfaces,
    PropertySchemaId? CreateSchema,
    PropertySchemaId? InspectorSchema);
```

The actual implementation may use different type names, but the semantic responsibilities must remain explicit.

### 4.1 On-select behavior

A feature may declare one of the following patterns or a composition of them:

- `SelectOnly` — filter/select feature context; no extra surface required;
- `ShowPrimaryInspector` — show one right-side detail/property surface;
- `ShowPrimaryAndSecondaryInspector` — show two persistent contextual surfaces;
- `ShowEmptyStateUntilInstanceSelected` — feature is selected but inspector waits for an instance;
- `ActivateToolContext` — prepare a host/CAD tool state without starting a create mutation.

Selecting a menu item must **not** silently mutate project data.

## 5. `+ Add` is a dispatcher, not one universal behavior

The visible `+ Add` action is stable, but its recipe is feature-specific.

Supported interaction patterns should include:

### 5.1 Direct

```text
select feature -> + Add -> create -> select result -> show inspector
```

Use when safe defaults and context are sufficient.

Example: Room direct creation can remain a one-click operation after selecting Room.

### 5.2 ChooseRecipe

```text
select feature -> + Add ▼ -> choose one of 2–5 modes -> continue
```

Use when multiple legitimate creation modes exist and none can safely be inferred.

The chooser should be a compact popover/menu, not a full window.

### 5.3 FormThenCreate

```text
select feature -> + Add -> parameter sheet -> validate -> create -> inspector
```

Use when required parameters must exist before mutation but CAD input is unnecessary.

### 5.4 FormThenPick

```text
select feature -> + Add -> parameter sheet -> CAD pick/draw -> create -> inspector
```

Use when the parameter choice changes how CAD input behaves.

### 5.5 PickThenForm

```text
select feature -> + Add -> CAD pick/detect host -> parameter sheet -> create -> inspector
```

Use when the selected geometry/host determines available parameters or defaults.

### 5.6 Wizard

```text
select feature -> + Add -> multi-step wizard -> CAD input as needed -> create
```

Use only when the workflow cannot reasonably fit one form. A wizard is an exception, not the default answer to complex business logic.

## 6. Surface vocabulary

QS3D should support a small fixed set of standard surfaces.

### 6.1 PrimaryInspector

Persistent right-side surface for the main selected feature/instance properties and detail.

Typical content:

- selected instance name;
- common editable parameters;
- family/type selection;
- status/validation;
- primary feature detail.

### 6.2 SecondaryInspector

Optional second persistent contextual surface. It should exist only when it meaningfully reduces workflow friction.

Typical content:

- host/dependency information;
- derived finish list;
- quantity breakdown;
- recognition/source information;
- feature-specific contextual helper controls.

**Normal Workspace must not exceed two persistent inspector/detail slots.** If a feature needs more information, use tabs/expanders inside those slots rather than creating additional columns/windows.

### 6.3 ModalSheet

One blocking form/dialog for a decision required before continuing.

Use for:

- create-time required parameters;
- a focused edit operation that cannot safely happen inline;
- confirmation for destructive operations.

Rules:

- at most one blocking modal per Workspace session;
- `Cancel` must not mutate project data;
- Enter/Escape/focus/default-button behavior is consistent;
- validation is shown in the form, not hidden in logs;
- no modal if safe defaults make direct creation possible.

### 6.4 RecipeChooser

Compact popover/dropdown attached to the Add action. Use for a small set of alternate create modes.

Do not turn a 2-option create choice into a full dialog.

### 6.5 FloatingTool

True detachable/floating window reserved for long-running side-by-side workflows that should remain visible while the user works in the CAD drawing.

Examples may include a large result/quantity review tool. Feature Add parameter forms are **not** floating tools.

Rules:

- only explicitly declared tool profiles may open one;
- repeated invocation focuses/reuses the existing logical tool instance;
- project/document switch must safely rebind or close it;
- do not allow arbitrary feature code to instantiate top-level windows directly.

## 7. Interaction state machine

Generic creation/edit orchestration should expose explicit state instead of implicit event-handler state:

```text
Idle
Selected
Preparing
ChoosingRecipe
CollectingFormInput
WaitingForCadInput
Creating
Created
Editing
Dirty
Invalid
MissingHost
Cancelled
Error
```

Not every feature uses every state.

Important transition rules:

1. only one active create session per Workspace panel;
2. repeated Add clicks cannot start overlapping mutations;
3. switching to an incompatible feature cancels or safely terminates the current transient flow;
4. cancellation returns to a valid selected/idle state;
5. create failure preserves user context and exposes an actionable error;
6. a successful create selects the result when that is the natural workflow;
7. transient modal/chooser surfaces never outlive their create session.

## 8. Schema-driven forms

Pre-create forms and post-create inspector editing should reuse a common schema vocabulary wherever possible.

A schema field should be able to declare:

- stable key;
- localized label/help text;
- data type;
- required/optional;
- default value;
- unit/precision;
- min/max/range;
- enum/choice options;
- reference/host selector;
- read-only/computed state;
- conditional visibility/enabling;
- create-time applicability;
- edit-time applicability;
- validation rules.

This lets one feature request different parameter forms without building a custom dialog class for every leaf.

## 9. Action bar rules

Different features can have different functions, but users need a stable hierarchy.

Recommended semantic ordering:

```text
PRIMARY
+ Add

COMMON CONTEXT
Edit Parameters | Material | 3D | Quantity | Regenerate | Locate

DESTRUCTIVE / LESS COMMON
Delete | Rebind Host | Repair | Advanced...
```

Rules:

- one visually primary action for a normal feature;
- alternate Add modes behind a split/dropdown;
- unsupported capabilities do not render dead buttons;
- disabled actions explain the missing precondition;
- uncommon/repair/admin actions go to overflow/context menus;
- action order is consistent by semantic role, even when some actions are omitted.

## 10. Representative feature flows

These are interaction examples, not a claim that every unimplemented business rule is already final.

### 10.1 Room

```text
OnSelect:
  show Room instances/context

Add:
  Direct

Post-create:
  select new Room
  PrimaryInspector = Room parameters/detail
  SecondaryInspector = optional Room finish/dependency surface
```

No generic `Tham số / Solid3D` chooser should be forced merely because other feature types use one.

### 10.2 Floor Finish

Possible target pattern:

```text
OnSelect:
  require/show selected Room host context

Add:
  primary = FromSelectedRoom
  optional pre-create form = material + thickness + rule options
  alternate = PickFace

Post-create:
  PrimaryInspector = finish parameters/material
  SecondaryInspector = host/dependency/quantity context
```

If project defaults make material/thickness safe to infer, the primary recipe may remain faster and expose editing after creation. Product behavior must choose the least-friction safe path.

### 10.3 Waterproofing

Possible pattern:

```text
+ Add
  -> choose/derive host scope
  -> form for material/thickness/upturn options if required
  -> create
  -> inspectors
```

### 10.4 Skirting

Possible pattern:

```text
+ Add
  -> selected Room perimeter by default
  -> optional profile/height/material form
  -> create
  -> parameters + host/quantity context
```

### 10.5 Grid

Possible pattern:

```text
+ Add ▼
  Straight Grid
  Curved Grid

recipe
  -> collect required parameters
  -> CAD pick/draw
  -> create
```

### 10.6 Beam / structural feature

A structural feature may legitimately have more than one creation recipe:

```text
+ Add ▼
  Parametric
  Pick Centerline
  Capture Existing CAD
```

A selected recipe may then show a form for family/section/level parameters before or after CAD input. This is still one generic state machine; it is not three independent windows.

### 10.7 Custom Quantity

A complex custom-quantity definition may need a wizard if it truly has multiple dependent decisions. It should still finish into standard inspectors/results rather than leaving a custom modal as the permanent editor.

## 11. Selection/session context

The Workspace needs one coherent selected-feature session containing the relevant subset of:

- FeatureId;
- active create recipe;
- Zone/Floor/project context;
- selected Family/Type;
- selected semantic instance;
- selected CAD/source entities;
- host reference(s);
- current interaction state;
- dirty/invalid/missing-host state;
- requested surface keys.

This prevents the common failure mode where a menu feature changes but an old pane, old Family selection or old modal continues to act on stale state.

## 12. Usability decisions

### 12.1 Prefer persistent panels over extra windows

If the user needs to read/edit information repeatedly while working, prefer Primary/Secondary Inspector slots. Do not make the user manage floating windows for routine properties.

### 12.2 Popup only for an actual decision

A popup is justified when required information cannot be inferred safely or when the user must choose a recipe. It is not justified just because a feature has parameters.

### 12.3 Preserve drawing context

CAD-oriented workflows must avoid covering the drawing with large modal dialogs for long periods. Collect the minimum required input, close the modal, and continue the CAD interaction.

### 12.4 Progressive disclosure

Simple feature = simple UI. Complex feature = same stable skeleton plus contextual options. The complexity of one feature must not make all other features look complex.

### 12.5 Actionable empty states

Examples:

```text
No instance selected      -> Select an item or + Add
Host required             -> Select a Room, then Add
Missing host              -> Rebind host / inspect source
Dirty                     -> Regenerate
Unsupported create recipe -> explain why; do not open a blank form
```

## 13. Things agents must not implement

1. no giant `switch (ElementCategory)` deciding every popup/pane/action;
2. no new top-level WPF Window per feature;
3. no direct business dispatch by Vietnamese button/header text;
4. no arbitrary third/fourth persistent inspector column;
5. no mandatory popup for features with safe direct Add;
6. no duplicate property renderer when a schema can drive the form;
7. no silent mutation on menu selection;
8. no cross-lane modification of active #3095/#3107 work;
9. no feature-specific state machine hidden in ad-hoc event handler booleans when the generic interaction session can represent it.

## 14. Agent implementation queue

The owner requested a concrete pool that AI agents can choose from. These Issues are **UNCLAIMED queue items, not ACTIVE lane reservations**. An agent claims one only by performing the repository collision check, updating the selected Issue to `Status: ACTIVE` with exact agent/session + current-main baseline + canonical branch, and then creating that one branch.

| Task | Issue | Scope | Main dependency |
|---|---:|---|---|
| UIX-001 | #3124 | Feature Registry + InteractionProfile contracts | foundation |
| UIX-002 | #3125 | interaction surface coordinator | #3124 |
| UIX-003 | #3126 | Add/CreateRecipe state machine + pre-create forms | #3124 |
| UIX-004 | #3128 | generic 0/1/2 inspector host layout | #3124/#3125 interfaces |
| UIX-005 | #3129 | left-navigation projection + SelectedFeatureContext | #3124 |
| UIX-006 | #3131 | Room vertical-slice migration | generic framework + landed #3095 |
| UIX-007 | #3132 | Floor Finish/Waterproofing/Skirting migration | generic framework + landed #3107 |
| UIX-008 | #3133 | completeness/usability guards | #3124, expands with framework |
| UIX-009 | #3134 | capability-driven action bar | #3124/#3129 |
| UIX-010 | #3135 | selection/session lifecycle | #3124, integrate interfaces |
| UIX-011 | #3136 | shared property/form schema renderer contracts | #3124 + landed #3107 |
| UIX-012 | #3137 | feature-by-feature interaction migration matrix | #3124/current main |
| UIX-013 | #3138 | reusable safe modal/popup primitives | #3124 + coordinator/create interfaces |
| UIX-014 | #3139 | floating/detachable tool policy/host | #3124/#3125 |

## 15. Parallelization guidance

Do not start every runtime lane at once. Use dependency waves.

### Wave 1 — foundation

- UIX-001 (#3124)
- UIX-012 (#3137) may proceed as a documentation/evidence matrix without modifying runtime carriers.

### Wave 2 — generic framework in parallel after contracts stabilize

- UIX-002 (#3125)
- UIX-003 (#3126)
- UIX-005 (#3129)
- UIX-008 (#3133) initial completeness guards
- UIX-009 (#3134)
- UIX-010 (#3135)
- UIX-013 (#3138)

Agents must use interface boundaries to avoid all editing the same `WorkspacePanel` files.

### Wave 3 — presentation + schema integration

- UIX-004 (#3128)
- UIX-011 (#3136) after #3107 is landed
- UIX-014 (#3139)

### Wave 4 — pilot migrations

- UIX-006 (#3131) after #3095 is landed and generic framework is ready
- UIX-007 (#3132) after #3107 is landed and generic framework is ready

Only after the pilots prove the abstraction should remaining categories be migrated engine-by-engine.

## 16. Definition of done for the interaction architecture

The migration is working when all of these are true:

- clicking a left-menu item resolves a stable FeatureId/InteractionProfile;
- feature-specific actions appear without feature-specific action-bar code;
- `+ Add` can represent direct, chooser, form, CAD-pick and wizard flows through one orchestration model;
- a pre-create popup/form is shown only when required by the selected recipe;
- routine editing lives in zero/one/two standard inspector slots;
- true floating windows are exceptional and centrally hosted;
- switching feature/project/selection does not leave stale panes or create sessions;
- property forms are schema-driven where practical;
- unsupported/incomplete profiles fail through deterministic guards instead of blank UI;
- Room and room-finish pilots preserve their intended business behavior while using the generic framework;
- adding the next feature is primarily a descriptor/recipe/schema task, not another Workspace rewrite.
