#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
DIRECT = V25 / "McpCadDirectModelRuntime.cs"
SINGLE = V25 / "SingleFootingCommands.cs"
AGENT = V25 / "McpCadAgentRuntime.cs"
SERVER = V25 / "McpEmbeddedServerV2.cs"


def between(text: str, start: str, end: str) -> str:
    start_index = text.find(start)
    if start_index < 0:
        return ""
    end_index = text.find(end, start_index + len(start))
    return text[start_index:] if end_index < 0 else text[start_index:end_index]


def require(errors: list[str], condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


def main() -> int:
    missing = [path for path in (DIRECT, SINGLE, AGENT, SERVER) if not path.is_file()]
    if missing:
        for path in missing:
            print("ERROR: missing", path.relative_to(ROOT))
        return 1

    direct = DIRECT.read_text(encoding="utf-8")
    single = SINGLE.read_text(encoding="utf-8")
    agent = AGENT.read_text(encoding="utf-8")
    server = SERVER.read_text(encoding="utf-8")
    errors: list[str] = []

    descriptor_start = direct.find('yield return Descriptor(\n                "qs3d_place_single_footing"')
    descriptor_end = direct.find("        }\n\n        internal static string Call", descriptor_start)
    descriptor = "" if descriptor_start < 0 or descriptor_end < 0 else direct[descriptor_start:descriptor_end]
    require(errors, bool(descriptor), "direct Móng đơn MCP descriptor is missing")
    if descriptor:
        for token in (
            '"qs3d_place_single_footing"',
            'Numeric("x", "y") + "," + ConfirmProperty()',
            '\\"x\\",\\"y\\",\\"confirmMutation\\"',
            'Active Floor elevation is resolved by the shared Móng đơn authoring workflow.',
        ):
            require(errors, token in descriptor, "direct Móng đơn descriptor lost token: " + token)
        require(errors, 'Numeric("x", "y", "z")' not in descriptor, "direct Móng đơn descriptor must not expose z")
        require(errors, '\\"z\\"' not in descriptor, "direct Móng đơn required schema must not expose z")

    require(errors, '"qs3d_place_single_footing"' in between(direct, "private static readonly HashSet<string> Tools", "private static readonly HashSet<string> KnownCommandTokens"),
            "direct tool registry does not include qs3d_place_single_footing")
    require(errors, 'case "qs3d_place_single_footing": result = PlaceSingleFooting(body); break;' in direct,
            "direct tool dispatcher does not route qs3d_place_single_footing")

    place = between(direct, "private static string PlaceSingleFooting(string body)", "private static string Save()")
    require(errors, bool(place), "direct Móng đơn placement method is missing")
    if place:
        for token in (
            'var x = NumberRequired(body, "x");',
            'var y = NumberRequired(body, "y");',
            "var document = RequireDocument();",
            "EnsureAutomationRunning();",
            "SingleFootingCommands.PlaceActiveSingleFootingAt(document, new Point3d(x, y, 0d))",
            'RecordMutation(document, "qs3d-place-single-footing", "handle=" + handle);',
            '\\"elevationPolicy\\":\\"active-floor\\"',
        ):
            require(errors, token in place, "direct Móng đơn placement lost token: " + token)
        for forbidden in (
            'NumberRequired(body, "z")',
            "SendStringToExecute",
            "Editor.GetPoint",
            "qs3d_run_command",
            "QS3DDRAWSINGLEFOOTING",
        ):
            require(errors, forbidden not in place, "direct Móng đơn placement must not use prompt/command/Z path: " + forbidden)

    number_required = between(direct, "private static double NumberRequired", "private static string LayerOptional")
    require(errors, "McpTopLevelJson.TryExtractDouble" in number_required, "direct numeric validation no longer uses bounded JSON double extraction")
    require(errors, "must be a finite number" in number_required, "direct numeric validation no longer fails closed on missing/non-finite coordinates")

    bridge = between(single, "internal static string PlaceActiveSingleFootingAt(Document document, Point3d center)", "private static string PlaceOne(")
    place_one = between(single, "private static string PlaceOne(", "private static Solid3d BuildSolid(")
    require(errors, "return PlaceOne(document, project, family, dimensions, center);" in bridge,
            "one-shot Móng đơn bridge no longer reuses shared PlaceOne authoring")
    require(errors, "var baseElevationM = ResolveActiveFloorElevation(project);" in place_one,
            "shared Móng đơn authoring no longer resolves Active Floor elevation")
    require(errors, "SingleFootingBaseElevationM" in place_one,
            "shared Móng đơn authoring no longer records base elevation provenance")

    direct_dispatch = between(agent, "public static string Call", "private static string Mutation")
    for token in (
        "if (McpCadDirectModelRuntime.IsTool(tool))",
        "return Mutation(args, tool, () => McpCadDirectModelRuntime.Call(tool, args));",
    ):
        require(errors, token in direct_dispatch, "canonical MCP mutation dispatch lost token: " + token)
    require(errors, "McpCadAgentRuntime.EnsureCurrentMutationRunning();" in direct,
            "direct runtime no longer re-checks shared mutation epoch/emergency stop")
    require(errors, "foreach (var descriptor in McpCadDirectModelRuntime.ToolDescriptors())" in server,
            "embedded server no longer publishes direct-runtime descriptors")

    if errors:
        print("ERROR: MCP direct Móng đơn preflight failed:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP exposes prompt-free Móng đơn X/Y placement through canonical mutation/epoch routing while Active Floor elevation remains owned by shared authoring.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
