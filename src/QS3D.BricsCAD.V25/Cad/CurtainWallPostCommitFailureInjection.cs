using System;
using System.Collections.Generic;

namespace QS3D.BricsCAD.V25.Cad
{
    /// <summary>
    /// Internal one-shot seam for the licensed LOCAL-002/P09 probe. Normal
    /// product configuration and process environment cannot arm this state.
    /// </summary>
    internal static class CurtainWallPostCommitFailureInjection
    {
        internal const string LiveFingerprint = "LIVE_FINGERPRINT";
        internal const string UiRefresh = "UI_REFRESH";

        private sealed class Ticket
        {
            public string Nonce { get; set; } = string.Empty;
            public string Phase { get; set; } = string.Empty;
        }

        private static readonly object Sync = new object();
        private static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            LiveFingerprint, UiRefresh
        };
        private static Ticket? Armed;
        private static Ticket? Consumed;

        internal static void Arm(string nonce, string phase)
        {
            Validate(nonce, phase);
            lock (Sync)
            {
                if (Armed != null || Consumed != null)
                    throw new InvalidOperationException("Curtain post-commit failure injection already has a pending ticket.");
                Armed = new Ticket { Nonce = nonce, Phase = phase };
            }
        }

        internal static void ThrowIfArmed(string phase)
        {
            if (!Allowed.Contains(phase))
                throw new ArgumentOutOfRangeException(nameof(phase), "Curtain post-commit failure phase is not allowlisted.");
            lock (Sync)
            {
                if (Armed == null || !string.Equals(Armed.Phase, phase, StringComparison.Ordinal)) return;
                Consumed = Armed;
                Armed = null;
            }
            throw new InvalidOperationException("Automation-injected Curtain post-commit failure at an allowlisted phase.");
        }

        internal static void RequireConsumed(string nonce, string phase)
        {
            Validate(nonce, phase);
            lock (Sync)
            {
                if (Armed != null || Consumed == null ||
                    !string.Equals(Consumed.Nonce, nonce, StringComparison.Ordinal) ||
                    !string.Equals(Consumed.Phase, phase, StringComparison.Ordinal))
                    throw new InvalidOperationException("Curtain post-commit failure ticket was not consumed at the expected phase.");
                Consumed = null;
            }
        }

        internal static void RequireIdle()
        {
            lock (Sync)
            {
                if (Armed != null || Consumed != null)
                    throw new InvalidOperationException("Curtain post-commit failure injection state is not idle.");
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
                throw new InvalidOperationException("Curtain post-commit failure nonce is invalid.");
            if (!Allowed.Contains(phase))
                throw new InvalidOperationException("Curtain post-commit failure phase is not allowlisted.");
        }
    }
}
