using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityGeometryExplanationAggregationPrecisionSmoke
    {
        internal static void Run()
        {
            PreservesLargeFirstRepresentableTotals();
            PreservesSmallFirstRepresentableTotals();
            PreservesDeductionAggregationAndValidation();
            OrdinarySelectorsRemainIsolated();
            FinalUnrepresentableTotalStillFailsClosed();
            NonFiniteAndNullInputsStillFailClosed();
        }

        private static void PreservesLargeFirstRepresentableTotals()
        {
            var explanation = Explanation(
                Face("A", 10000000000000000d, 0d, 10000000000000000d),
                Face("B", 1d, 0d, 1d),
                Face("C", 1d, 0d, 1d));

            Equal(10000000000000002d, explanation.GrossFormworkArea, "Large-first gross total");
            Equal(10000000000000002d, explanation.NetFormworkArea, "Large-first net total");
            Equal(0d, explanation.DeductionFormworkArea, "Large-first deduction total");
            explanation.Validate(new QuantityGeometryTolerances());
        }

        private static void PreservesSmallFirstRepresentableTotals()
        {
            var explanation = Explanation(
                Face("A", 1d, 0d, 1d),
                Face("B", 1d, 0d, 1d),
                Face("C", 10000000000000000d, 0d, 10000000000000000d));

            Equal(10000000000000002d, explanation.GrossFormworkArea, "Small-first gross total");
            Equal(10000000000000002d, explanation.NetFormworkArea, "Small-first net total");
            explanation.Validate(new QuantityGeometryTolerances());
        }

        private static void PreservesDeductionAggregationAndValidation()
        {
            var explanation = Explanation(
                Face("A", 10000000000000000d, 10000000000000000d, 0d),
                Face("B", 1d, 1d, 0d),
                Face("C", 1d, 1d, 0d));

            Equal(10000000000000002d, explanation.GrossFormworkArea, "Deduction-control gross total");
            Equal(10000000000000002d, explanation.DeductionFormworkArea, "Compensated deduction total");
            Equal(0d, explanation.NetFormworkArea, "Deduction-control net total");
            explanation.Validate(new QuantityGeometryTolerances());
        }

        private static void OrdinarySelectorsRemainIsolated()
        {
            var explanation = Explanation(
                Face("A", 10d, 3d, 7d),
                Face("B", 20d, 5d, 15d));

            Equal(30d, explanation.GrossFormworkArea, "Ordinary gross total");
            Equal(8d, explanation.DeductionFormworkArea, "Ordinary deduction total");
            Equal(22d, explanation.NetFormworkArea, "Ordinary net total");
            explanation.Validate(new QuantityGeometryTolerances());
        }

        private static void FinalUnrepresentableTotalStillFailsClosed()
        {
            var explanation = Explanation(
                Face("A", 9007199254740992d, 0d, 9007199254740992d),
                Face("B", 1d, 0d, 1d));

            Capture<OverflowException>(() => _ = explanation.GrossFormworkArea);
            Capture<OverflowException>(() => _ = explanation.NetFormworkArea);
            Capture<OverflowException>(() => explanation.Validate(new QuantityGeometryTolerances()));
        }

        private static void NonFiniteAndNullInputsStillFailClosed()
        {
            var nonFinite = Explanation(Face("A", double.PositiveInfinity, 0d, 0d));
            Capture<InvalidOperationException>(() => _ = nonFinite.GrossFormworkArea);
            Capture<InvalidOperationException>(() => nonFinite.Validate(new QuantityGeometryTolerances()));

            var nullFaces = new QuantityGeometryExplanation { FormworkFaces = null! };
            Capture<InvalidOperationException>(() => _ = nullFaces.GrossFormworkArea);
            Capture<InvalidOperationException>(() => nullFaces.Validate(new QuantityGeometryTolerances()));

            var nullEntry = new QuantityGeometryExplanation
            {
                FormworkFaces = new QuantityFormworkFaceExplanation[] { null! }
            };
            Capture<InvalidOperationException>(() => _ = nullEntry.NetFormworkArea);
        }

        private static QuantityGeometryExplanation Explanation(params QuantityFormworkFaceExplanation[] faces)
        {
            return new QuantityGeometryExplanation
            {
                GrossVolume = 0d,
                DeductionVolume = 0d,
                NetVolume = 0d,
                FormworkFaces = new List<QuantityFormworkFaceExplanation>(faces).AsReadOnly()
            };
        }

        private static QuantityFormworkFaceExplanation Face(string id, double gross, double deduction, double net)
        {
            return new QuantityFormworkFaceExplanation
            {
                FaceId = id,
                GrossArea = gross,
                DeductionArea = deduction,
                NetArea = net
            };
        }

        private static T Capture<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (expected.Equals(actual)) return;
            throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class QuantityGeometryExplanationAggregationPrecisionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QuantityGeometryExplanationAggregationPrecisionSmoke.Run();
        }
    }
}