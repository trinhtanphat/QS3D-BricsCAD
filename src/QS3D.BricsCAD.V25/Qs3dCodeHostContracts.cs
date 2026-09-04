using System;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Serializable QS3D Code host/session identity. Only scalar values cross the local IPC boundary.
    /// </summary>
    internal sealed class Qs3dCodeHostIdentity
    {
        internal Qs3dCodeHostIdentity(string hostId, string sessionId, int processId, string hostMajor)
        {
            HostId = hostId ?? string.Empty;
            SessionId = sessionId ?? string.Empty;
            ProcessId = processId;
            HostMajor = hostMajor ?? string.Empty;
        }

        public string HostId { get; private set; }
        public string SessionId { get; private set; }
        public int ProcessId { get; private set; }
        public string HostMajor { get; private set; }
    }

    /// <summary>
    /// Serializable active-drawing identity. No live host object escapes through this type.
    /// </summary>
    internal sealed class Qs3dCodeDocumentIdentity
    {
        internal Qs3dCodeDocumentIdentity(string drawingId, string displayName, bool isNamed)
        {
            DrawingId = drawingId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            IsNamed = isNamed;
        }

        public string DrawingId { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsNamed { get; private set; }
    }

    /// <summary>
    /// Bounded request contract accepted from authenticated local QS3D Code clients.
    /// </summary>
    internal sealed class Qs3dCodeHostRequest
    {
        public string OperationId { get; set; } = string.Empty;
        public string PermissionClass { get; set; } = string.Empty;
        public string HostId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string DrawingId { get; set; } = string.Empty;
        public string ArgumentsJson { get; set; } = string.Empty;
        public string WriterToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bounded result contract. Native host handles are represented only inside payload text produced by reviewed runtimes.
    /// </summary>
    internal sealed class Qs3dCodeHostResult
    {
        internal Qs3dCodeHostResult(
            bool ok,
            string operationId,
            Qs3dCodeHostIdentity hostIdentity,
            Qs3dCodeDocumentIdentity activeDocumentIdentity,
            string payloadJson,
            string errorCode,
            string message)
        {
            Ok = ok;
            OperationId = operationId ?? string.Empty;
            HostIdentity = hostIdentity;
            ActiveDocumentIdentity = activeDocumentIdentity;
            PayloadJson = payloadJson ?? string.Empty;
            ErrorCode = errorCode ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public bool Ok { get; private set; }
        public string OperationId { get; private set; }
        public Qs3dCodeHostIdentity HostIdentity { get; private set; }
        public Qs3dCodeDocumentIdentity ActiveDocumentIdentity { get; private set; }
        public string PayloadJson { get; private set; }
        public string ErrorCode { get; private set; }
        public string Message { get; private set; }
    }
}
