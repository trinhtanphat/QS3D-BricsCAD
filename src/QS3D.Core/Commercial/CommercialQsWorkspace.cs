using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using QS3D.Core.Cost;

namespace QS3D.Core.Commercial
{
    public sealed class CommercialQsWorkspaceState
    {
        public const int CurrentSchemaVersion = 1;

        public CommercialQsWorkspaceState()
        {
            SchemaVersion = CurrentSchemaVersion;
            Currency = "VND";
            Variations = new List<CommercialVariationWorkspaceRow>();
            ProgressItems = new List<CommercialProgressWorkspaceRow>();
            VariationCertifications = new List<CommercialVariationCertificationWorkspaceRow>();
            Ipc = new CommercialIpcWorkspaceInput();
            FinalAccount = new CommercialFinalAccountWorkspaceInput();
        }

        public int SchemaVersion { get; set; }
        public string Currency { get; set; }
        public List<CommercialVariationWorkspaceRow> Variations { get; set; }
        public List<CommercialProgressWorkspaceRow> ProgressItems { get; set; }
        public List<CommercialVariationCertificationWorkspaceRow> VariationCertifications { get; set; }
        public CommercialIpcWorkspaceInput Ipc { get; set; }
        public CommercialFinalAccountWorkspaceInput FinalAccount { get; set; }
    }

    public sealed class CommercialVariationWorkspaceRow
    {
        public CommercialVariationWorkspaceRow()
        {
            VariationId = string.Empty;
            Description = string.Empty;
            RevisionId = "R1";
            Status = CommercialVariationStatus.Pending;
        }

        public string VariationId { get; set; }
        public string Description { get; set; }
        public decimal ProposedAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public CommercialVariationStatus Status { get; set; }
        public string RevisionId { get; set; }
    }

    public sealed class CommercialProgressWorkspaceRow
    {
        public CommercialProgressWorkspaceRow()
        {
            ItemCode = string.Empty;
            Unit = "m";
        }

        public string ItemCode { get; set; }
        public string Unit { get; set; }
        public decimal ContractQuantity { get; set; }
        public decimal UnitRate { get; set; }
        public decimal PreviousCertifiedQuantity { get; set; }
        public decimal ClaimedThisPeriodQuantity { get; set; }
    }

    public sealed class CommercialVariationCertificationWorkspaceRow
    {
        public CommercialVariationCertificationWorkspaceRow()
        {
            VariationId = string.Empty;
        }

        public string VariationId { get; set; }
        public decimal PreviousCertified { get; set; }
        public decimal CertifiedThisPeriod { get; set; }
    }

    public sealed class CommercialIpcWorkspaceInput
    {
        public CommercialIpcWorkspaceInput()
        {
            CertificateId = "IPC-001";
        }

        public string CertificateId { get; set; }
        public decimal RetentionPercent { get; set; }
        public decimal VariationRetentionThisPeriod { get; set; }
        public decimal RetentionRelease { get; set; }
        public decimal AdvanceRecovery { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal PreviousNetCertified { get; set; }
    }

    public sealed class CommercialFinalAccountWorkspaceInput
    {
        public CommercialFinalAccountWorkspaceInput()
        {
            FinalAccountId = "FA-001";
        }

        public string FinalAccountId { get; set; }
        public decimal OriginalContractValue { get; set; }
        public decimal FinalAdjustment { get; set; }
        public decimal PreviousGrossCertified { get; set; }
        public decimal RetentionHeld { get; set; }
        public decimal RetentionRelease { get; set; }
        public decimal FinalDeductions { get; set; }
    }

    public static class CommercialQsWorkspaceCalculator
    {
        public static CommercialVariationRegister BuildVariationRegister(CommercialQsWorkspaceState state)
        {
            state = RequireState(state);
            var currency = RequireWorkspaceCurrency(state);
            var rows = state.Variations ?? throw new InvalidOperationException("Commercial workspace variation collection is unavailable.");
            var variations = new List<CommercialVariation>(rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null)
                    throw new InvalidOperationException("Commercial workspace variation row " + i + " is null.");
                var id = RequireText(row.VariationId, "variation id", i);
                var revisionId = RequireText(row.RevisionId, "variation revision", i);
                variations.Add(new CommercialVariation(
                    id,
                    RequireText(row.Description, "variation description", i),
                    currency,
                    row.ProposedAmount,
                    row.ApprovedAmount,
                    row.Status,
                    new CommercialRevisionRef("variation", id, revisionId)));
            }

            return new CommercialVariationRegister(currency, variations);
        }

        public static ProgressClaimResult EvaluateProgress(CommercialQsWorkspaceState state)
        {
            state = RequireState(state);
            var rows = state.ProgressItems ?? throw new InvalidOperationException("Commercial workspace progress collection is unavailable.");
            var contractItems = new List<ProgressContractItem>(rows.Count);
            var claimLines = new List<ProgressClaimLine>(rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null)
                    throw new InvalidOperationException("Commercial workspace progress row " + i + " is null.");
                var itemCode = RequireText(row.ItemCode, "progress item code", i);
                contractItems.Add(new ProgressContractItem(
                    itemCode,
                    RequireText(row.Unit, "progress unit", i),
                    row.ContractQuantity,
                    row.UnitRate));
                claimLines.Add(new ProgressClaimLine(
                    itemCode,
                    row.PreviousCertifiedQuantity,
                    row.ClaimedThisPeriodQuantity));
            }

            var ipc = state.Ipc ?? throw new InvalidOperationException("Commercial workspace IPC input is unavailable.");
            return new ProgressClaimService().Evaluate(contractItems, claimLines, ipc.RetentionPercent);
        }

        public static InterimPaymentCertificate CreateIpc(CommercialQsWorkspaceState state)
        {
            state = RequireState(state);
            var ipc = state.Ipc ?? throw new InvalidOperationException("Commercial workspace IPC input is unavailable.");
            var certificationRows = state.VariationCertifications ??
                throw new InvalidOperationException("Commercial workspace variation certification collection is unavailable.");
            var certificationLines = new List<VariationCertificationLine>(certificationRows.Count);
            for (var i = 0; i < certificationRows.Count; i++)
            {
                var row = certificationRows[i];
                if (row == null)
                    throw new InvalidOperationException("Commercial workspace variation certification row " + i + " is null.");
                certificationLines.Add(new VariationCertificationLine(
                    RequireText(row.VariationId, "variation certification id", i),
                    row.PreviousCertified,
                    row.CertifiedThisPeriod));
            }

            return new InterimPaymentCertificateService().Create(
                RequireText(ipc.CertificateId, "IPC certificate id"),
                RequireWorkspaceCurrency(state),
                EvaluateProgress(state),
                BuildVariationRegister(state),
                certificationLines,
                ipc.VariationRetentionThisPeriod,
                ipc.RetentionRelease,
                ipc.AdvanceRecovery,
                ipc.OtherDeductions,
                ipc.PreviousNetCertified);
        }

        public static FinalAccountResult ReconcileFinalAccount(CommercialQsWorkspaceState state)
        {
            state = RequireState(state);
            var finalAccount = state.FinalAccount ??
                throw new InvalidOperationException("Commercial workspace final-account input is unavailable.");
            return new FinalAccountService().Reconcile(
                RequireText(finalAccount.FinalAccountId, "final-account id"),
                RequireWorkspaceCurrency(state),
                finalAccount.OriginalContractValue,
                BuildVariationRegister(state),
                finalAccount.FinalAdjustment,
                finalAccount.PreviousGrossCertified,
                finalAccount.RetentionHeld,
                finalAccount.RetentionRelease,
                finalAccount.FinalDeductions);
        }

        private static CommercialQsWorkspaceState RequireState(CommercialQsWorkspaceState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.SchemaVersion != CommercialQsWorkspaceState.CurrentSchemaVersion)
                throw new InvalidOperationException(
                    "Unsupported commercial workspace schema version: " +
                    state.SchemaVersion.ToString(CultureInfo.InvariantCulture) + ".");
            return state;
        }

        private static string RequireWorkspaceCurrency(CommercialQsWorkspaceState state)
        {
            return RateBookContract.RequireCurrency(state.Currency, nameof(state.Currency));
        }

        private static string RequireText(string value, string label, int? index = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                var suffix = index.HasValue ? " at row " + index.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
                throw new InvalidOperationException("Commercial workspace " + label + suffix + " is required.");
            }
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Commercial workspace " + label + " must not contain outer whitespace.");
            return value;
        }
    }

    public static class CommercialQsWorkspaceCodec
    {
        public const int MaximumSerializedCharacters = 2 * 1024 * 1024;
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(CommercialQsWorkspaceState));

        public static string Serialize(CommercialQsWorkspaceState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.SchemaVersion != CommercialQsWorkspaceState.CurrentSchemaVersion)
                throw new InvalidOperationException("Only the current commercial workspace schema can be persisted.");

            var settings = new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = false
            };
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            using (var xmlWriter = XmlWriter.Create(writer, settings))
            {
                Serializer.Serialize(xmlWriter, state);
                var serialized = writer.ToString();
                if (serialized.Length > MaximumSerializedCharacters)
                    throw new InvalidOperationException("Commercial workspace payload exceeds the 2 MiB persistence limit.");
                return serialized;
            }
        }

        public static CommercialQsWorkspaceState Deserialize(string serialized)
        {
            if (string.IsNullOrWhiteSpace(serialized))
                return new CommercialQsWorkspaceState();
            if (serialized.Length > MaximumSerializedCharacters)
                throw new InvalidOperationException("Commercial workspace payload exceeds the 2 MiB persistence limit.");

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            CommercialQsWorkspaceState state;
            using (var reader = new StringReader(serialized))
            using (var xmlReader = XmlReader.Create(reader, settings))
            {
                state = Serializer.Deserialize(xmlReader) as CommercialQsWorkspaceState;
            }
            if (state == null)
                throw new InvalidOperationException("Commercial workspace payload did not contain a workspace state.");
            if (state.SchemaVersion != CommercialQsWorkspaceState.CurrentSchemaVersion)
                throw new InvalidOperationException(
                    "Unsupported commercial workspace schema version: " +
                    state.SchemaVersion.ToString(CultureInfo.InvariantCulture) + ".");

            if (state.Variations == null) state.Variations = new List<CommercialVariationWorkspaceRow>();
            if (state.ProgressItems == null) state.ProgressItems = new List<CommercialProgressWorkspaceRow>();
            if (state.VariationCertifications == null) state.VariationCertifications = new List<CommercialVariationCertificationWorkspaceRow>();
            if (state.Ipc == null) state.Ipc = new CommercialIpcWorkspaceInput();
            if (state.FinalAccount == null) state.FinalAccount = new CommercialFinalAccountWorkspaceInput();
            if (state.Currency == null) state.Currency = "VND";
            return state;
        }
    }
}
