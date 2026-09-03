#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
V25 = ROOT / "src" / "QS3D.BricsCAD.V25"
SERVER = V25 / "McpEmbeddedServerV2.cs"
RUNTIME = V25 / "McpCadAgentRuntime.cs"
DOMAIN = V25 / "McpQs3dDomainRuntime.cs"
TOP_LEVEL_JSON = V25 / "McpTopLevelJson.cs"
V25_PROJECT = V25 / "QS3D.BricsCAD.V25.csproj"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"
ACCOUNT = V25 / "McpCloudflareAccountOnboarding.cs"
FALLBACK = V25 / "McpCloudflareOnboarding.cs"
CONNECTOR = V25 / "McpConnectorRibbonCommands.cs"
RESOLVER = V25 / "McpPublicEndpointResolver.cs"
PLUGIN = V25 / "PluginEntry.cs"


def read(path: Path) -> str:
    if not path.is_file():
        raise FileNotFoundError(str(path.relative_to(ROOT)))
    return path.read_text(encoding="utf-8")


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        return ""
    next_method = source.find("\n        private static ", start + len(signature))
    return source[start:] if next_method < 0 else source[start:next_method]


def main() -> int:
    errors: list[str] = []
    try:
        server = read(SERVER)
        runtime = read(RUNTIME)
        domain = read(DOMAIN)
        top_level_json = read(TOP_LEVEL_JSON)
        v25_project = read(V25_PROJECT)
        v26_project = read(V26_PROJECT)
        account = read(ACCOUNT)
        fallback = read(FALLBACK)
        connector = read(CONNECTOR)
        resolver = read(RESOLVER)
        plugin = read(PLUGIN)
    except FileNotFoundError as exc:
        print("ERROR: missing", exc)
        return 1

    required = {
        "resolver provider precedence": (resolver, "McpCloudflareAccountTunnelManager.PublicMcpUrl"),
        "resolver quick/token precedence": (resolver, "McpCloudflareTunnelManager.PublicMcpUrl"),
        "HTTPS-only public endpoint": (resolver, "Uri.UriSchemeHttps"),
        "loopback public rejection": (resolver, "uri.IsLoopback"),
        "literal public-address validation": (resolver, "IPAddress.TryParse(uri.Host"),
        "private/link-local literal rejection": (resolver, "IsPrivateOrLocalAddress"),
        "configured fallback snapshot": (resolver, "ConfiguredEnvironmentFallback"),
        "provider publication isolated from fallback": (resolver, "NormalizeCandidate(ConfiguredEnvironmentFallback)"),
        "canonical MCP path": (resolver, 'path = "/mcp"'),
        "process endpoint synchronization": (resolver, "EnvironmentVariableTarget.Process"),
        "startup endpoint publication": (plugin, "McpPublicEndpointResolver.Resolve()"),
        "connector resolver use": (connector, "McpPublicEndpointResolver.Resolve()"),
        "copy URL command": (connector, 'CommandMethod("QS3DMCPCOPYURL"'),
        "copy token command": (connector, 'CommandMethod("QS3DMCPCOPYTOKEN"'),
        "copy config command": (connector, 'CommandMethod("QS3DMCPCOPYCONFIG"'),
        "legacy settings hide bearer value": (connector, "Bearer token: [hidden; use QS3DMCPCOPYTOKEN]"),
        "generated guide starts from Agent Center": (connector, "1. Open MCP Agent Center from TOOL > MCP (AI)."),
        "generated guide uses click-first installer": (connector, "2. Click the automatic cloudflared install/update button if needed."),
        "live Cloudflare tunnel list": (account, 'RunCommand(executable, "tunnel list"'),
        "exact tunnel-name comparison": (account, "string.Equals(parts[1], name, StringComparison.OrdinalIgnoreCase)"),
        "missing tunnel credential fail-closed": (account, "máy này thiếu credential"),
        "DNS conflict fail-closed": (account, "QS3D không tự bỏ qua xung đột DNS"),
        "hostname-scoped ingress": (account, '"ingress:\\r\\n"'),
        "canonical named config writer": (account, "WriteCanonicalConfig"),
        "saved tunnel rewrites canonical config": (account, "WriteCanonicalConfig(id, hostname, credentials)"),
        "provision writes canonical config": (account, "WriteCanonicalConfig(tunnelId, hostname, credentials)"),
        "account setup verified one-click installer": (account, "McpCloudflaredBootstrapper.BeginInstall"),
        "named public URL requires live process": (account, "PublicMcpUrl => IsRunning"),
        "Quick Tunnel URL polling": (account, "DispatcherTimer"),
        "Quick Tunnel bounded poll": (account, "_quickUrlPollTicks >= 20"),
        "named tunnel output bound to process owner": (account, "HandleRunLine(process, args.Data, false)"),
        "named stale-process output rejection": (account, "if (!ReferenceEquals(_process, process)) return;"),
        "fallback process owner before exit events": (fallback, "EnableRaisingEvents = false"),
        "fallback process exit cleanup": (fallback, "HandleProcessExit(Process process)"),
        "fallback output bound to process owner": (fallback, "HandleLine(process, args.Data, discoverQuickUrl)"),
        "fallback stale-process output rejection": (fallback, "if (!ReferenceEquals(_process, process)) return;"),
        "fallback public URL requires live process": (fallback, "if (!IsRunning) return string.Empty;"),
        "compiled transport loopback listener": (server, "IPAddress.Loopback"),
        "compiled transport exact JSON media type": (server, "IsJsonContentType(contentType)"),
        "compiled transport bounded sessions": (server, "MaxSessions = 128"),
        "compiled transport bounded clients": (server, "MaxConcurrentClients = 16"),
        "compiled transport runtime delegation": (server, "McpCadAgentRuntime.Call(tool, arguments)"),
        "strict HTTP field-name validation": (server, "IsHttpFieldName(name)"),
        "singleton Content-Type header": (server, 'string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase)'),
        "singleton Transfer-Encoding header": (server, 'string.Equals(name, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase)'),
        "singleton Origin header": (server, 'string.Equals(name, "Origin", StringComparison.OrdinalIgnoreCase)'),
        "reject any Transfer-Encoding header": (server, 'if (headers.ContainsKey("Transfer-Encoding"))'),
        "Origin validation helper": (server, "private static bool IsAllowedOrigin("),
        "Origin URI parsing": (server, "Uri.TryCreate(origin, UriKind.Absolute"),
        "Origin loopback admission": (server, "uri.IsLoopback"),
        "Origin exact public resource admission": (server, "IsSameOriginAsPublicMcp(uri, publicMcpUrl)"),
        "Origin public-resource comparator": (server, "private static bool IsSameOriginAsPublicMcp("),
        "Origin rejection HTTP 403": (server, 'WriteResponse(stream, 403, "Forbidden"'),
        "strict UTF-8 request body": (server, "StrictUtf8.GetString(body)"),
        "invalid UTF-8 request rejection": (server, "Invalid UTF-8 in MCP HTTP body."),
        "hard HTTP header terminator cap": (server, "if (headerEnd + 4 > MaxHeaderBytes)"),
        "serialized MCP session state": (server, "private static readonly object SessionSync = new object();"),
        "atomic session creation helper": (server, "private static bool TryCreateSession("),
        "serialized session deletion helper": (server, "private static bool TryDeleteSession("),
        "protocol-version validation helper": (server, "private static bool TryValidateProtocolVersionHeader("),
        "serialized session refresh": (server, "Sessions.TryUpdate(sessionId, state, stored)"),
        "session validation status output": (server, "out int statusCode"),
        "unknown session HTTP 404": (server, "statusCode = 404;"),
        "404 session response": (server, 'sessionStatusCode == 404 ? "Not Found" : "Bad Request"'),
        "top-level JSON trailing-comma rejection": (top_level_json, "JSON object cannot end with a trailing comma."),
        "MCP arguments trailing-comma rejection": (top_level_json, "MCP arguments object cannot end with a trailing comma."),
        "strict RFC JSON whitespace helper": (top_level_json, "IsJsonWhitespace(source[index])"),
        "strict RFC JSON trim helper": (top_level_json, "TrimJsonWhitespace"),
        "recursive JSON object grammar": (top_level_json, "private static bool TrySkipJsonObject("),
        "recursive JSON array grammar": (top_level_json, "private static bool TrySkipJsonArray("),
        "recursive JSON primitive grammar": (top_level_json, "private static bool TrySkipJsonPrimitive("),
        "bounded recursive JSON depth": (top_level_json, "MaxJsonDepth = 64"),
        "JSON-RPC numeric id support": (top_level_json, "if (IsJsonNumberToken(raw)) return raw;"),
        "JSON-RPC invalid id rejection": (top_level_json, 'throw new InvalidOperationException("JSON-RPC id must be a string, number, or null.");'),
        "runtime foreground ESC fallback": (runtime, "TrySendEscapeFallback()"),
        "runtime emergency-stop latch": (runtime, "StopAutomation();"),
        "runtime mutation epoch context": (runtime, "private static readonly AsyncLocal<int?> MutationEpoch"),
        "runtime mutation epoch state": (runtime, "private static int _automationEpoch;"),
        "runtime epoch invalidation": (runtime, "Interlocked.Increment(ref _automationEpoch);"),
        "runtime mutation stop recheck": (runtime, "static void EnsureCurrentMutationRunning()"),
        "runtime mutation CAD dispatch": (runtime, "private static string InvokeCadMutation("),
        "runtime command token canonicalizer": (runtime, "private static string NormalizeCadCommandToken("),
        "runtime primary command canonicalization": (runtime, 'var command = NormalizeCadCommandToken(McpTopLevelJson.ExtractString(body, "command"));'),
        "runtime prompt command canonicalization": (runtime, "var commandLike = NormalizeCadCommandToken(trimmed);"),
        "runtime queued dispatch state": (runtime, "CadWorkQueued = 0"),
        "runtime running dispatch state": (runtime, "CadWorkRunning = 1"),
        "runtime cancelled-before-start state": (runtime, "CadWorkCancelledBeforeStart = 2"),
        "runtime atomic start claim": (runtime, "Interlocked.CompareExchange(ref item.DispatchState, CadWorkRunning, CadWorkQueued)"),
        "runtime atomic timeout cancellation": (runtime, "Interlocked.CompareExchange(ref item.DispatchState, CadWorkCancelledBeforeStart, CadWorkQueued)"),
        "runtime uncertain timeout truth": (runtime, "completion is uncertain"),
        "runtime no-auto-retry truth": (runtime, "Do not retry automatically"),
        "QS3D domain mutation stop recheck": (domain, "McpCadAgentRuntime.EnsureCurrentMutationRunning();"),
        "QS3D domain command validation": (domain, "Regex.IsMatch(command, McpCadAgentRuntime.Qs3dCommandPattern"),
        "QS3D domain command dispatch": (domain, 'document.SendStringToExecute(command + "\\n", true, false, true);'),
        "V25 legacy monolith exclusion": (v25_project, '<Compile Remove="McpEmbeddedServer.cs" />'),
        "V26 legacy monolith exclusion": (v26_project, "..\\QS3D.BricsCAD.V25\\McpEmbeddedServer.cs"),
    }
    for label, (text, token) in required.items():
        if token not in text:
            errors.append(f"missing {label}: {token}")

    handle_request_start = server.find("private static void HandleRequest(")
    origin_check = server.find("if (!IsAllowedOrigin(request.Headers, publicMcpUrl))", handle_request_start)
    health_route = server.find('request.Path, "/healthz"', handle_request_start)
    if handle_request_start < 0 or origin_check < 0 or health_route < 0 or origin_check > health_route:
        errors.append("MCP Origin validation must run before every route, including healthz")

    extract_string_start = top_level_json.find("internal static string ExtractString(")
    extract_string_end = top_level_json.find("internal static bool ExtractBoolean(", extract_string_start)
    if extract_string_start < 0 or extract_string_end < 0:
        errors.append("cannot inspect MCP top-level string extraction")
    else:
        extract_string = top_level_json[extract_string_start:extract_string_end]
        if "throw new InvalidOperationException(error);" in extract_string:
            errors.append("MCP string extraction still throws parser errors instead of failing closed to caller validation")
        if "return string.Empty;" not in extract_string:
            errors.append("MCP string extraction lacks fail-closed empty-value behavior for malformed input")

    create_session_start = server.find("private static bool TryCreateSession(")
    create_session_end = server.find("private static bool TryDeleteSession(", create_session_start)
    if create_session_start >= 0 and create_session_end > create_session_start:
        create_session = server[create_session_start:create_session_end]
        if "lock (SessionSync)" not in create_session or "Sessions.Count >= MaxSessions" not in create_session:
            errors.append("MCP session creation is not atomically capacity-checked under SessionSync")

    delete_handler_start = server.find('if (request.Method == "DELETE")')
    delete_handler_end = server.find('if (request.Method != "POST")', delete_handler_start)
    if delete_handler_start < 0 or delete_handler_end <= delete_handler_start:
        errors.append("cannot inspect MCP DELETE session handler")
    else:
        delete_handler = server[delete_handler_start:delete_handler_end]
        if "TryDeleteSession(request.Headers, sessionId, out sessionError, out sessionStatusCode)" not in delete_handler:
            errors.append("MCP DELETE does not atomically validate protocol version and session termination result")
        if 'sessionStatusCode == 404 ? "Not Found" : "Bad Request"' not in delete_handler:
            errors.append("MCP DELETE lacks distinct HTTP 400 protocol-version / 404 stale-session truth")

    delete_session_start = server.find("private static bool TryDeleteSession(")
    delete_session_end = server.find("private static bool TryValidateSession(", delete_session_start)
    if delete_session_start < 0 or delete_session_end <= delete_session_start:
        errors.append("cannot inspect MCP session deletion helper")
    else:
        delete_session = server[delete_session_start:delete_session_end]
        if "lock (SessionSync)" not in delete_session:
            errors.append("MCP session deletion is not serialized under SessionSync")
        if "CleanupSessionsLocked();" not in delete_session:
            errors.append("MCP session deletion does not expire stale sessions before deciding DELETE result")
        if "TryValidateProtocolVersionHeader(headers, stored.ProtocolVersion, out error)" not in delete_session:
            errors.append("MCP session deletion ignores invalid/unsupported MCP-Protocol-Version")
        if "Sessions.TryRemove(sessionId, out ignored)" not in delete_session:
            errors.append("MCP session deletion no longer removes the validated session under SessionSync")

    validate_session_start = server.find("private static bool TryValidateSession(")
    validate_session_end = server.find("private static bool TryValidateProtocolVersionHeader(", validate_session_start)
    if validate_session_start < 0 or validate_session_end <= validate_session_start:
        errors.append("cannot inspect MCP session validation/refresh")
    else:
        validate_session = server[validate_session_start:validate_session_end]
        if "lock (SessionSync)" not in validate_session:
            errors.append("MCP session validation/refresh is not serialized under SessionSync")
        if "TryValidateProtocolVersionHeader(headers, stored.ProtocolVersion, out error)" not in validate_session:
            errors.append("MCP session validation does not reject empty/mismatched protocol-version headers")
        if "!string.IsNullOrWhiteSpace(version)" in validate_session:
            errors.append("MCP session validation still accepts an explicitly empty MCP-Protocol-Version header")

    protocol_helper_start = server.find("private static bool TryValidateProtocolVersionHeader(")
    protocol_helper_end = server.find("private static void CleanupSessionsLocked(", protocol_helper_start)
    if protocol_helper_start < 0 or protocol_helper_end <= protocol_helper_start:
        errors.append("cannot inspect MCP protocol-version validation helper")
    else:
        protocol_helper = server[protocol_helper_start:protocol_helper_end]
        if 'headers.TryGetValue("MCP-Protocol-Version", out version)' not in protocol_helper:
            errors.append("MCP protocol-version helper does not inspect the HTTP header")
        if "string.Equals(version, expectedProtocolVersion, StringComparison.Ordinal)" not in protocol_helper:
            errors.append("MCP protocol-version helper does not require exact negotiated-version match")
        if "string.IsNullOrWhiteSpace(version)" in protocol_helper:
            errors.append("MCP protocol-version helper treats an explicitly empty header as absent")

    mutation = method_block(runtime, "private static string Mutation(")
    if not mutation:
        errors.append("cannot inspect MCP mutation gate")
    else:
        if "EnsureAutomationRunning();" not in mutation:
            errors.append("MCP mutation gate does not reject an already-stopped agent")
        if "MutationEpoch.Value = epoch;" not in mutation or "EnsureAutomationRunning(epoch);" not in mutation:
            errors.append("MCP mutation gate does not bind each confirmed mutation to the current stop epoch")

    invoke_mutation = method_block(runtime, "private static string InvokeCadMutation(")
    if not invoke_mutation or "EnsureAutomationRunning(epoch.Value);" not in invoke_mutation:
        errors.append("queued CAD mutations do not re-check the captured stop epoch at CAD-context execution")

    # Verify every native mutation owned by McpCadAgentRuntime individually. The old numeric
    # count included two QS3D mutations that now correctly live in McpQs3dDomainRuntime.
    native_mutation_methods = (
        "CreateLine", "CreateCircle", "CreateArc", "CreatePolyline", "CreateText", "CreateMText",
        "TransformEntity", "DeleteEntity", "SetEntityLayer", "LayerAction", "RunCadCommandSequence",
    )
    add_entity = method_block(runtime, "private static string AddEntity(")
    add_entity_dispatches = bool(
        add_entity
        and "private static string AddEntity(Func<Entity> entityFactory, string layer, string auditTool)" in add_entity
        and "return InvokeCadMutation(() =>" in add_entity
        and "var entity = entityFactory();" in add_entity
    )
    factory_mutation_methods = {
        "CreateLine", "CreateCircle", "CreateArc", "CreatePolyline", "CreateText", "CreateMText",
    }
    for method in native_mutation_methods:
        block = method_block(runtime, f"private static string {method}(")
        direct_dispatch = bool(block and "return InvokeCadMutation(" in block)
        factory_dispatch = bool(
            block
            and method in factory_mutation_methods
            and "return AddEntity(" in block
            and add_entity_dispatches
        )
        if not block or not (direct_dispatch or factory_dispatch):
            errors.append(f"native CAD mutation {method} bypasses the mutation-aware CAD dispatcher")

    normalize_command = method_block(runtime, "private static string NormalizeCadCommandToken(")
    if not normalize_command or "token[index] == '_' || token[index] == '.'" not in normalize_command:
        errors.append("CAD command token canonicalizer does not strip arbitrary leading global/English prefix sequences")
    if ".TrimStart('_').TrimStart('.')" in runtime:
        errors.append("CAD command guard still uses order-dependent prefix trimming that permits ._ command injection")

    ui_click = method_block(runtime, "private static string UiClick(")
    ui_type = method_block(runtime, "private static string UiType(")
    ui_key = method_block(runtime, "private static string UiKey(")
    unicode_text = method_block(runtime, "private static void SendUnicodeText(")
    if ui_click.count("EnsureCurrentMutationRunning();") < 2:
        errors.append("UI click does not re-check the stop epoch before cursor/input injection and repeated clicks")
    if ui_type.count("EnsureCurrentMutationRunning();") < 2:
        errors.append("UI typing does not re-check the stop epoch before text and optional Enter injection")
    if "EnsureCurrentMutationRunning();" not in ui_key:
        errors.append("UI key injection does not re-check the stop epoch")
    if "EnsureCurrentMutationRunning();" not in unicode_text:
        errors.append("long Unicode typing does not re-check the stop epoch for each injected character")

    emergency_stop = method_block(runtime, "private static string EmergencyStop(")
    if "StopAutomation();" not in emergency_stop:
        errors.append("Emergency Stop does not invalidate the mutation epoch before attempting ESC delivery")
    resume_agent = method_block(runtime, "private static string ResumeAgent(")
    if "Interlocked.Increment(ref _automationEpoch);" not in resume_agent or "_automationStopped = false;" not in resume_agent:
        errors.append("Agent resume does not advance the mutation epoch before reopening mutation admission")

    named_start = account.find("private static bool StartProcess")
    named_exit = account.find("private static void HandleProcessExit", named_start)
    if named_start < 0 or named_exit < 0:
        errors.append("cannot inspect Named Tunnel process startup ordering")
    else:
        named_block = account[named_start:named_exit]
        stdout_drain = named_block.find("process.BeginOutputReadLine();")
        stderr_drain = named_block.find("process.BeginErrorReadLine();")
        exit_events = named_block.find("process.EnableRaisingEvents = true;")
        if min(stdout_drain, stderr_drain, exit_events) < 0 or max(stdout_drain, stderr_drain) > exit_events:
            errors.append("Named Tunnel must begin stdout/stderr drain before enabling Exited callbacks")

    if 'contentType.StartsWith("application/json"' in server:
        errors.append("compiled MCP transport accepts application/json lookalike media types via prefix matching")
    if "rawArguments.Trim()" in server or "var candidate = raw.Trim();" in server:
        errors.append("compiled MCP transport must not re-normalize raw JSON object boundaries with string.Trim")
    if "char.IsWhiteSpace(source[index])" in top_level_json:
        errors.append("MCP JSON parser accepts non-RFC Unicode whitespace via char.IsWhiteSpace")
    if ".Trim()" in top_level_json:
        errors.append("MCP JSON parser must not normalize non-RFC Unicode whitespace via string.Trim")
    if 'IndexOf("already exists"' in account:
        errors.append("Cloudflare DNS conflict must not be silently accepted via 'already exists'")
    if '"Bearer token: " + McpEmbeddedServer.GetBearerToken()' in connector:
        errors.append("legacy settings must not render the raw bearer token; use explicit copy action")
    if '"1. Run QS3DMCPACCOUNTSETUP.' in connector:
        errors.append("generated guide must not make a typed BricsCAD setup command the default path")

    for source_name, text in (("transport", server), ("runtime", runtime), ("domain", domain)):
        for forbidden in ("powershell.exe", "cmd.exe", "Process.Start(", "mouse_event("):
            if forbidden in text:
                errors.append(f"compiled MCP {source_name} exposes forbidden OS execution/input token: {forbidden}")

    if errors:
        print("Production MCP hardening preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print(
        "PASS: compiled modular MCP transport/runtime use strict bounded HTTP framing/UTF-8, exact JSON "
        "media type admission, loopback-or-exact-public-resource Origin validation, strict recursive RFC JSON grammar, valid JSON-RPC ids, "
        "serialized bounded sessions, epoch-invalidated mutation dispatch/UI input, canonical command-prefix rejection, "
        "strict negotiated protocol-version validation and 404 expiry truth; CAD timeout/recovery, QS3D domain mutation "
        "boundaries and validated Cloudflare endpoint/onboarding boundaries remain fail-closed."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())