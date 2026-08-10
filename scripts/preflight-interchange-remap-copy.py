#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.is_file():
        raise SystemExit(f"FAIL: missing {path}")
    return target.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {label}: missing {needle!r}")


policy = read("src/QS3D.Core/Export/ProjectInterchangeSemanticReferencePolicy.cs")
core = read("src/QS3D.Core/Export/ProjectInterchangeRemapCopyImporter.cs")
command = read("src/QS3D.BricsCAD.V25/ProjectInterchangeRemapCopyCommands.cs")
smoke = read("tests/QS3D.Core.SmokeTests/ProjectInterchangeRemapCopySmoke.cs")
registration = read("tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs")
doc = read("docs/INTERCHANGE-REMAP-COPY.md")
plan = read("docs/CONTINUE-ALL-PROJECT-LOGIC-2026-08-10.md")

require(policy, "ProjectFloorService.BottomLevelIdKey", "reference policy")
require(policy, "ProjectFloorService.TopLevelIdKey", "reference policy")
require(policy, "LooksLikeSemanticReferenceKey", "reference policy")

for token in (
    "ProjectInterchangeValidatedSnapshotReader.Read",
    "SHA256.Create",
    "ProjectStateSnapshot.Capture",
    "ImportInterchangeRemapCopy",
    "SourceHandlesToDiscard",
    "DrawingFingerprint = string.Empty",
    "element.MarkDirty(ElementDirtyFlags.All)",
    "ProjectInterchangeSemanticReferencePolicy.TryGetPropertyReference",
    "looks like a semantic identity reference",
    "snapshot.Restore(target)",
):
    require(core, token, "core remapped copy")

for prefix in ("RZ-", "RL-", "RF-", "RE-"):
    require(core, prefix, "deterministic identity prefixes")

require(command, '[CommandMethod("QS3DINTERCHANGEREMAP"', "adapter command")
require(command, "ProjectInterchangeJsonValidator.MaxFileBytes", "guarded file read")
require(command, "ProjectInterchangeRemapCopyImporter.Plan", "preview before mutation")
require(command, "MessageBoxButton.YesNo", "explicit confirmation")
require(command, "ProjectInterchangeRemapCopyImporter.Import", "explicit apply")
require(command, "chưa tự lưu .qsdb", "no auto-save boundary")

for token in (
    "DeterministicCopyPreservesTargetAndRemapsReferences",
    "UnknownReferenceLikePropertyFailsClosed",
    "NamespaceValidationFailsClosed",
    "SourceHandles.Count",
    "BottomLevelIdKey",
    "TopLevelIdKey",
):
    require(smoke, token, "smoke coverage")
require(registration, "ProjectInterchangeRemapCopySmoke.Run();", "smoke registration")

for token in (
    "source-implemented, runtime-unqualified",
    "not a live external link",
    "SourceHandles",
    "BottomLevelId",
    "TopLevelId",
    "LOCAL_ONLY V25 qualification",
):
    require(doc, token, "remapped-copy documentation")

for token in (
    "Semantic identity is authoritative",
    "References are typed",
    "CAD ownership never crosses files implicitly",
    "Mutation must be previewable and atomic",
    "Dirty/generated state is a safety signal",
):
    require(plan, token, "project logic plan")

print("PASS: interchange remapped-copy source contract")
