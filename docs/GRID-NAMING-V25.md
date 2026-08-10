# QS3D Grid naming — BricsCAD V25 interaction

Updated: 2026-08-10 (UTC+7)

## Command

`QS3DGRIDRENUMBER`

This command is the BricsCAD V25 interaction layer over the existing Core `GridNamingService`. It does not create another Grid catalog and does not replace `QS3DGRID`, which remains the source-capture command.

## Reviewed ordering contract

Grid numbering is intentionally explicit:

1. enter the number of Grid references to rename;
2. click each already-captured Grid source one-by-one in the exact desired order;
3. choose `Numeric` or `Alphabetic`;
4. choose the start index;
5. Numeric mode may choose zero-padding from 0 through 6;
6. optionally enter a prefix and suffix;
7. Core validates the entire plan before applying labels.

The command **không dùng thứ tự PICKFIRST** and does **không suy đoán spatial order** from coordinates, angle, handle value, creation order or current selection ordering. This preserves the Core rule that the caller must supply an explicit reviewed order.

Each clicked CAD entity must resolve to exactly one existing `ElementCategory.Grid` semantic element through its authoritative source Handle. Missing ownership, ambiguous ownership or selecting the same semantic Grid twice fails closed before renumbering.

## Atomic semantic boundary

All editor prompts finish before semantic mutation begins. Immediately before calling `GridNamingService.Renumber`, the command captures a `ProjectStateSnapshot`. If the Core operation throws, the project snapshot is restored; a rollback failure is surfaced as an aggregate failure rather than silently accepting partial state.

PICKFIRST restoration, selection sync, palette refresh and status output happen only after successful semantic mutation and are best-effort UI work. A UI refresh error therefore does not turn a completed semantic rename into a false rollback condition.

## Limits

- batch: 1..2000 Grid elements;
- sequence index: 1..999999;
- numeric zero-padding: 0..6;
- prefix/suffix and final-label validation remain owned by Core `GridNamingService`;
- labels remain case-insensitively unique across Grid elements outside the renumber batch.

## Relationship to existing Grid workflow

- `QS3DGRID`: capture LINE/ARC Grid reference sources;
- `QS3DGRIDRENUMBER`: assign deterministic semantic labels to an explicitly reviewed ordered set;
- `QS3DSYNCSOURCE`: reconcile native edits of tracked Grid sources;
- source Handles remain authoritative CAD references;
- no Grid solid is created or claimed by this naming command.

Native Grid bubbles/text, generated annotation ownership/replacement, automatic rectangular/radial systems, Grid intersections and DrawJig authoring remain separate work. This command deliberately does not fake those capabilities by writing unowned text into the DWG.

## V25 runtime gate

Source implementation is `REMOTE_DONE`; native runtime remains `LOCAL_ONLY` until an exact-SHA BricsCAD V25 matrix validates:

1. `QS3DGRID` capture of LINE and ARC references;
2. Numeric and Alphabetic `QS3DGRIDRENUMBER` ordering;
3. duplicate click and non-Grid click rejection;
4. duplicate external label rejection without partial mutation;
5. prefix/suffix and numeric padding boundaries;
6. cancel at every prompt without semantic mutation;
7. save/reopen label persistence;
8. PICKFIRST and palette inspection after success;
9. multi-DWG isolation and drawing fingerprint behavior;
10. Unicode/HiDPI command and palette behavior.

Do not describe native Grid naming as V25 runtime-certified until that matrix is actually executed on licensed BricsCAD V25.
