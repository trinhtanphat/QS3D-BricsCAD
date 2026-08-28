using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class DuplicatePairKeyIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DelimiterBearingElementIdsCannotCollapsePairIdentity();
            EscapeCharacterAndDelimiterCompositionIsInjective();
            PairIdentityRemainsInputOrderIndependent();
        }

        private static void DelimiterBearingElementIdsCannotCollapsePairIdentity()
        {
            var service = new DuplicateDetectionService();
            var firstBox = new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d);
            var secondBox = new AxisAlignedBox(10d, 0d, 0d, 11d, 1d, 1d);
            var result = service.Detect(new[]
            {
                Element("A", "PairOne", firstBox),
                Element("B|C", "PairOne", firstBox),
                Element("A|B", "PairTwo", secondBox),
                Element("C", "PairTwo", secondBox)
            });

            if (result.Pairs.Count != 2)
                throw new InvalidOperationException("DuplicatePairKeyIntegritySmoke: expected exactly two independent exact-geometry pairs.");

            RequireUniqueKeys(result, "delimiter repartition");
        }

        private static void EscapeCharacterAndDelimiterCompositionIsInjective()
        {
            var service = new DuplicateDetectionService();
            var firstBox = new AxisAlignedBox(20d, 0d, 0d, 21d, 1d, 1d);
            var secondBox = new AxisAlignedBox(30d, 0d, 0d, 31d, 1d, 1d);
            var result = service.Detect(new[]
            {
                Element("A", "LeadingDelimiter", firstBox),
                Element("|A", "LeadingDelimiter", firstBox),
                Element("A|", "TrailingDelimiter", secondBox),
                Element("A", "TrailingDelimiter", secondBox)
            });

            if (result.Pairs.Count != 2)
                throw new InvalidOperationException("DuplicatePairKeyIntegritySmoke: expected two escape-boundary pairs.");
            RequireUniqueKeys(result, "escape/delimiter boundary");

            var slashBox = new AxisAlignedBox(40d, 0d, 0d, 41d, 1d, 1d);
            var slashResult = service.Detect(new[]
            {
                Element("A\\|B", "Slash", slashBox),
                Element("C", "Slash", slashBox)
            });
            if (slashResult.Pairs.Count != 1 || slashResult.Pairs[0].PairKey.IndexOf("\\\\", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("DuplicatePairKeyIntegritySmoke: escape characters must themselves be escaped in PairKey.");
        }

        private static void PairIdentityRemainsInputOrderIndependent()
        {
            var service = new DuplicateDetectionService();
            var box = new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d);
            var first = service.Detect(new[]
            {
                Element("LEFT|SEGMENT", "Same", box),
                Element("RIGHT", "Same", box)
            });
            var second = service.Detect(new[]
            {
                Element("RIGHT", "Same", box),
                Element("LEFT|SEGMENT", "Same", box)
            });

            if (first.Pairs.Count != 1 || second.Pairs.Count != 1)
                throw new InvalidOperationException("DuplicatePairKeyIntegritySmoke: expected one duplicate pair for ordering regression.");
            if (!string.Equals(first.Pairs[0].PairKey, second.Pairs[0].PairKey, StringComparison.Ordinal))
                throw new InvalidOperationException("DuplicatePairKeyIntegritySmoke: PairKey changed with enumeration order.");
        }

        private static void RequireUniqueKeys(DuplicateDetectionResult result, string boundary)
        {
            var keys = result.Pairs.Select(pair => pair.PairKey).ToArray();
            if (keys.Distinct(StringComparer.Ordinal).Count() != result.Pairs.Count)
                throw new InvalidOperationException("DuplicatePairKeyIntegritySmoke: PairKey collision at " + boundary + ".");
        }

        private static CoordinationElement Element(string id, string category, AxisAlignedBox bounds)
        {
            return new CoordinationElement(id, "C03", category, "Default", "Model", bounds);
        }
    }
}
