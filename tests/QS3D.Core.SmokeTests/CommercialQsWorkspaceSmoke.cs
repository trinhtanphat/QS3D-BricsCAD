using System;
using QS3D.Core.Commercial;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialQsWorkspaceSmoke
    {
        public static void Run()
        {
            WorkspaceRoundTripPreservesQsInputs();
            WorkspaceCalculatorDrivesVariationIpcAndFinalAccount();
        }

        private static void WorkspaceRoundTripPreservesQsInputs()
        {
            var state = Fixture();
            var serialized = CommercialQsWorkspaceCodec.Serialize(state);
            var restored = CommercialQsWorkspaceCodec.Deserialize(serialized);

            Equal("VND", restored.Currency, "Commercial workspace currency changed during persistence round-trip.");
            Equal(3, restored.Variations.Count, "Commercial workspace variation rows changed during persistence round-trip.");
            Equal(1, restored.ProgressItems.Count, "Commercial workspace progress rows changed during persistence round-trip.");
            Equal(2, restored.VariationCertifications.Count, "Commercial workspace certification rows changed during persistence round-trip.");
            Equal("IPC-007", restored.Ipc.CertificateId, "Commercial workspace IPC identity changed during persistence round-trip.");
            Equal("FA-001", restored.FinalAccount.FinalAccountId, "Commercial workspace final-account identity changed during persistence round-trip.");
            Equal(120m, restored.Variations[0].ApprovedAmount, "Commercial workspace approved variation amount changed during persistence round-trip.");
            Equal(30m, restored.ProgressItems[0].ClaimedThisPeriodQuantity, "Commercial workspace progress quantity changed during persistence round-trip.");
        }

        private static void WorkspaceCalculatorDrivesVariationIpcAndFinalAccount()
        {
            var state = Fixture();
            var register = CommercialQsWorkspaceCalculator.BuildVariationRegister(state);
            Equal(100m, register.ApprovedNetChange, "Workspace variation register must preserve approved additions and omissions.");

            var progress = CommercialQsWorkspaceCalculator.EvaluateProgress(state);
            Equal(300m, progress.GrossCertifiedThisPeriod, "Workspace progress gross must reuse the core progress service.");
            Equal(30m, progress.RetentionThisPeriod, "Workspace progress retention must reuse the core progress service.");

            var ipc = CommercialQsWorkspaceCalculator.CreateIpc(state);
            Equal(30m, ipc.VariationCertifiedThisPeriod, "Workspace IPC must certify signed approved variations through the core IPC service.");
            Equal(330m, ipc.GrossCertifiedThisPeriod, "Workspace IPC gross must combine progress and variations through the core IPC service.");
            Equal(290m, ipc.NetCertifiedThisPeriod, "Workspace IPC net must apply retention, release, recovery and deductions exactly once.");
            Equal(790m, ipc.CumulativeNetCertified, "Workspace IPC cumulative value must preserve prior certification.");

            var final = CommercialQsWorkspaceCalculator.ReconcileFinalAccount(state);
            Equal(1075m, final.FinalContractValue, "Workspace final account must reuse approved variations and final adjustment.");
            Equal(120m, final.AmountDue, "Workspace final account amount due must reuse the core reconciliation service.");
            Equal(0m, final.RecoveryDue, "Workspace final account must not fabricate recovery for a positive settlement.");
        }

        private static CommercialQsWorkspaceState Fixture()
        {
            var state = new CommercialQsWorkspaceState
            {
                Currency = "VND",
                Ipc = new CommercialIpcWorkspaceInput
                {
                    CertificateId = "IPC-007",
                    RetentionPercent = 10m,
                    VariationRetentionThisPeriod = 3m,
                    RetentionRelease = 5m,
                    AdvanceRecovery = 10m,
                    OtherDeductions = 2m,
                    PreviousNetCertified = 500m
                },
                FinalAccount = new CommercialFinalAccountWorkspaceInput
                {
                    FinalAccountId = "FA-001",
                    OriginalContractValue = 1000m,
                    FinalAdjustment = -25m,
                    PreviousGrossCertified = 1000m,
                    RetentionHeld = 50m,
                    RetentionRelease = 50m,
                    FinalDeductions = 5m
                }
            };

            state.Variations.Add(new CommercialVariationWorkspaceRow
            {
                VariationId = "VO-ADD",
                Description = "Approved additional scope",
                ProposedAmount = 120m,
                ApprovedAmount = 120m,
                Status = CommercialVariationStatus.Approved,
                RevisionId = "R1"
            });
            state.Variations.Add(new CommercialVariationWorkspaceRow
            {
                VariationId = "VO-REJECT",
                Description = "Rejected proposal",
                ProposedAmount = 90m,
                ApprovedAmount = 0m,
                Status = CommercialVariationStatus.Rejected,
                RevisionId = "R1"
            });
            state.Variations.Add(new CommercialVariationWorkspaceRow
            {
                VariationId = "VO-OMIT",
                Description = "Approved omission",
                ProposedAmount = -20m,
                ApprovedAmount = -20m,
                Status = CommercialVariationStatus.Approved,
                RevisionId = "R1"
            });

            state.ProgressItems.Add(new CommercialProgressWorkspaceRow
            {
                ItemCode = "BASE",
                Unit = "m",
                ContractQuantity = 100m,
                UnitRate = 10m,
                PreviousCertifiedQuantity = 20m,
                ClaimedThisPeriodQuantity = 30m
            });

            state.VariationCertifications.Add(new CommercialVariationCertificationWorkspaceRow
            {
                VariationId = "VO-ADD",
                PreviousCertified = 20m,
                CertifiedThisPeriod = 50m
            });
            state.VariationCertifications.Add(new CommercialVariationCertificationWorkspaceRow
            {
                VariationId = "VO-OMIT",
                PreviousCertified = 0m,
                CertifiedThisPeriod = -20m
            });

            return state;
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(decimal expected, decimal actual, string message)
        {
            if (expected != actual)
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
