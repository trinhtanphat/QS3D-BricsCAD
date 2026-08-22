using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementVerticalPlacementOffsetKeySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CanonicalCustomKeyRemainsReadable();
            PaddedCustomKeyFailsClosed();
            BlankKeyRemainsRejected();
        }

        private static void CanonicalCustomKeyRemainsReadable()
        {
            var element = NewElement();
            const string key = "QS_Floor_Offset";
            element.Properties[key] = "1.25";

            Equal(1.25d, ElementVerticalPlacementService.ReadLevelOffset(element, key),
                "Canonical vertical-placement offset key no longer resolves its stored value.");
            Equal(0d, ElementVerticalPlacementService.ReadLevelOffset(element, "QS_Missing_Offset"),
                "Missing canonical offset key no longer falls back to zero.");
        }

        private static void PaddedCustomKeyFailsClosed()
        {
            var element = NewElement();
            const string key = "QS_Floor_Offset";
            element.Properties[key] = "2.5";

            Throws<ArgumentException>(() => ElementVerticalPlacementService.ReadLevelOffset(element, " " + key));
            Throws<ArgumentException>(() => ElementVerticalPlacementService.ReadLevelOffset(element, key + " "));
            Throws<ArgumentException>(() => ElementVerticalPlacementService.ReadLevelOffset(element, "\t" + key + "\r\n"));

            Equal(2.5d, ElementVerticalPlacementService.ReadLevelOffset(element, key),
                "Padded-key rejection changed the canonical stored offset.");
        }

        private static void BlankKeyRemainsRejected()
        {
            var element = NewElement();
            Throws<ArgumentException>(() => ElementVerticalPlacementService.ReadLevelOffset(element, string.Empty));
            Throws<ArgumentException>(() => ElementVerticalPlacementService.ReadLevelOffset(element, "   "));
        }

        private static ProjectElement NewElement() =>
            new ProjectElement("vertical-offset-key-smoke", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);

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

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal(double expected, double actual, string message)
        {
            if (BitConverter.DoubleToInt64Bits(expected) != BitConverter.DoubleToInt64Bits(actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
