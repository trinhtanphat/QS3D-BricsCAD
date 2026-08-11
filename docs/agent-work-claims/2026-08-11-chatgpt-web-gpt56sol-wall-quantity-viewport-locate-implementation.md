# Wall Quantity viewport locate — implementation summary

This branch implements the registered `ACTIVE` claim `2026-08-11-chatgpt-web-gpt56sol-wall-quantity-viewport-locate.md`.

- `Bám 3D` defaults on for list/grid selection.
- `Định vị 3D` and double-click provide explicit reveal.
- locate revalidates active document + pinned ProjectId + current semantic ElementId/category + detached current detail row before resolving current source Handles.
- only after revalidation does it call `CadHandleService.Select(...)` and queue `QS3DZOOMSELECTED`.
- the preflight now guards the revalidation/select/zoom order and the read-only boundary.
- no Core Reporting formulas, persistence, `Commands.cs`, Ribbon, RightPanel or native geometry builders are changed.
