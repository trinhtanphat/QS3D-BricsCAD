using System;
using QS3D.Core.Commercial;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialQsSettlementSmoke
    {
        public static void Run()
        {
            VariationRegisterCountsApprovedChangesOnly();
            VariationRegisterRejectsDuplicateAndMixedCurrency();
            IpcReusesProgressClaimAndBoundsVariationCertification();
            FinalAccountReconcilesContractAndRetention();
        }

        private static void VariationRegisterCountsApprovedChangesOnly()
        {
            var register = Register();
            Equal(100m, register.ApprovedNetChange, "Approved variation net change must ignore rejected proposals and preserve omissions.");
            Equal(3, register.Variations.Count, "Variation register must preserve the reviewable lifecycle rows.");
        }

        private static void VariationRegisterRejectsDuplicateAndMixedCurrency()
        {
            Expect<ArgumentException>(
                () => _ = new CommercialVariationRegister("VND", new[]
                {
                    Approved("VO-01", 10m),
                    Approved("vo-01", 20m)
                }),
                "Duplicate variation IDs must fail closed case-insensitively.");

            Expect<InvalidOperationException>(
                () => _ = new CommercialVariationRegister("VND", new[]
                {
                    Approved("VO-VND", 10m),
                    new CommercialVariation(
                        "VO-USD",
                        "Foreign currency variation",
                        "USD",
                        10m,
                        10m,
                        CommercialVariationStatus.Approved,
                        Revision("VO-USD"))
                }),
                "Mixed-currency variation registers must fail closed.");
        }

        private static void IpcReusesProgressClaimAndBoundsVariationCertification()
        {
            var progress = new ProgressClaimService().Evaluate(
                new[] { new ProgressContractItem("BASE", "m", 100m, 10m) },
                new[] { new ProgressClaimLine("BASE", 20m, 30m) },
                retentionPercent: 10m);

            Equal(300m, progress.GrossCertifiedThisPeriod, "Progress fixture gross changed unexpectedly.");
            Equal(30m, progress.RetentionThisPeriod, "Progress fixture retention changed unexpectedly.");
            Equal(270m, progress.NetCertifiedThisPeriod, "Progress fixture net changed unexpectedly.");

            var register = Register();
            var ipc = new InterimPaymentCertificateService().Create(
                "IPC-007",
                "VND",
                progress,
                register,
                new[]
                {
                    new VariationCertificationLine("VO-ADD", 20m, 50m),
                    new VariationCertificationLine("VO-OMIT", 0m, -20m)
                },
                variationRetentionThisPeriod: 3m,
                retentionRelease: 5m,
                advanceRecovery: 10m,
                otherDeductions: 2m,
                previousNetCertified: 500m);

            Equal(30m, ipc.VariationCertifiedThisPeriod, "IPC must preserve signed approved-variation certification.");
            Equal(330m, ipc.GrossCertifiedThisPeriod, "IPC gross must combine existing progress gross with certified variations.");
            Equal(33m, ipc.RetentionThisPeriod, "IPC retention must retain the existing progress result plus explicit variation retention.");
            Equal(290m, ipc.NetCertifiedThisPeriod, "IPC net must apply explicit release/recovery/deductions exactly once.");
            Equal(790m, ipc.CumulativeNetCertified, "IPC cumulative net must reconcile with prior certified value.");

            Expect<InvalidOperationException>(
                () => _ = new InterimPaymentCertificateService().Create(
                    "IPC-OVER",
                    "VND",
                    progress,
                    register,
                    new[] { new VariationCertificationLine("VO-ADD", 30m, 100m) }),
                "IPC must reject cumulative variation certification beyond the approved amount.");

            Expect<InvalidOperationException>(
                () => _ = new InterimPaymentCertificateService().Create(
                    "IPC-REJECTED",
                    "VND",
                    progress,
                    register,
                    new[] { new VariationCertificationLine("VO-REJECT", 0m, 1m) }),
                "IPC must reject certification against non-approved variations.");
        }

        private static void FinalAccountReconcilesContractAndRetention()
        {
            var final = new FinalAccountService().Reconcile(
                "FA-001",
                "VND",
                originalContractValue: 1000m,
                variations: Register(),
                finalAdjustment: -25m,
                previousGrossCertified: 1000m,
                retentionHeld: 50m,
                retentionRelease: 50m,
                finalDeductions: 5m);

            Equal(1075m, final.FinalContractValue, "Final contract value must include approved variations and explicit final adjustment.");
            Equal(75m, final.GrossBalanceBeforeRetentionAndDeductions, "Final gross balance must reconcile prior gross certification.");
            Equal(0m, final.UnreleasedRetention, "Full retention release must leave no retained balance.");
            Equal(120m, final.AmountDue, "Final amount due must include retention release and final deductions exactly once.");
            Equal(0m, final.RecoveryDue, "Positive final settlement must not fabricate a recovery.");

            Expect<ArgumentOutOfRangeException>(
                () => _ = new FinalAccountService().Reconcile(
                    "FA-RETENTION",
                    "VND",
                    1000m,
                    Register(),
                    0m,
                    1000m,
                    50m,
                    51m,
                    0m),
                "Final account must reject retention release above the retained balance.");
        }

        private static CommercialVariationRegister Register()
        {
            return new CommercialVariationRegister("VND", new[]
            {
                new CommercialVariation(
                    "VO-ADD",
                    "Approved additional scope",
                    "VND",
                    120m,
                    120m,
                    CommercialVariationStatus.Approved,
                    Revision("VO-ADD")),
                new CommercialVariation(
                    "VO-REJECT",
                    "Rejected proposal",
                    "VND",
                    90m,
                    0m,
                    CommercialVariationStatus.Rejected,
                    Revision("VO-REJECT")),
                new CommercialVariation(
                    "VO-OMIT",
                    "Approved omission",
                    "VND",
                    -20m,
                    -20m,
                    CommercialVariationStatus.Approved,
                    Revision("VO-OMIT"))
            });
        }

        private static CommercialVariation Approved(string id, decimal amount)
        {
            return new CommercialVariation(
                id,
                "Approved variation " + id,
                "VND",
                amount,
                amount,
                CommercialVariationStatus.Approved,
                Revision(id));
        }

        private static CommercialRevisionRef Revision(string id)
        {
            return new CommercialRevisionRef("variation", id, "R1");
        }

        private static void Equal(decimal expected, decimal actual, string message)
        {
            if (expected != actual)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Expect<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new Exception(message);
        }
    }
}