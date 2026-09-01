#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
DIRECT = V25 / "McpCadDirectModelRuntime.cs"
DOMAIN = V25 / "McpQs3dDomainRuntime.cs"
SINGLE = V25 / "SingleFootingCommands.cs"
AGENT = V25 / "McpCadAgentRuntime.cs"
SERVER = V25 / "McpEmbeddedServerV2.cs"


def require(errors, condition, message):
    if not condition:
        errors.append(message)


def between(text, start, end):
    first = text.find(start)
    if first < 0:
        return ""
    last = text.find(end, first + len(start))
    return text[first:] if last < 0 else text[first:last]


def main():
    missing = [path for path in (DIRECT, DOMAIN, SINGLE, AGENT, SERVER) if not path.is_file()]
    if missing:
        for path in missing:
            print("ERROR: missing", path.relative_to(ROOT))
        return 1

    direct = DIRECT.read_text(encoding="utf-8")
    domain = DOMAIN.read_text(encoding="utf-8")
    single = SINGLE.read_text(encoding="utf-8")
    agent = AGENT.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")
    errors = []

    direct_registry = between(direct, "private static readonly HashSet<string> Tools", "private static readonly HashSet<string> KnownCommandTokens")
    require(errors, '"qs3d_place_single_footing"' not in direct_registry,
            "Móng đơn business tool must not be registered in direct CAD runtime")
    require(errors, 'case "qs3d_place_single_footing"' not in direct,
            "Móng đơn business tool must not dispatch in direct CAD runtime")
    require(errors, "private static string PlaceSingleFooting" not in direct,
            "Móng đơn business implementation must not live in direct CAD runtime")

    for token in (
        'string.Equals(tool, "qs3d_place_single_footing", StringComparison.Ordinal)',
        'SingleFootingCommands.PlaceActiveSingleFootingAt(document, new Point3d(x, y, 0d))',
        'McpCadAgentRuntime.EnsureCurrentMutationRunning();',
        'McpCadAgentRuntime.AuditDomainMutation("qs3d_place_single_footing", "handle=" + handle);',
        '\\"elevationPolicy\\":\\"active-floor\\"',
    ):
        require(errors, token in domain, "QS3D Móng đơn domain runtime lost token: " + token)
    for forbidden in ('NumberRequired(body, "z")', "SendStringToExecute", "Editor.GetPoint"):
        placement = between(domain, "private static string PlaceSingleFooting", "private static Document RequireDocument")
        require(errors, forbidden not in placement, "QS3D Móng đơn placement must remain prompt-free XY authoring: " + forbidden)

    bridge = between(single, "internal static string PlaceActiveSingleFootingAt(Document document, Point3d center)", "private static string PlaceOne(")
    place_one = between(single, "private static string PlaceOne(", "private static Solid3d BuildSolid(")
    require(errors, "return PlaceOne(document, project, family, dimensions, center);" in bridge,
            "Móng đơn MCP bridge no longer reuses shared PlaceOne authoring")
    require(errors, "var baseElevationM = ResolveActiveFloorElevation(project);" in place_one,
            "shared Móng đơn authoring no longer resolves Active Floor elevation")
    require(errors, "SingleFootingBaseElevationM" in place_one,
            "shared Móng đơn authoring no longer records base elevation provenance")

    for token in (
        'case "qs3d_place_single_footing": return Mutation(args, tool, () => McpQs3dDomainRuntime.Call(tool, args));',
        'Tool("qs3d_place_single_footing"',
    ):
        require(errors, token in agent + server, "canonical MCP/domain routing lost token: " + token)

    if errors:
        print("ERROR: MCP QS3D-domain Móng đơn preflight failed:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: qs3d_place_single_footing is QS3D-domain owned, prompt-free, confirmation/epoch gated, and reuses shared Active Floor authoring while CAD-direct remains independent.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
