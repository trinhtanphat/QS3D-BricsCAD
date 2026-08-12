# WPF XAML event-handler contract guard — 2026-08-12

## Why this guard exists

A WPF XAML document can name a code-behind callback while the matching C# partial class no longer contains that method. The markup still looks structurally valid, so the defect is easy to miss during source review and is only exposed later by build/load behavior. `RightPanel.xaml` previously demonstrated this failure mode with a declared `PreviewKeyDown` callback that had no implementation.

`preflight-wpf-xaml-event-contract.py` adds a source-only, repository-wide regression guard for that contract without changing any BricsCAD UI behavior.

## Contract checked

The preflight scans `src/QS3D.BricsCAD.V25/**/*.xaml` and, for each XAML document with `x:Class`:

1. parses the XAML and fails closed on malformed XML/XAML;
2. resolves the class name to C# files in the same source directory that declare that class, including split partials such as `RightPanel.Keyboard.cs`;
3. extracts literal WPF event callback identifiers from common routed/CLR event attributes;
4. also recognizes project/custom event hookups that follow the existing `OnXxx` callback convention, while excluding literal data/configuration attributes such as `Text`, `Content`, `Header`, `Tag`, `Command`, `Name` and `Value`;
5. handles `EventSetter Event="..." Handler="..."` explicitly;
6. requires each extracted callback to have a C# method declaration in the resolved class source set.

The method check intentionally looks for a declaration rather than a bare `HandlerName(` token, so an invocation elsewhere cannot hide a missing callback.

## Intentional boundaries

This guard checks literal XAML-to-code-behind contracts only. It does not try to interpret bindings, markup extensions, commands, generated BAML, runtime reflection, BricsCAD native APIs, DPI behavior or visual layout. XAML resource dictionaries without `x:Class` are parsed for well-formedness but do not require a code-behind class.

The guard is additive. It does not edit existing XAML, code-behind, palettes, Workspace, RightPanel, quantity UI, Ribbon, updater or domain services, and it does not create a replacement/fake model viewport. BricsCAD model space remains the product viewport.

## Runner integration

No `scripts/preflight-all.py` change is required. The existing aggregate runner discovers `scripts/preflight-*.py`, so this file participates automatically by naming convention.

## Validation performed remotely

The script was syntax-compiled and exercised with synthetic WPF fixtures covering:

- a valid class whose handlers are split across `Sample.xaml.cs` and `Sample.Keyboard.cs`;
- `Click`, `TextChanged`, `PreviewKeyDown` and `EventSetter` callbacks;
- non-event literals/bindings that must be ignored;
- a deliberately missing callback, which failed with the XAML path, element/event and candidate source filenames;
- malformed XAML, which failed closed with the parser diagnostic.

A full repository checkout is not available in the connector execution environment, so this change does **not** claim execution of the complete repository preflight suite or a BricsCAD V25/WPF runtime PASS. Native WPF/HiDPI/modeless qualification remains a local-agent responsibility under the repository's existing local qualification workflow.
