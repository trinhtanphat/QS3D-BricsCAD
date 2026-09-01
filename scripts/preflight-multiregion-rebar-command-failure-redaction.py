#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/MultiRegionRebarCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing MultiRegionRebarCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")

    required = (
        '[CommandMethod("QS3DSLABREBAR3DMULTI", CommandFlags.UsePickSet)]',
        '[CommandMethod("QS3DFOUNDATIONREBAR3DMULTI", CommandFlags.UsePickSet)]',
        '[CommandMethod("QS3DMULTIREBARHEALTH", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)',
        'ExistingProjectMutationContext.Require(document, "Slab Multi-Region Rebar 3D")',
        'ExistingProjectMutationContext.Require(document, "Foundation Multi-Region Rebar 3D")',
        'EnsureSameProjectSnapshot(project.ProjectId, project.ChangeVersion, expectedProjectId, expectedChangeVersion',
        'SlabFoundationMultiRegionMeshSolidBuilder.BuildSlab(document, project)',
        'SlabFoundationMultiRegionMeshSolidBuilder.BuildFoundation(document, project)',
        'GeneratedMultiRegionRebarRuntimeHealthService.Inspect(document, project)',
        'catch (Exception)\n            {\n                Report(document, "QS3DSLABREBAR3DMULTI không thể hoàn tất.',
        'catch (Exception)\n            {\n                Report(document, "QS3DFOUNDATIONREBAR3DMULTI không thể hoàn tất.',
        'Report(document, "QS3DMULTIREBARHEALTH không thể hoàn tất kiểm tra. Project/native geometry không bị thay đổi.")',
        'private static void FinalizeUi(Document document, string operation, string message)',
        'var uiSyncFailed = false;',
        'try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }',
        'try { document.Editor.Regen(); } catch { uiSyncFailed = true; }',
        'try { PaletteCoordinator.SetStatus(message); } catch { uiSyncFailed = true; }',
        'TryWriteMessage(document, "\\nQS3D " + message);',
        'native update đã hoàn tất; một phần UI không thể đồng bộ.',
        'TryWriteMessage(document, "\\n  [" + issue.Severity',
        'if (issues.Count > 50) TryWriteMessage(document, "\\n  … health output truncated.");',
    )
    for token in required:
        if token not in text:
            errors.append("missing command redaction/lifecycle token: " + token)

    for forbidden in (
        "ex.Message",
        "exception.Message",
        "GetBaseException()",
        "StackTrace",
        "UI sync warning: ",
        'document.Editor.WriteMessage("\\n  ["',
        'if (issues.Count > 50) document.Editor.WriteMessage(',
    ):
        if forbidden in text:
            errors.append("user-visible Multi-Region rebar surface exposes raw/fallible host detail: " + forbidden)

    slab_preview = text.find('ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)')
    slab_mutation = text.find('ExistingProjectMutationContext.Require(document, "Slab Multi-Region Rebar 3D")')
    slab_build = text.find('SlabFoundationMultiRegionMeshSolidBuilder.BuildSlab(document, project)')
    slab_finalize = text.find('FinalizeUi(document, "Slab Multi-Region Rebar 3D", message)')
    if min(slab_preview, slab_mutation, slab_build, slab_finalize) < 0 or not slab_preview < slab_mutation < slab_build < slab_finalize:
        errors.append("Slab multi-region ordering must remain read-only preview -> mutation admission -> native build -> post-commit UI")

    foundation_start = text.find('public void BuildFoundationMultiRegionRebar3D()')
    foundation_preview = text.find('ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)', foundation_start)
    foundation_mutation = text.find('ExistingProjectMutationContext.Require(document, "Foundation Multi-Region Rebar 3D")', foundation_start)
    foundation_build = text.find('SlabFoundationMultiRegionMeshSolidBuilder.BuildFoundation(document, project)', foundation_start)
    foundation_finalize = text.find('FinalizeUi(document, "Foundation Multi-Region Rebar 3D", message)', foundation_start)
    if min(foundation_start, foundation_preview, foundation_mutation, foundation_build, foundation_finalize) < 0 or not foundation_start < foundation_preview < foundation_mutation < foundation_build < foundation_finalize:
        errors.append("Foundation multi-region ordering must remain read-only preview -> mutation admission -> native build -> post-commit UI")

    health_start = text.find('public void MultiRegionRebarHealth()')
    health_inspect = text.find('GeneratedMultiRegionRebarRuntimeHealthService.Inspect(document, project)', health_start)
    health_end = text.find('private static void EnsureSameProjectSnapshot', health_start)
    if min(health_start, health_inspect, health_end) < 0:
        errors.append("cannot resolve Health command block")
    else:
        health_block = text[health_start:health_end]
        for forbidden in (
            "ExistingProjectMutationContext.Require",
            "BuildSlab(",
            "BuildFoundation(",
            "project.Touch()",
            "transaction.Commit",
        ):
            if forbidden in health_block:
                errors.append("Health must remain read-only; found mutation token: " + forbidden)

print("QS3D Multi-Region rebar command failure-redaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Multi-Region Slab/Foundation commands preserve preview/freshness/native ordering, redact caught host failures, isolate post-commit UI synchronization, and keep Health read-only with fail-isolated presentation.")
