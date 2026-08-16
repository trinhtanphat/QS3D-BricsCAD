using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportNonFiniteRegressionSmoke
    {
        internal static void Run()
        {
            RejectsNaNWithoutFalsePositiveAssertion();
            RejectsPositiveInfinityWithoutFalsePositiveAssertion();
        }

        private static void RejectsNaNWithoutFalsePositiveAssertion()
        {
            AssertThrowsInvalidOperation(double.NaN, "NaN quantity must fail closed.");
        }

        private static void RejectsPositiveInfinityWithoutFalsePositiveAssertion()
        {
            AssertThrowsInvalidOperation(double.PositiveInfinity, "Infinite quantity must fail closed.");
        }

        private static void AssertThrowsInvalidOperation(double value, string message)
        {
            var family = new FamilyDefinition("Non-finite Beam", ElementCategory.Beam, "Concrete");
            try
            {
                QuantityReportBuilder.Group(new[]
                {
                    new ElementInstance("non-finite", family, "L1")
                    {
                        GrossConcreteM3 = value,
                        NetConcreteM3 = value,
                        LengthM = value
                    }
                });
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }

    internal static class QuantityReportNonFiniteRegressionRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QuantityReportNonFiniteRegressionSmoke.Run();
        }
    }
}
