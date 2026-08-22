#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.Core/Documentation/SemanticDocumentationTableBuilder.cs"
RENDERER = ROOT / "src/QS3D.Core/Documentation/SemanticTagRenderer.cs"
CONTEXT = ROOT / "src/QS3D.Core/Documentation/SemanticTagRenderContext.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticDocumentationTableSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/DOCUMENTATION-LAYER.md"
errors = []

for path in (BUILDER, RENDERER, CONTEXT, SMOKE, REG, DOC):
    if not path.is_file():
        errors.append("missing semantic documentation table contract file: " + str(path.relative_to(ROOT)))

if BUILDER.is_file():
    text = BUILDER.read_text(encoding="utf-8")
    for token in (
        "private const int MaxRows = 5000",
        "private const int MaxColumns = 32",
        "ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count",
        "var headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)",
        "var context = new SemanticTagRenderContext(project)",
        "elements.Add(context.ResolveElement(id))",
        "SemanticTagRenderer.Render(context, element, column.Template, allowEmpty: true)",
        "Cells = new List<string>(cells).AsReadOnly()",
        "Headers = new List<string>(headers).AsReadOnly()",
        "Rows = new List<SemanticDocumentationRow>(rows).AsReadOnly()",
        "return new SemanticDocumentationTable(",
    ):
        if token not in text:
            errors.append("SemanticDocumentationTableBuilder.cs missing bounded/indexed/immutable token: " + token)
    if "foreach (var candidate in project.Elements)" in text:
        errors.append("SemanticDocumentationTableBuilder.cs regressed to scanning all project elements for every requested row.")
    if "SemanticTagRenderer.Render(project, element, column.Template" in text:
        errors.append("SemanticDocumentationTableBuilder.cs regressed to rebuilding the semantic lookup context for every cell.")

if CONTEXT.is_file():
    text = CONTEXT.read_text(encoding="utf-8")
    for token in (
        "internal sealed class SemanticTagRenderContext",
        "Dictionary<string, ProjectElement>",
        "HashSet<string> _ambiguousElementIds",
        "public ProjectElement ResolveElement(string id)",
        "public void EnsureElement(ProjectElement element)",
        "private void EnsureFamilyIndex()",
        "private void EnsureFloorIndex()",
        "private void EnsureZoneIndex()",
        "StringComparer.OrdinalIgnoreCase",
    ):
        if token not in text:
            errors.append("SemanticTagRenderContext.cs missing indexed/fail-closed token: " + token)

if RENDERER.is_file():
    text = RENDERER.read_text(encoding="utf-8")
    for token in (
        "return Render(project, element, template, allowEmpty: false)",
        "var context = new SemanticTagRenderContext(project)",
        "internal static string Render(SemanticTagRenderContext context",
        "context.EnsureElement(element)",
        "context.ResolveFamily(element)",
        "context.ResolveFloor(element)",
        "context.ResolveZone(element)",
        "if (output.Length == 0 && !allowEmpty)",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)",
        'key.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)',
        'key.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)',
    ):
        if token not in text:
            errors.append("SemanticTagRenderer.cs lost indexed label/table rendering boundary: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ExplicitOrderAndTemplatesArePreserved",
        "BlankOptionalCellsAreAllowedWithoutWeakeningTagLabels",
        "DuplicateElementIdsFailClosed",
        "AmbiguousProjectElementFailsClosed",
        "DuplicateHeadersFailClosed",
        "GeneratedOwnershipPropertiesRemainBlocked",
        "OutputSnapshotsAreDefensivelyImmutable",
        "UnusedReferenceIndexesStayLazy",
        "sourceCells[0] = \"MUTATED\"",
        "((IList<string>)row.Cells)[0] = \"MUTATED\"",
        "((IList<SemanticDocumentationRow>)table.Rows).Clear()",
    ):
        if token not in text:
            errors.append("SemanticDocumentationTableSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "SemanticDocumentationTableSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("semantic documentation table smoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "SemanticDocumentationTableBuilder",
        "caller supplies an explicit ordered semantic element-ID list",
        "never creates CAD entities",
        "defensively copied",
        "**not** a second BQ/BBS/schedule calculation engine",
        "DWG tables — source-implemented native Table slice",
        "Still open for native table qualification/expansion:",
    ):
        if token not in text:
            errors.append("DOCUMENTATION-LAYER.md missing table/runtime/immutability boundary: " + token)

print("QS3D semantic documentation table preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: generic semantic documentation tables are bounded, explicitly ordered, deep read-only snapshots, use one indexed batch render context with lazy semantic-reference indexes, block generated handles and remain non-mutating while native DWG ownership stays a V25 gate.")
