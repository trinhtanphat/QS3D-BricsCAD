# MCP Background Semantic UI

Status: source/static contract for V25. Licensed BricsCAD runtime qualification remains LOCAL_ONLY.

## Purpose

Background Semantic UI extends the existing same-process BricsCAD Background Control so an MCP caller can discover and activate Ribbon, WPF, and other UI Automation-capable custom controls without taking over the user's desktop.

The compatibility surface stays on the existing tools. Discovery returns an exact elementPath for each bounded node plus a freshness generation that must be bound to the next semantic mutation.

- `bricscad_ui_text_snapshot` with `mode=semantic` returns a bounded semantic ControlView tree and `discoveryGeneration`.
- `bricscad_ui_invoke` keeps the legacy Win32 `controlHandle` Button path and additionally accepts `windowHandle`, exact `elementPath`, `action`, `expectedControlType`, `expectedAutomationId`, and `expectedDiscoveryGeneration`.

The existing text snapshot and standard Win32 Button behavior remain available.

## Background-only safety contract

This path is restricted to an exact visible top-level window owned by the same-process BricsCAD host. It does not focus the target window, does not move the cursor, does not inject keyboard or mouse input, and uses no screenshot or OCR.

There is no implicit foreground fallback. An unsupported, stale, disabled, offscreen, process-mismatched, control-type-mismatched, automation-id-mismatched, discovery-generation-mismatched, active-document-mismatched, or unavailable UI Automation target must fail closed rather than calling `desktop_*`.

Semantic discovery requires `confirmSensitiveRead=true`. Semantic mutation requires `confirmMutation=true` and runs through the shared mutation epoch/emergency barrier, canonical actionId ACK ledger, and process-global writer.

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

The top-level response also returns `discoveryGeneration`. A fresh semantic discovery binds that generation to the exact same-process top-level target window, target UI thread, and active document observed when discovery completed.

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
  "expectedDiscoveryGeneration": 17,
  "confirmMutation": true,
  "actionId": "semantic-ribbon-17"
}
```

The supported provider actions are deliberately narrow:

- `InvokePattern` -> `invoke`
- `TogglePattern` -> `toggle`
- `SelectionItemPattern` -> `select`
- `ExpandCollapsePattern` -> `expand` / `collapse`

Before mutation, the runtime requires the `expectedDiscoveryGeneration` from fresh semantic discovery, re-resolves the exact `elementPath`, confirms the target remains inside the current BricsCAD process, verifies `expectedControlType` and `expectedAutomationId`, verifies the requested action is currently supported, and checks that the element is enabled and onscreen.

The runtime also re-checks active document affinity and the target window thread. A semantic provider call is rejected before invocation when the current native thread is the same target UI thread because same target UI thread self-UIA calls can deadlock.

Immediately before the UI Automation provider method is entered, the runtime atomically invalidates the accepted discovery generation. Every provider attempt therefore consumes its generation and returns or reports `requiresRediscovery=true`.

## Freshness, affinity, ACK, and retry contract

Semantic mutation reuses the repository-wide mutation wrapper. It does not create a second semantic idempotency protocol. The canonical actionId ACK ledger performs reservation/replay identity, while the canonical process-global writer prevents concurrent CAD mutations and the mutation epoch enforces Pause/Emergency Stop boundaries.

A missing or stale `expectedDiscoveryGeneration` fails before the provider attempt. A changed active document or changed target window/UI thread also fails closed before the provider boundary when detectable. The caller must perform fresh semantic discovery before submitting a new semantic action.

Once a provider attempt starts, the generation is invalid. There is **no automatic retry**. The caller must not blindly repeat the operation with a new actionId because the provider may already have changed BricsCAD UI or CAD state.

A successful provider return with the post-provider mutation/window/document/thread checks still matching is reported as `applicationStatus=provider-completed`. This means only that the allowlisted UI Automation provider call returned and the bounded postconditions remained valid. It does not prove that any command launched by Ribbon/WPF has finished changing the drawing, so the response explicitly keeps `cadStateVerified=false`, `retryAllowed=false`, and `requiresRediscovery=true`.

If the UI Automation provider throws after invocation begins, the semantic surface reports the stable bounded reason `provider-error` and classifies the application outcome as `uncertain`. If provider invocation returns but a required postcondition diverges, the stable reason is `postcondition-diverged` and the outcome is also `uncertain`. Both uncertainty classes keep `cadStateVerified=false`, `retryAllowed=false`, and `requiresRediscovery=true`.

For an uncertain provider attempt, the runtime writes the bounded uncertainty result into the existing Accepted ACK for the current actionId and then returns a stable redacted failure. The generic mutation wrapper already preserves an Accepted ACK after the process-global writer has been acquired, so retrying the same actionId replays the ACK instead of invoking the provider again. Recovery requires inspection of `cad_mutation_status`, application state, and a fresh semantic discovery; a human/agent decision is required before any distinct recovery actionId is submitted.

Raw UI Automation exception messages, stacks, inner exceptions, provider text, paths, or screenshots are not returned on this remote surface. Only stable bounded reason codes and non-sensitive target metadata are used.

## Legacy Win32 compatibility

A request containing only `controlHandle` plus `confirmMutation=true` still follows the existing bounded `BM_CLICK` path and still accepts only a visible standard Win32 `Button` owned by the current BricsCAD process.

`expectedDiscoveryGeneration` is required only by the semantic route. No caller is forced to migrate legacy button automation to UI Automation.

## Failure model

The semantic path must fail closed for:

- invalid or non-top-level `windowHandle`
- another process/session target
- stale or malformed `elementPath`
- missing/stale `expectedDiscoveryGeneration`
- changed `controlType` or `automationId`
- changed active document
- changed target UI thread
- same target UI thread self-provider invocation
- unavailable UI Automation provider/pattern
- unsupported semantic action
- disabled/offscreen target
- mutation pause/emergency-stop around the provider action

After the provider boundary, provider failure or postcondition divergence is **uncertain**, not success and not proof of no mutation.

Failure never authorizes mouse, keyboard, screen, focus, clipboard, shell, process, or file-system fallback.

## Qualification

Static/hosted qualification covers source guards, project compilation with trusted references, deterministic source/runtime-independent tests, freshness/ACK source contracts, and CI policy checks.

It must not be reported as licensed BricsCAD runtime PASS.

Licensed BricsCAD runtime qualification remains LOCAL_ONLY and should exercise at least:

1. open a V25 licensed BricsCAD session with the plugin loaded;
2. obtain the BricsCAD top-level `windowHandle`;
3. call semantic discovery with `confirmSensitiveRead=true` and record `discoveryGeneration`;
4. select a harmless Ribbon/WPF control that exposes one of the allowlisted patterns;
5. invoke it in Background Control with exact `elementPath`, expected metadata, matching `expectedDiscoveryGeneration`, explicit actionId, and `confirmMutation=true`;
6. verify a normal provider path reports `provider-completed`, `cadStateVerified=false`, `retryAllowed=false`, and `requiresRediscovery=true`;
7. verify stale generation, stale path, expected-metadata mismatch, active-document switch, and same target UI thread cases fail closed before provider invocation;
8. if a safe deterministic provider/postcondition failure can be induced, verify `uncertain` uses only `provider-error` or `postcondition-diverged`, keeps the actionId ACK Accepted, and replaying the same actionId does not invoke the provider again;
9. verify the user's foreground window, cursor position, and keyboard focus were not taken over;
10. verify Pause/Emergency Stop blocks semantic mutations;
11. verify no `desktop_*` fallback occurs.
