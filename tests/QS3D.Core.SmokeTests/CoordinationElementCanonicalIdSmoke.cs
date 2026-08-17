using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationElementCanonicalIdSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedElementIds();
            PreservesClassificationNormalization();
            PreservesCaseInsensitiveDuplicateDetection();
        }

        private static void RejectsPaddedElementIds()
        {
            var bounds = Box();
            var canonical = new CoordinationElement("E1", "Structural", "Wall", "S1", "R1", bounds);
            Equal("E1", canonical.ElementId, "canonical element id");

            Throws<ArgumentException>(() => new CoordinationElement(" E1", "Structural", "Wall", "S1", "R1", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E1 ", "Structural", "Wall", "S1", "R1", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("\tE1", "Structural", "Wall", "S1", "R1", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E1\n", "Structural", "Wall", "S1", "R1", bounds));
        }

        private static void PreservesClassificationNormalization()
        {
            var element = new CoordinationElement(
                "E2",
                " Structural ",
                " Wall ",
                " S2 ",
                " R2 ",
                Box());

            Equal("Structural", element.Discipline, "discipline normalization");
            Equal("Wall", element.Category, "category normalization");
            Equal("S2", element.System, "system normalization");
            Equal("R2", element.Region, "region normalization");
        }

        private static void PreservesCaseInsensitiveDuplicateDetection()
        {
            var detector = new ClashDetectionService();
            var bounds = Box();
            var elements = new[]
            {
                new CoordinationElement("E3", "Structural", "Wall", "S3", "R3", bounds),
                new CoordinationElement("e3", "MEP", "Pipe", "S4", "R4", bounds)
            };

            Throws<ArgumentException>(() => detector.Detect(elements));
        }

        private static AxisAlignedBox Box()
        {
            return new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("CoordinationElementCanonicalIdSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("CoordinationElementCanonicalIdSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
