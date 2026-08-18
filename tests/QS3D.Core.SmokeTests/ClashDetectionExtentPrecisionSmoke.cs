using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class ClashDetectionExtentPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Run();
        }

        internal static void Run()
        {
            RejectsOverlapExtentThatLosesLowerOperand();
            PreservesOrdinaryFiniteOverlap();
        }

        private static void RejectsOverlapExtentThatLosesLowerOperand()
        {
            var service = new ClashDetectionService();
            var elements = new[]
            {
                Element("A", "Arch", new AxisAlignedBox(0d, 0d, 0d, 1e20, 10d, 10d)),
                Element("B", "Struct", new AxisAlignedBox(1d, 2d, 2d, 1e20, 8d, 8d))
            };

            Throws<InvalidOperationException>(() => service.Detect(elements));
        }

        private static void PreservesOrdinaryFiniteOverlap()
        {
            var service = new ClashDetectionService();
            var elements = new[]
            {
                Element("A", "Arch", new AxisAlignedBox(0d, 0d, 0d, 10d, 10d, 10d)),
                Element("B", "Struct", new AxisAlignedBox(2d, 2d, 2d, 8d, 8d, 8d))
            };

            var results = service.Detect(elements);
            if (results.Count != 1 || results[0].Kind != ClashKind.Hard)
                throw new InvalidOperationException("Expected one ordinary hard clash.");
            Equal(6d, results[0].OverlapXM, "ordinary overlap X");
            Equal(6d, results[0].OverlapYM, "ordinary overlap Y");
            Equal(6d, results[0].OverlapZM, "ordinary overlap Z");
        }

        private static CoordinationElement Element(string id, string discipline, AxisAlignedBox bounds)
        {
            return new CoordinationElement(id, discipline, "Test", "Test", "Test", bounds);
        }

        private static void Equal(double expected, double actual, string context)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    context + " expected " + expected + " but was " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
