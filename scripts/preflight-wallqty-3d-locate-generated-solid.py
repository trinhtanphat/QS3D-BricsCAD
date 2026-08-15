#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WallQuantityWindow.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing WallQuantityWindow source: " + str(SOURCE.relative_to(ROOT)))
else:
    source = SOURCE.read_text(encoding="utf-8")

    locate_start = source.find("private void LocateSelected(")
    helper_start = source.find("private IReadOnlyList<string> Resolve3DLocateHandles(", locate_start)
    next_method = source.find("private QuantityReportRow ResolveCurrentRow(", helper_start)
    locate = source[locate_start:helper_start] if locate_start >= 0 and helper_start > locate_start else ""
    helper = source[helper_start:next_method] if helper_start >= 0 and next_method > helper_start else ""

    if not locate:
        errors.append("WallQuantityWindow missing LocateSelected before Resolve3DLocateHandles.")
    else:
        current_project = locate.find('var currentProject = EnsureCurrentProject("định vị Tường trong View 3D");')
        current_row = locate.find("var currentRow = ResolveCurrentRow(currentProject, displayedView);")
        resolve_call = locate.find("var handles = Resolve3DLocateHandles(currentProject, currentElement, currentRow);")
        select_call = locate.find("CadHandleService.Select(_document, handles)")
        zoom_call = locate.find('_document.SendStringToExecute("QS3DZOOMSELECTED ", false, false, false);')
        positions = (current_project, current_row, resolve_call, select_call, zoom_call)
        if min(positions) < 0 or not (current_project < current_row < resolve_call < select_call < zoom_call):
            errors.append("LocateSelected must revalidate the active project/semantic row and validated handles before selecting CAD objects, then queue QS3DZOOMSELECTED without reactivating the document.")
        if "SourceHandleResolver.Resolve" in locate:
            errors.append("LocateSelected must not bypass Resolve3DLocateHandles with direct source-handle fallback.")
        if '_document.SendStringToExecute("QS3DZOOMSELECTED ", true,' in locate:
            errors.append("LocateSelected must not reactivate the already-current document when queuing QS3DZOOMSELECTED because that can clear the implied selection.")

    if not helper:
        errors.append("WallQuantityWindow missing Resolve3DLocateHandles helper.")
    else:
        missing_property = helper.find("if (!currentElement.Properties.TryGetValue(generatedSolidHandleKey, out var rawGeneratedHandle))")
        source_fallback = helper.find("SourceHandleResolver.Resolve(currentProject, currentRow.ElementIds)", missing_property)
        fallback_return = helper.find("return sourceHandles;", source_fallback)
        stale_check = helper.find("if (currentElement.IsGeneratedSolidStale())", fallback_return)
        normalize = helper.find("CadHandleService.NormalizeHexHandle(rawGeneratedHandle)", stale_check)
        invalid_check = helper.find("if (normalized == null)", normalize)
        live_check = helper.find("CadHandleService.GetLiveSolidHandles(_document, new[] { normalized })", invalid_check)
        live_reject = helper.find("if (!liveSolidHandles.Contains(normalized))", live_check)
        ownership_lookup = helper.find("GeneratedGeometryService.FindMatchingOwnedHandles(", live_reject)
        project_id = helper.find("currentProject.ProjectId,", ownership_lookup)
        element_id = helper.find("currentElement.Id,", project_id)
        category = helper.find("currentElement.Category);", element_id)
        ownership_reject = helper.find("if (!ownershipMatches)", category)
        generated_return = helper.find("return new[] { normalized };", ownership_reject)

        positions = (
            missing_property,
            source_fallback,
            fallback_return,
            stale_check,
            normalize,
            invalid_check,
            live_check,
            live_reject,
            ownership_lookup,
            project_id,
            element_id,
            category,
            ownership_reject,
            generated_return,
        )
        if min(positions) < 0 or not (
            missing_property < source_fallback < fallback_return < stale_check < normalize < invalid_check
            < live_check < live_reject < ownership_lookup < project_id < element_id < category
            < ownership_reject < generated_return
        ):
            errors.append("Resolve3DLocateHandles must keep fallback confined to missing GeneratedSolidHandle, then fail closed through stale/hex/live/ownership validation before returning the generated Solid3d handle.")

        boundary = helper.find("return sourceHandles;\n            }\n\n            if (currentElement.IsGeneratedSolidStale())")
        if boundary < 0:
            errors.append("Source geometry fallback must terminate inside the missing-GeneratedSolidHandle branch before stale generated-solid validation begins.")

        if helper.count("SourceHandleResolver.Resolve(currentProject, currentRow.ElementIds)") != 1:
            errors.append("Resolve3DLocateHandles must contain exactly one source-handle fallback path.")

        for token in (
            'const string generatedSolidHandleKey = "GeneratedSolidHandle";',
            '"GeneratedSolidHandle của Tường tồn tại nhưng rỗng hoặc không phải handle hex hợp lệ; từ chối fallback sang hình học nguồn."',
            '"GeneratedSolidHandle của Tường không còn resolve tới Solid3d sống; từ chối fallback sang hình học nguồn."',
            '"GeneratedSolidHandle trỏ tới Solid3d sống nhưng QS3D ownership không khớp project/element/category; từ chối fallback sang hình học nguồn."',
            "StringComparison.OrdinalIgnoreCase",
        ):
            if token not in helper:
                errors.append("Resolve3DLocateHandles missing fail-closed generated-solid token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DWALLQTY 3D locate revalidates the live semantic/CAD identity, preserves implied selection while queuing QS3DZOOMSELECTED, prefers the owned live generated Solid3d, and falls back to source geometry only when no GeneratedSolidHandle property exists.")
