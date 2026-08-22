using System;
using Bricscad.ApplicationServices;
using QS3D.Core.Units;

namespace QS3D.BricsCAD.V25.Services
{
    /// <summary>
    /// One-shot confirmation bridge for the scope-validated LOCAL-001 probe.
    /// Normal QS3DUNITS calls never arm this bridge and continue through the
    /// interactive Editor.GetKeywords path.
    /// </summary>
    internal static class DrawingUnitAutomationConfirmation
    {
        private static readonly object Sync = new object();
        private static Document? _document;
        private static LengthUnit _unit;

        public static void Arm(Document document, LengthUnit unit)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!Enum.IsDefined(typeof(LengthUnit), unit)) throw new ArgumentOutOfRangeException(nameof(unit));
            lock (Sync)
            {
                if (_document != null)
                    throw new InvalidOperationException("A drawing-unit automation confirmation is already armed.");
                _document = document;
                _unit = unit;
            }
        }

        public static bool TryConsume(Document document, out LengthUnit unit)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            lock (Sync)
            {
                if (!ReferenceEquals(_document, document))
                {
                    unit = default(LengthUnit);
                    return false;
                }
                unit = _unit;
                _document = null;
                _unit = default(LengthUnit);
                return true;
            }
        }

        public static bool IsArmed(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            lock (Sync) return ReferenceEquals(_document, document);
        }
    }
}
