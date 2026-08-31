using System;
using System.Collections.Generic;

namespace QS3D.Core.Geometry
{
    internal static class GridSnapInputMaterializer
    {
        internal static List<GridReferenceCurve> Materialize(
            IEnumerable<GridReferenceCurve> curves,
            int maxCurves,
            string label)
        {
            if (curves == null) throw new ArgumentNullException(nameof(curves));
            if (maxCurves <= 0) throw new ArgumentOutOfRangeException(nameof(maxCurves));

            var admittedCount = ReadKnownCount(curves, label);
            if (admittedCount.HasValue && admittedCount.Value > maxCurves)
                throw new InvalidOperationException(label + " supports at most " + maxCurves + " curves.");

            var result = admittedCount.HasValue
                ? new List<GridReferenceCurve>(admittedCount.Value)
                : new List<GridReferenceCurve>();

            using (var enumerator = curves.GetEnumerator())
            {
                while (true)
                {
                    ValidateKnownCount(curves, admittedCount, label);
                    var moved = enumerator.MoveNext();
                    ValidateKnownCount(curves, admittedCount, label);
                    if (!moved) break;

                    if (admittedCount.HasValue && result.Count >= admittedCount.Value)
                        throw new InvalidOperationException(label + " produced more curves than its known Count.");
                    if (result.Count >= maxCurves)
                        throw new InvalidOperationException(label + " supports at most " + maxCurves + " curves.");

                    var curve = enumerator.Current;
                    ValidateKnownCount(curves, admittedCount, label);
                    result.Add(curve);
                }
            }

            ValidateKnownCount(curves, admittedCount, label);
            if (admittedCount.HasValue && result.Count != admittedCount.Value)
                throw new InvalidOperationException(string.Format(
                    label + " known Count reported {0} curves but traversal produced {1}.",
                    admittedCount.Value,
                    result.Count));

            return result;
        }

        private static int? ReadKnownCount(IEnumerable<GridReferenceCurve> curves, string label)
        {
            int? count = null;
            string source = string.Empty;

            var generic = curves as ICollection<GridReferenceCurve>;
            if (generic != null)
                BindCount(generic.Count, "ICollection<GridReferenceCurve>", label, ref count, ref source);

            var readOnly = curves as IReadOnlyCollection<GridReferenceCurve>;
            if (readOnly != null)
                BindCount(readOnly.Count, "IReadOnlyCollection<GridReferenceCurve>", label, ref count, ref source);

            var nonGeneric = curves as System.Collections.ICollection;
            if (nonGeneric != null)
                BindCount(nonGeneric.Count, "System.Collections.ICollection", label, ref count, ref source);

            return count;
        }

        private static void BindCount(
            int candidate,
            string candidateSource,
            string label,
            ref int? count,
            ref string source)
        {
            if (candidate < 0)
                throw new InvalidOperationException(label + " " + candidateSource + " Count cannot be negative.");
            if (count.HasValue && count.Value != candidate)
                throw new InvalidOperationException(
                    label + " exposes conflicting known Count values through " + source + " and " + candidateSource + ".");
            if (!count.HasValue)
            {
                count = candidate;
                source = candidateSource;
            }
        }

        private static void ValidateKnownCount(
            IEnumerable<GridReferenceCurve> curves,
            int? admittedCount,
            string label)
        {
            var current = ReadKnownCount(curves, label);
            if (current.HasValue != admittedCount.HasValue ||
                (current.HasValue && current.Value != admittedCount!.Value))
                throw new InvalidOperationException(label + " known Count changed during traversal.");
        }
    }
}
