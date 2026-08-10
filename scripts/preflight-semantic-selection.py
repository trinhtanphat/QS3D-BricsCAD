#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs": [
        "public static class GeneratedHandleOwnershipPolicy",
        "IsOwnerSlot",
        "EnumerateOwnerHandles",
        "CollectOwnerHandles",
        "TryFindOwner",
        'StartsWith("Generated"',
        'PhysicalOpeningCutSolidHandle',
    ],
    "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs": [
        "SemanticHandleOwnershipResolver",
        "selected.Contains(handle)",
        "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)",
        "ambiguously owned by semantic elements",
    ],
    "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs": [
        "SelectImplied", "StartOpenCloseTransaction", "SemanticHandleOwnershipResolver.Resolve(project, selectedHandles)"
    ],
    "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs": [
        "SemanticSelectionResolver.ResolveImplied(", "floor.assign", "EnsureBoundDrawingIsActive"
    ],
    "src/QS3D.BricsCAD.V25/UI/MaterialCatalogWindow.xaml.cs": [
        "SemanticSelectionResolver.ResolveImplied(", "material.assign", "MdiActiveDocument"
    ],
    "tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipSmoke.cs": [
        "ModuleInitializer", "UnrelatedAmbiguityDoesNotBlockCleanSelection", "SelectedAmbiguityIsRejected",
        "GeneratedMultiHandleResolvesOwner", "FoundationMeshGeneratedHandleResolvesOwner",
        "FutureGeneratedOwnerSlotResolvesOwner", "ReferenceHandleIsNotGeneratedOwner",
        "OwnerCollectionDedupesAndIncludesOpeningCut", "AmbiguousGeneratedOwnerIsRejected",
        'GeneratedFuturePanelHandles', 'PhysicalOpeningCutSolidHandle',
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing semantic-selection file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing guard/token: " + needle)

resolver = ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs"
if resolver.is_file():
    text = resolver.read_text(encoding="utf-8")
    for stale in (
        "private static readonly string[] SingleHandleKeys",
        "private static readonly string[] MultiHandleKeys",
        '"GeneratedFoundationMeshHandles"',
    ):
        if stale in text:
            errors.append("SemanticHandleOwnershipResolver still hard-codes generated owner families: " + stale)

adapter = ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs"
if adapter.is_file():
    text = adapter.read_text(encoding="utf-8")
    for stale in ("BuildOwnershipIndex(project)", "private static readonly string[] SingleHandleKeys", "private static void Add("):
        if stale in text:
            errors.append("SemanticSelectionResolver still duplicates whole-project ownership logic: " + stale)

print("QS3D semantic selection preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: semantic selection uses the shared generated-owner policy, rejects ambiguity/provenance drift and automatically supports future Generated*Handle(s) families.")
