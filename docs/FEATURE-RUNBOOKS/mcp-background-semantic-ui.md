# MCP Background Semantic UI

Status: source/static contract for V25. Licensed BricsCAD runtime qualification remains LOCAL_ONLY.

## Purpose

Background Semantic UI extends the existing same-process BricsCAD Background Control so an MCP caller can discover and activate Ribbon, WPF, and other UI Automation-capable custom controls without taking over the user's desktop.

The compatibility surface stays on the existing tools. Discovery returns an exact elementPath for each bounded node.

- `bricscad_ui_text_snapshot` with `mode=semantic` returns a bounded semantic ControlView tree.
- `bricscad_ui_invoke` keeps the legacy Win32 `controlHandle` Button path and additionally accepts `windowHandle`, exact `elementPath`, `action`, `expectedControlType`, and `expectedAutomationId`.

The existing text snapshot and standard Win32 Button behavior remain available.

## Background-only safety contract

This path is restricted to an exact visible top-level window owned by the same-process BricsCAD host. It does not focus the target window, does not move the cursor, does not inject keyboard or mouse input, and uses no screenshot or OCR.

There is no implicit foreground fallback. An unsupported, stale, disabled, offscreen, process-mismatched, control-type-mismatched, automation-id-mismatched, or unavailable UI Automation target must fail closed rather than calling `desktop_*`.

Semantic discovery requires `confirmSensitiveRead=true`. Semantic mutation requires `confirmMutation=true` and runs through the shared mutation epoch/emergency barrier before and after the provider action.

The semantic runtime does not expose `ValuePattern` or `TextPattern` content, does not call `SetFocus`, does not capture pixels, and does not expose shell/process/file-system execution.

## Discovery

Example request shape:

```json
{
  "mode": "semantic",
  "windowHandle": "0000000000123456",
  "maxDepth": 5,
  "maxNodes": 120,
  "confirmSensitiveRead": true
}
```

`bricscad_ui_text_snapshot` returns nodes containing:

- exact `elementPath` (`root`, `0`, `0/1`, ...)
- `controlType`
- bounded `automationId`
- `enabled`
- `offscreen`
- currently supported `actions`

Traversal uses UI Automation `ControlView` and is bounded to `MaxDepth = 8` and `MaxNodes = 200`.

The discovery result intentionally does not read generic edit/document text through UI Automation.

## Semantic actions

Example request shape:

```json
{
  "windowHandle": "0000000000123456",
  "elementPath": "3/1/0",
  "action": "invoke",
  "expectedControlType": "Button",
  "expectedAutomationId": "RibbonCommand42",
  "confirmMutation": true
}
```

The supported provider actions are deliberately narrow:

- `InvokePattern` -> `invoke`
- `TogglePattern` -> `toggle`
- `SelectionItemPattern` -> `select`
- `ExpandCollapsePattern` -> `expand` / `collapse`

Before mutation, the runtime re-resolves the exact `elementPath`, confirms the target remains inside the current BricsCAD process, verifies `expectedControlType` and `expectedAutomationId`, verifies the requested action is currently supported, and checks that the element is enabled and onscreen.

After the action, the runtime re-checks the shared mutation/emergency barrier and revalidates BricsCAD window ownership. It does not require the old element to survive because a successful Ribbon command may close or rebuild its UI subtree.

## Legacy Win32 compatibility

A request containing only `controlHandle` plus `confirmMutation=true` still follows the existing bounded `BM_CLICK` path and still accepts only a visible standard Win32 `Button` owned by the current BricsCAD process.

No caller is forced to migrate legacy button automation to UI Automation.

## Failure model

The semantic path must fail closed for:

- invalid or non-top-level `windowHandle`
- another process/session target
- stale or malformed `elementPath`
- changed `controlType` or `automationId`
- unavailable UI Automation provider/pattern
- unsupported semantic action
- disabled/offscreen target
- mutation pause/emergency-stop between validation and completion

Failure never authorizes mouse, keyboard, screen, focus, clipboard, shell, process, or file-system fallback.

## Qualification

Static/hosted qualification covers source guards, project compilation with trusted references, deterministic source/runtime-independent tests, and CI policy checks.

It must not be reported as licensed BricsCAD runtime PASS.

Licensed BricsCAD runtime qualification remains LOCAL_ONLY and should exercise at least:

1. open a V25 licensed BricsCAD session with the plugin loaded;
2. obtain the BricsCAD top-level `windowHandle`;
3. call semantic discovery with `confirmSensitiveRead=true`;
4. select a harmless Ribbon/WPF control that exposes one of the allowlisted patterns;
5. invoke it in Background Control with exact `elementPath` plus expected metadata and `confirmMutation=true`;
6. verify the user's foreground window, cursor position, and keyboard focus were not taken over;
7. verify stale-path and expected-metadata mismatch cases fail closed;
8. verify Pause/Emergency Stop blocks semantic mutations;
9. verify no `desktop_*` fallback occurs.
