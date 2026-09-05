using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Typed, fail-closed bridge from authenticated QS3D Code local clients to the active BricsCAD host.
    /// </summary>
    internal static class Qs3dCodeHostBridge
    {
        private const int MaxArgumentsCharacters = 65536;

        internal static Qs3dCodeHostResult Execute(Qs3dCodeHostRequest request)
        {
            Qs3dCodeHostIdentity? host = null;
            Qs3dCodeDocumentIdentity? active = null;
            var operationId = string.Empty;
            try
            {
                if (request == null) throw new InvalidOperationException("request_required: host request is required.");
                operationId = NormalizeToken(request.OperationId, 80);
                host = Qs3dCodeHostService.GetHostIdentity();
                active = CaptureActiveDocumentIdentity(host.SessionId);

                if (string.Equals(operationId, "host.identity", StringComparison.Ordinal))
                {
                    RequirePermission(request, "read");
                    RejectStale(request, host, active, false);
                    return Success(operationId, host, active, "{}");
                }

                if (string.Equals(request.PermissionClass, "emergency-stop", StringComparison.Ordinal))
                {
                    if (!string.Equals(operationId, "cad_agent_stop", StringComparison.Ordinal))
                        throw new InvalidOperationException("permission_denied: emergency-stop permission only admits cad_agent_stop.");
                    RejectStale(request, host, active, false);
                    Qs3dCodeHostService.EmergencyStop();
                    return Success(operationId, host, active, "{\"stopped\":true}");
                }

                RejectStale(request, host, active, true);
                if (string.Equals(request.PermissionClass, "read", StringComparison.Ordinal))
                    return Success(operationId, host, active, ExecuteRead(operationId, BoundArguments(request.ArgumentsJson)));

                RequirePermission(request, "cad-mutation");
                return Success(operationId, host, active, ExecuteMutation(request, operationId, host));
            }
            catch (StaleIdentityException ex)
            {
                return Failure(operationId, host, active, "stale_identity", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Failure(operationId, host, active, Classify(ex.Message), ex.Message);
            }
            catch (Exception ex)
            {
                McpDiagnosticHub.Record("qs3d-code", "error", "host-bridge-failure", ex.GetType().Name);
                return Failure(operationId, host, active, "host_error", "QS3D Code host operation failed. Inspect bounded diagnostics for details.");
            }
        }

        private static string ExecuteRead(string operationId, string arguments)
        {
            switch (operationId)
            {
                case "host.status":
                    return McpCadAgentRuntime.Call("bricscad_status", "{}");
                case "mcp.status":
                    return McpCadAgentRuntime.Call("mcp_status", arguments);
                case "drawing.active":
                    return McpCadAgentRuntime.Call("cad_active_document", "{}");
                case "drawing.selection":
                    return McpCadAgentRuntime.Call("cad_selection", "{}");
                case "diagnostics.tail":
                    return McpCadAgentRuntime.Call("cad_audit_tail", arguments);
                default:
                    throw new InvalidOperationException("operation_not_allowed: unsupported read operation.");
            }
        }

        private static string ExecuteMutation(
            Qs3dCodeHostRequest request,
            string operationId,
            Qs3dCodeHostIdentity host)
        {
            if (!IsBoundedMutationOperation(operationId))
                throw new InvalidOperationException("operation_not_allowed: unsupported mutation operation.");
            if (McpCadAgentRuntime.AutomationStopped)
                throw new InvalidOperationException("automation_stopped: CAD automation is emergency-stopped.");

            var arguments = BoundArguments(request.ArgumentsJson);
            using (McpCadMutationCoordinator.EnterMutation(
                request.WriterToken,
                operationId,
                detail => McpDiagnosticHub.Record("qs3d-code", "info", "writer-admission", detail)))
            {
                if (McpCadAgentRuntime.AutomationStopped)
                    throw new InvalidOperationException("automation_stopped: CAD automation stopped before the mutation could start.");
                var executionActive = CaptureActiveDocumentIdentity(host.SessionId);
                RejectStale(request, host, executionActive, true);
                return McpCadDirectModelRuntime.Call(operationId, arguments);
            }
        }

        private static bool IsBoundedMutationOperation(string operationId)
        {
            switch (operationId)
            {
                case "cad_create_box":
                case "cad_extrude":
                case "cad_boolean_union":
                case "cad_boolean_subtract":
                case "cad_boolean_intersect":
                case "cad_save":
                case "cad_save_as":
                    return true;
                default:
                    return false;
            }
        }

        private static Qs3dCodeDocumentIdentity CaptureActiveDocumentIdentity(string sessionId)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
                return new Qs3dCodeDocumentIdentity(string.Empty, string.Empty, false);

            var name = document.Name ?? string.Empty;
            var displayName = SafeLeaf(name);
            var runtimeId = RuntimeHelpers.GetHashCode(document).ToString(CultureInfo.InvariantCulture);
            var drawingId = HashIdentity(sessionId + "\n" + name + "\n" + runtimeId);
            return new Qs3dCodeDocumentIdentity(drawingId, displayName, Path.IsPathRooted(name));
        }

        private static void RejectStale(
            Qs3dCodeHostRequest request,
            Qs3dCodeHostIdentity host,
            Qs3dCodeDocumentIdentity active,
            bool requireDrawing)
        {
            if (!FixedEquals(request.HostId, host.HostId))
                throw new StaleIdentityException("Host identity changed; reload local QS3D Code host state.");
            if (!FixedEquals(request.SessionId, host.SessionId))
                throw new StaleIdentityException("Host session changed; reload local QS3D Code host state.");
            if (!requireDrawing) return;
            if (active.DrawingId.Length == 0)
                throw new StaleIdentityException("No active drawing is available for this operation.");
            if (!FixedEquals(request.DrawingId, active.DrawingId))
                throw new StaleIdentityException("Active drawing changed; refresh identity before dispatch.");
        }

        private static void RequirePermission(Qs3dCodeHostRequest request, string expected)
        {
            if (!string.Equals(request.PermissionClass, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("permission_denied: request permission class is not admitted for this operation.");
        }

        private static string BoundArguments(string? value)
        {
            var arguments = string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
            if (arguments.Length > MaxArgumentsCharacters || arguments.IndexOf('\0') >= 0)
                throw new InvalidOperationException("invalid_arguments: arguments exceed the local host boundary.");
            return arguments;
        }

        private static string NormalizeToken(string? value, int maxLength)
        {
            var normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new InvalidOperationException("operation_required: operationId is required.");
            if (normalized.Length > maxLength) throw new InvalidOperationException("operation_not_allowed: operationId exceeds bounds.");
            foreach (var ch in normalized)
            {
                if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.'))
                    throw new InvalidOperationException("operation_not_allowed: operationId contains unsupported characters.");
            }
            return normalized;
        }

        private static string HashIdentity(string? value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static string SafeLeaf(string? value)
        {
            try
            {
                var leaf = Path.GetFileName(value ?? string.Empty);
                return leaf.Length > 260 ? leaf.Substring(0, 260) : leaf;
            }
            catch { return string.Empty; }
        }

        private static bool FixedEquals(string? left, string? right)
        {
            var a = Encoding.UTF8.GetBytes(left ?? string.Empty);
            var b = Encoding.UTF8.GetBytes(right ?? string.Empty);
            var difference = a.Length ^ b.Length;
            var count = Math.Max(a.Length, b.Length);
            for (var i = 0; i < count; i++)
            {
                var av = i < a.Length ? a[i] : (byte)0;
                var bv = i < b.Length ? b[i] : (byte)0;
                difference |= av ^ bv;
            }
            return difference == 0;
        }

        private static Qs3dCodeHostResult Success(
            string operationId,
            Qs3dCodeHostIdentity host,
            Qs3dCodeDocumentIdentity active,
            string payload)
        {
            return new Qs3dCodeHostResult(true, operationId, host, active, payload, string.Empty, string.Empty);
        }

        private static Qs3dCodeHostResult Failure(
            string operationId,
            Qs3dCodeHostIdentity? host,
            Qs3dCodeDocumentIdentity? active,
            string code,
            string message)
        {
            return new Qs3dCodeHostResult(false, operationId, host, active, string.Empty, code, BoundMessage(message));
        }

        private static string BoundMessage(string? value)
        {
            var message = value ?? string.Empty;
            if (message.Length > 1200) message = message.Substring(0, 1200);
            return message.Replace("\r", " ").Replace("\n", " ");
        }

        private static string Classify(string? message)
        {
            var value = message ?? string.Empty;
            if (value.StartsWith("permission_denied:", StringComparison.Ordinal)) return "permission_denied";
            if (value.StartsWith("operation_", StringComparison.Ordinal)) return "operation_not_allowed";
            if (value.StartsWith("invalid_arguments:", StringComparison.Ordinal)) return "invalid_arguments";
            if (value.StartsWith("automation_stopped:", StringComparison.Ordinal)) return "automation_stopped";
            return "operation_failed";
        }

        private sealed class StaleIdentityException : InvalidOperationException
        {
            internal StaleIdentityException(string message) : base(message) { }
        }
    }
}
