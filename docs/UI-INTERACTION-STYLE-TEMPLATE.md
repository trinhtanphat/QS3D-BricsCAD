# QS3D Interactive Surface Style Template

This template defines the default visual perimeter contract for interactive QS3D desktop UI surfaces. It applies to new work and to touched legacy controls unless a product-specific contract explicitly requires a different treatment.

## Rounded-border invariant

Every **visible interactive surface** must own exactly one visible perimeter with:

- a non-transparent border that remains distinguishable from the surrounding surface;
- rounded corners using the local UI radius token (5 px in the BLT Start Center unless a component family defines another approved token);
- hover/pressed/active feedback that stays inside the same rounded perimeter;
- no change to command routing, persistence, keyboard/focus, or CAD-state semantics solely to achieve the visual treatment.

This includes buttons, action cards that function as buttons, clickable recent-item rows, toggles, draggable thumbs/handles, and equivalent visible pointer-driven controls.

## Perimeter ownership

Use **one perimeter owner per visible interaction target**. A transparent nested `Button`, `RepeatButton`, hit-test proxy, or other implementation-only click surface may remain borderless when a compliant visible parent already owns the border and rounded corners. Do not add a second visible border around that nested proxy because it creates double outlines and mismatched hover geometry.

For scroll controls, the draggable thumb/handle is an interactive perimeter owner and must be visibly bordered and rounded. The surrounding track may also carry an outline for separation, but invisible paging hit-zones inside the track do not receive independent borders.

## BLT Start Center reference

`src/QS3D.BricsCAD.V25/UI/BltStartCenterPanel.cs` is the current reference implementation for this contract:

- quick-action cards (`Tạo dự án mới`, `Mở tệp dự án...`, `Lưu`, `Lưu thành...`) use a 1 px rounded visible frame;
- status navigation and status toggles use a rounded visible frame;
- recent-project rows use a full rounded perimeter rather than a bottom-only separator;
- the vertical scrollbar carries a visible outline and its draggable thumb uses a dedicated rounded-border template;
- transparent internal click proxies remain borderless because their visible parent owns the perimeter.

## Review checklist

When reviewing a touched interactive surface, verify all of the following:

1. The user can visually identify the complete boundary of the interactive target.
2. The visible boundary is rounded and uses the approved local radius.
3. There is only one visible perimeter for the target.
4. Hover/pressed/active visuals do not spill outside the rounded perimeter.
5. Drag handles/thumbs have their own visible rounded perimeter.
6. Invisible implementation-only hit zones do not introduce duplicate borders.
7. Functional behavior is unchanged unless the task explicitly owns a behavior change.
