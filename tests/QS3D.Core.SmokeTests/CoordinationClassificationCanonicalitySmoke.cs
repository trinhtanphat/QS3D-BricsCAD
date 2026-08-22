using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationClassificationCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedClassificationText();
            RejectsEmbeddedControlCharacters();
            PreservesCanonicalClassificationText();
            PreservesCaseInsensitiveSameDisciplineFiltering();
        }

        private static void RejectsPaddedClassificationText()
        {
            var bounds = Box();
            Throws<ArgumentException>(() => new CoordinationElement("E-1", " MEP", "Pipe", "CHW", "Zone-A", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E-1", "MEP ", "Pipe", "CHW", "Zone-A", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E-1", "MEP", "\tPipe", "CHW", "Zone-A", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E-1", "MEP", "Pipe\t", "CHW", "Zone-A", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E-1", "MEP", "Pipe", "\rCHW", "Zone-A", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E-1", "MEP", "Pipe", "CHW\n", "Zone-A", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E-1", "MEP", "Pipe", "CHW", " Zone-A ", bounds));
        }

        private static void RejectsEmbeddedControlCharacters()
        {
            var bounds = Box();
            Throws<ArgumentException>(() => new CoordinationElement("E-1", "ME\tP", "Pipe", "CHW", "Zone-A", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E-1", "MEP", "Pi\npe", "CHW", "Zone-A", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E-1", "MEP", "Pipe", "C\rHW", "Zone-A", bounds));
            Throws<ArgumentException>(() => new CoordinationElement("E-1", "MEP", "Pipe", "CHW", "Zone\u0001A", bounds));
        }

        private static void PreservesCanonicalClassificationText()
        {
            var element = new CoordinationElement("E-1", "MEP", "Pipe", "CHW", "Zone-A", Box());
            Equal("MEP", element.Discipline, "discipline");
            Equal("Pipe", element.Category, "category");
            Equal("CHW", element.System, "system");
            Equal("Zone-A", element.Region, "region");
        }

        private static void PreservesCaseInsensitiveSameDisciplineFiltering()
        {
            var service = new ClashDetectionService();
            var elements = new[]
            {
                new CoordinationElement("A", "MEP", "Pipe", "CHW", "Zone-A", new AxisAlignedBox(0d, 0d, 0d, 2d, 2d, 2d)),
                new CoordinationElement("B", "mep", "Duct", "SA", "Zone-A", new AxisAlignedBox(1d, 1d, 1d, 3d, 3d, 3d))
            };

            Equal(0, service.Detect(elements).Count, "same-discipline default filter");
            Equal(1, service.Detect(elements, includeSameDiscipline: true).Count, "same-discipline opt-in");
        }

        private static AxisAlignedBox Box()
        {
            return new AxisAlignedBox(0d, 0d, 0d, 1d, 1d, 1d);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("CoordinationClassificationCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
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

            throw new InvalidOperationException("CoordinationClassificationCanonicalitySmoke expected " + typeof(TException).Name + ".");
        }
    }
}