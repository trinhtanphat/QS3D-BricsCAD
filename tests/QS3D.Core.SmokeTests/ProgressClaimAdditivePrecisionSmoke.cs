using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class ProgressClaimAdditivePrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            SwallowedCertifiedLineContributionFailsClosed();
            RepresentableLowOrderContributionRemainsAccepted();
            ExactZeroContributionRemainsAccepted();
            AccumulatedOverflowRemainsFailClosed();
            OrdinaryRetentionAndCappingRemainStable();
        }

        private static void SwallowedCertifiedLineContributionFailsClosed()
        {
            var contracts = new[]
            {
                new ProgressContractItem("A-LARGE", "ea", 1m, 70000000000000000000000000000m),
                new ProgressContractItem("B-SMALL", "ea", 1m, 0.1m)
            };
            var claims = new[]
            {
                new ProgressClaimLine("A-LARGE", 0m, 1m),
                new ProgressClaimLine("B-SMALL", 0m, 1m)
            };

            Throws<OverflowException>(() =>
                new ProgressClaimService().Evaluate(contracts, claims));
        }

        private static void RepresentableLowOrderContributionRemainsAccepted()
        {
            var result = new ProgressClaimService().Evaluate(
                new[]
                {
                    new ProgressContractItem("A-LARGE", "ea", 1m, 70000000000000000000000000000m),
                    new ProgressContractItem("B-ONE", "ea", 1m, 1m)
                },
                new[]
                {
                    new ProgressClaimLine("A-LARGE", 0m, 1m),
                    new ProgressClaimLine("B-ONE", 0m, 1m)
                });

            Equal(
                70000000000000000000000000001m,
                result.GrossCertifiedThisPeriod,
                "Representable low-order progress contribution changed.");
            Equal(
                result.GrossCertifiedThisPeriod,
                result.NetCertifiedThisPeriod,
                "Zero-retention progress net changed.");
        }

        private static void ExactZeroContributionRemainsAccepted()
        {
            var result = new ProgressClaimService().Evaluate(
                new[]
                {
                    new ProgressContractItem("A-LARGE", "ea", 1m, 70000000000000000000000000000m),
                    new ProgressContractItem("B-ZERO", "ea", 1m, 123m)
                },
                new[]
                {
                    new ProgressClaimLine("A-LARGE", 0m, 1m),
                    new ProgressClaimLine("B-ZERO", 0m, 0m)
                });

            Equal(
                70000000000000000000000000000m,
                result.GrossCertifiedThisPeriod,
                "Exact-zero progress contribution must not change gross.");
        }

        private static void AccumulatedOverflowRemainsFailClosed()
        {
            Throws<OverflowException>(() =>
                new ProgressClaimService().Evaluate(
                    new[]
                    {
                        new ProgressContractItem("A-LARGE", "ea", 1m, 70000000000000000000000000000m),
                        new ProgressContractItem("B-OVERFLOW", "ea", 1m, 10000000000000000000000000000m)
                    },
                    new[]
                    {
                        new ProgressClaimLine("A-LARGE", 0m, 1m),
                        new ProgressClaimLine("B-OVERFLOW", 0m, 1m)
                    }));
        }

        private static void OrdinaryRetentionAndCappingRemainStable()
        {
            var result = new ProgressClaimService().Evaluate(
                new[]
                {
                    new ProgressContractItem("A", "ea", 10m, 100m),
                    new ProgressContractItem("B", "ea", 5m, 20m)
                },
                new[]
                {
                    new ProgressClaimLine("A", 3m, 9m),
                    new ProgressClaimLine("B", 0m, 2m)
                },
                retentionPercent: 10m);

            Equal(2, result.Lines.Count, "Ordinary progress result line count changed.");
            Equal("A", result.Lines[0].ItemCode, "Progress line ordering changed.");
            Equal(7m, result.Lines[0].CertifiedThisPeriodQuantity, "Progress capping changed.");
            Equal(2m, result.Lines[0].RejectedQuantity, "Progress rejected quantity changed.");
            Equal(0m, result.Lines[0].RemainingQuantity, "Progress remaining quantity changed.");
            Equal(700m, result.Lines[0].CertifiedThisPeriodValue, "Progress certified value changed.");
            Equal(740m, result.GrossCertifiedThisPeriod, "Progress gross changed.");
            Equal(74m, result.RetentionThisPeriod, "Progress retention changed.");
            Equal(666m, result.NetCertifiedThisPeriod, "Progress net changed.");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected: " + expected + "; actual: " + actual + ".");
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

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
