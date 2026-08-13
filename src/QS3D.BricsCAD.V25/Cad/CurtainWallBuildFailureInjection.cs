using System;
using System.Collections.Generic;

namespace QS3D.BricsCAD.V25.Cad
{
    /// <summary>
    /// Internal one-shot seam for the licensed LOCAL-002/P08 probe. Nothing in
    /// normal product configuration or the process environment can arm it.
    /// </summary>
    internal static class CurtainWallBuildFailureInjection
    {
        internal const string SemanticRegeneration = "SEMANTIC_REGENERATION";
        internal const string LineHost = "LINE_HOST";
        internal const string PathHost = "PATH_HOST";
        internal const string LineFrame = "LINE_FRAME";
        internal const string PathFrame = "PATH_FRAME";
        internal const string LinePanel = "LINE_PANEL";
        internal const string PathPanel = "PATH_PANEL";

        private sealed class Ticket
        {
            public string Nonce { get; set; } = string.Empty;
            public string Phase { get; set; } = string.Empty;
        }

        private static readonly object Sync = new object();
        private static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            SemanticRegeneration, LineHost, PathHost, LineFrame, PathFrame, LinePanel, PathPanel
        };
        private static Ticket? Armed;
        private static Ticket? Consumed;

        internal static IReadOnlyCollection<string> AllowedPhases => Allowed;

        internal static void Arm(string nonce, string phase)
        {
            Validate(nonce, phase);
            lock (Sync)
            {
                if (Armed != null || Consumed != null)
                    throw new InvalidOperationException("Curtain failure injection already has a pending ticket.");
                Armed = new Ticket { Nonce = nonce, Phase = phase };
            }
        }

        internal static void ThrowIfArmed(string phase)
        {
            if (!Allowed.Contains(phase))
                throw new ArgumentOutOfRangeException(nameof(phase), "Curtain failure injection phase is not allowlisted.");
            lock (Sync)
            {
                if (Armed == null || !string.Equals(Armed.Phase, phase, StringComparison.Ordinal)) return;
                Consumed = Armed;
                Armed = null;
            }
            throw new InvalidOperationException("Automation-injected Curtain build failure at an allowlisted pre-commit phase.");
        }

        internal static void RequireConsumed(string nonce, string phase)
        {
            Validate(nonce, phase);
            lock (Sync)
            {
                if (Armed != null || Consumed == null ||
                    !string.Equals(Consumed.Nonce, nonce, StringComparison.Ordinal) ||
                    !string.Equals(Consumed.Phase, phase, StringComparison.Ordinal))
                    throw new InvalidOperationException("Curtain failure injection ticket was not consumed at the expected phase.");
                Consumed = null;
            }
        }

        internal static void RequireIdle()
        {
            lock (Sync)
            {
                if (Armed != null || Consumed != null)
                    throw new InvalidOperationException("Curtain failure injection state is not idle.");
            }
        }

        internal static void Clear(string nonce)
        {
            if (!Guid.TryParseExact(nonce, "N", out _)) return;
            lock (Sync)
            {
                if (Armed != null && string.Equals(Armed.Nonce, nonce, StringComparison.Ordinal)) Armed = null;
                if (Consumed != null && string.Equals(Consumed.Nonce, nonce, StringComparison.Ordinal)) Consumed = null;
            }
        }

        private static void Validate(string nonce, string phase)
        {
            if (!Guid.TryParseExact(nonce, "N", out _))
                throw new InvalidOperationException("Curtain failure injection nonce is invalid.");
            if (!Allowed.Contains(phase))
                throw new InvalidOperationException("Curtain failure injection phase is not allowlisted.");
        }
    }
}
