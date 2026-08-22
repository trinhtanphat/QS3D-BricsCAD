using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripExchangeResultUnicodeIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsLoneHighSurrogate();
            RejectsLoneLowSurrogate();
            PreservesSupplementaryUnicodeExactly();
        }

        private static void RejectsLoneHighSurrogate()
        {
            Throws<ArgumentException>(() => new IfcRoundTripExchangeResult(
                "ifc-invalid-\uD800",
                IfcRoundTripResultState.Unmapped,
                null));
        }

        private static void RejectsLoneLowSurrogate()
        {
            Throws<ArgumentException>(() => new IfcRoundTripExchangeResult(
                "ifc-invalid-low",
                IfcRoundTripResultState.Unmapped,
                null,
                classificationIdentity: "class-invalid-\uDC00"));
        }

        private static void PreservesSupplementaryUnicodeExactly()
        {
            const string externalObjectId = "ifc-object-\uD83D\uDE80";
            const string stateDetail = "Unsupported-\uD83D\uDE80";
            const string classificationIdentity = "class-\uD83D\uDE80";

            var result = new IfcRoundTripExchangeResult(
                externalObjectId,
                IfcRoundTripResultState.Unsupported,
                null,
                stateDetail,
                classificationIdentity);

            Require(string.Equals(externalObjectId, result.ExternalObjectId, StringComparison.Ordinal),
                "IFC result changed a valid supplementary external identity.");
            Require(string.Equals(stateDetail, result.StateDetail, StringComparison.Ordinal),
                "IFC result changed valid supplementary state evidence.");
            Require(string.Equals(classificationIdentity, result.ClassificationIdentity, StringComparison.Ordinal),
                "IFC result changed a valid supplementary classification identity.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
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

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
