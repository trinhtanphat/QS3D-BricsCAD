#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ElementVerticalPlacementService.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")

required = (
    "CaptureFloorGeneration(project)",
    "FindCapturedFloor(",
    "project.ChangeVersion",
    "project.Floors.Count",
    "StringComparer.OrdinalIgnoreCase",
)
for marker in required:
    if marker not in text:
        fail(f"vertical placement is missing floor-generation fence marker: {marker}")

if "ValidateFloorIdentityCollection(project);" in text:
    fail("vertical placement still validates the live floor catalog separately from level lookup")
if "FindFloor(project," in text:
    fail("vertical placement still performs live ProjectState floor lookup after validation")

# Hosted openings are one logical placement result. The host and opening must
# therefore resolve against the same detached floor generation instead of two
# independent public Resolve calls that can straddle a project-floor mutation.
hosted_start = text.index("public static HostedOpeningVerticalPlacement ResolveHostedOpening(\n            ProjectState project,\n            ProjectElement host,")
hosted_end = text.index("public static HostedOpeningVerticalPlacement ResolveHostedOpening(\n            ProjectState project,\n            ElementVerticalPlacement hostPlacement,", hosted_start)
hosted_block = text[hosted_start:hosted_end]
if "IReadOnlyDictionary<string, double>? floorGeneration = null;" not in hosted_block:
    fail("hosted opening resolution must model its optional shared floor generation as nullable")
if text.count("IReadOnlyDictionary<string, double>? floorGeneration") < 3:
    fail("all optional floor-generation holders/parameters must use the nullable reference contract")
if hosted_block.count("floorGeneration = CaptureFloorGeneration(project);") < 2:
    fail("hosted opening resolution must capture the shared floor generation for whichever participant first needs Level lookup")
if "var hostPlacement = ResolveCore(" not in hosted_block:
    fail("hosted opening host placement must consume the caller-held floor generation")
if "return ResolveHostedOpeningCore(" not in hosted_block:
    fail("hosted opening result must forward the caller-held floor generation to opening placement")
if hosted_block.count("floorGeneration);") < 2:
    fail("hosted opening resolution must pass the same floor-generation variable to host and opening resolution")
if "ResolveCore(" not in text:
    fail("vertical placement must expose an internal core path that can consume a caller-captured floor generation")
if "floorGeneration ?? CaptureFloorGeneration(project)" not in text:
    fail("single-element resolution must capture lazily while hosted resolution may supply its shared generation")

print("PASS: vertical placement and hosted openings resolve levels from one nullable fenced floor generation")
