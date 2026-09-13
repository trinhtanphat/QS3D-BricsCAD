using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using QS3D.Core.Cost;

namespace QS3D.Core.Commercial
{
    public enum SupplyChainPartyStatus
    {
        Prospective = 0,
        Approved = 1,
        Suspended = 2,
        Closed = 3
    }

    public enum CommitmentStatus
    {
        Draft = 0,
        Committed = 1,
        InProgress = 2,
        Complete = 3,
        Cancelled = 4
    }

    public enum DeliveryStatus
    {
        Planned = 0,
        Dispatched = 1,
        PartDelivered = 2,
        Delivered = 3,
        Cancelled = 4
    }

    public sealed class SupplyChainParty
    {
        public SupplyChainParty(string partyId, string name, SupplyChainPartyStatus status)
        {
            PartyId = CommercialGuard.RequireToken(partyId, nameof(partyId));
            Name = CommercialGuard.RequireCanonicalText(name, nameof(name));
            if (!Enum.IsDefined(typeof(SupplyChainPartyStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
        }

        public string PartyId { get; }
        public string Name { get; }
        public SupplyChainPartyStatus Status { get; }
    }

    public sealed class SubcontractCommitment
    {
        public SubcontractCommitment(
            string commitmentId,
            TenderAwardDecision award,
            SupplyChainParty party,
            decimal committedAmount,
            CommitmentStatus status,
            CommercialRevisionRef revision)
        {
            CommitmentId = CommercialGuard.RequireToken(commitmentId, nameof(commitmentId));
            Award = award ?? throw new ArgumentNullException(nameof(award));
            Party = party ?? throw new ArgumentNullException(nameof(party));
            Currency = RateBookContract.RequireCurrency(award.Currency, nameof(award));
            if (committedAmount < 0m) throw new ArgumentOutOfRangeException(nameof(committedAmount));
            if (!Enum.IsDefined(typeof(CommitmentStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            if (party.Status != SupplyChainPartyStatus.Approved)
                throw new InvalidOperationException("A subcontract commitment requires an approved supply-chain party.");
            if (!string.Equals(award.Bidder, party.Name, StringComparison.Ordinal))
                throw new InvalidOperationException("Commitment party must match the awarded bidder.");
            Revision = RequireRevision(revision, "subcontract-commitment", CommitmentId, nameof(revision));
            CommittedAmount = committedAmount;
            Status = status;
        }

        public string CommitmentId { get; }
        public TenderAwardDecision Award { get; }
        public SupplyChainParty Party { get; }
        public string Currency { get; }
        public decimal CommittedAmount { get; }
        public CommitmentStatus Status { get; }
        public CommercialRevisionRef Revision { get; }

        private static CommercialRevisionRef RequireRevision(CommercialRevisionRef revision, string kind, string id, string parameterName)
        {
            if (revision == null) throw new ArgumentNullException(parameterName);
            if (!string.Equals(revision.SourceKind, kind, StringComparison.Ordinal) ||
                !string.Equals(revision.SourceId, id, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Revision identity does not match the subcontract commitment.", parameterName);
            return revision;
        }
    }

    public sealed class PurchaseOrderDelivery
    {
        public PurchaseOrderDelivery(
            string orderId,
            string deliveryId,
            SubcontractCommitment commitment,
            decimal orderedQuantity,
            decimal deliveredQuantity,
            decimal actualCost,
            DeliveryStatus status,
            DateTime statusUtc,
            CommercialRevisionRef revision)
        {
            OrderId = CommercialGuard.RequireToken(orderId, nameof(orderId));
            DeliveryId = CommercialGuard.RequireToken(deliveryId, nameof(deliveryId));
            Commitment = commitment ?? throw new ArgumentNullException(nameof(commitment));
            if (orderedQuantity <= 0m) throw new ArgumentOutOfRangeException(nameof(orderedQuantity));
            if (deliveredQuantity < 0m || deliveredQuantity > orderedQuantity) throw new ArgumentOutOfRangeException(nameof(deliveredQuantity));
            if (actualCost < 0m) throw new ArgumentOutOfRangeException(nameof(actualCost));
            if (!Enum.IsDefined(typeof(DeliveryStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            if (status == DeliveryStatus.Delivered && deliveredQuantity != orderedQuantity)
                throw new InvalidOperationException("Delivered status requires full ordered quantity delivery.");
            if (status == DeliveryStatus.Cancelled && deliveredQuantity != 0m)
                throw new InvalidOperationException("Cancelled delivery cannot carry delivered quantity.");
            StatusUtc = CommercialGuard.RequireUtc(statusUtc, nameof(statusUtc));
            Revision = RequireRevision(revision, "purchase-order-delivery", DeliveryId, nameof(revision));
            OrderedQuantity = orderedQuantity;
            DeliveredQuantity = deliveredQuantity;
            ActualCost = actualCost;
            Status = status;
        }

        public string OrderId { get; }
        public string DeliveryId { get; }
        public SubcontractCommitment Commitment { get; }
        public decimal OrderedQuantity { get; }
        public decimal DeliveredQuantity { get; }
        public decimal ActualCost { get; }
        public DeliveryStatus Status { get; }
        public DateTime StatusUtc { get; }
        public CommercialRevisionRef Revision { get; }
        public decimal ProgressRatio => OrderedQuantity == 0m ? 0m : DeliveredQuantity / OrderedQuantity;

        private static CommercialRevisionRef RequireRevision(CommercialRevisionRef revision, string kind, string id, string parameterName)
        {
            if (revision == null) throw new ArgumentNullException(parameterName);
            if (!string.Equals(revision.SourceKind, kind, StringComparison.Ordinal) ||
                !string.Equals(revision.SourceId, id, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Revision identity does not match the delivery.", parameterName);
            return revision;
        }
    }

    public sealed class ConstructionProjectControlSnapshot
    {
        internal ConstructionProjectControlSnapshot(
            string projectId,
            string currency,
            decimal committedCost,
            decimal actualCost,
            decimal openCommitment,
            decimal forecastExposure,
            int orderCount,
            int deliveredOrderCount,
            IReadOnlyList<ProjectControlIntegrationRecord> integrationRecords)
        {
            ProjectId = projectId;
            Currency = currency;
            CommittedCost = committedCost;
            ActualCost = actualCost;
            OpenCommitment = openCommitment;
            ForecastExposure = forecastExposure;
            OrderCount = orderCount;
            DeliveredOrderCount = deliveredOrderCount;
            IntegrationRecords = integrationRecords;
        }

        public string ProjectId { get; }
        public string Currency { get; }
        public decimal CommittedCost { get; }
        public decimal ActualCost { get; }
        public decimal OpenCommitment { get; }
        public decimal ForecastExposure { get; }
        public int OrderCount { get; }
        public int DeliveredOrderCount { get; }
        public IReadOnlyList<ProjectControlIntegrationRecord> IntegrationRecords { get; }
    }

    public sealed class ProjectControlIntegrationRecord
    {
        internal ProjectControlIntegrationRecord(string projectId, PurchaseOrderDelivery delivery)
        {
            SchemaVersion = "qs3d-project-controls/v1";
            ProjectId = projectId;
            CommitmentId = delivery.Commitment.CommitmentId;
            OrderId = delivery.OrderId;
            DeliveryId = delivery.DeliveryId;
            PartyId = delivery.Commitment.Party.PartyId;
            Currency = delivery.Commitment.Currency;
            CommittedAmount = delivery.Commitment.CommittedAmount;
            ActualCost = delivery.ActualCost;
            ProgressRatio = delivery.ProgressRatio;
            Status = delivery.Status.ToString();
            RevisionId = delivery.Revision.RevisionId;
            StatusUtc = delivery.StatusUtc;
        }

        public string SchemaVersion { get; }
        public string ProjectId { get; }
        public string CommitmentId { get; }
        public string OrderId { get; }
        public string DeliveryId { get; }
        public string PartyId { get; }
        public string Currency { get; }
        public decimal CommittedAmount { get; }
        public decimal ActualCost { get; }
        public decimal ProgressRatio { get; }
        public string Status { get; }
        public string RevisionId { get; }
        public DateTime StatusUtc { get; }
    }

    public sealed class ConstructionLifecycleService
    {
        private const int MaximumEntries = 10000;

        public ConstructionProjectControlSnapshot BuildProjectControls(
            string projectId,
            IEnumerable<SubcontractCommitment> commitments,
            IEnumerable<PurchaseOrderDelivery> deliveries)
        {
            projectId = CommercialGuard.RequireToken(projectId, nameof(projectId));
            var commitmentSnapshot = CommercialGuard.Snapshot(commitments, nameof(commitments), MaximumEntries);
            var deliverySnapshot = CommercialGuard.Snapshot(deliveries, nameof(deliveries), MaximumEntries);
            if (commitmentSnapshot.Count == 0) throw new InvalidOperationException("Project controls require at least one commitment.");

            var byId = new Dictionary<string, SubcontractCommitment>(StringComparer.OrdinalIgnoreCase);
            var currency = commitmentSnapshot[0].Currency;
            decimal committed = 0m;
            for (var i = 0; i < commitmentSnapshot.Count; i++)
            {
                var item = commitmentSnapshot[i];
                if (!string.Equals(item.Currency, currency, StringComparison.Ordinal))
                    throw new InvalidOperationException("Project controls cannot aggregate mixed currencies.");
                if (!byId.TryAdd(item.CommitmentId, item))
                    throw new InvalidOperationException("Duplicate commitment id: " + item.CommitmentId + ".");
                committed = Add(committed, item.CommittedAmount, "committed cost");
            }

            decimal actual = 0m;
            var deliveredOrders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var orders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var records = new List<ProjectControlIntegrationRecord>(deliverySnapshot.Count);
            var deliveryIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < deliverySnapshot.Count; i++)
            {
                var delivery = deliverySnapshot[i];
                if (!deliveryIds.Add(delivery.DeliveryId))
                    throw new InvalidOperationException("Duplicate delivery id: " + delivery.DeliveryId + ".");
                if (!byId.TryGetValue(delivery.Commitment.CommitmentId, out var canonical) ||
                    !string.Equals(canonical.Revision.RevisionId, delivery.Commitment.Revision.RevisionId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Delivery references a missing or stale commitment revision.");
                orders.Add(delivery.OrderId);
                if (delivery.Status == DeliveryStatus.Delivered) deliveredOrders.Add(delivery.OrderId);
                actual = Add(actual, delivery.ActualCost, "actual cost");
                records.Add(new ProjectControlIntegrationRecord(projectId, delivery));
            }

            records.Sort((left, right) =>
            {
                var order = StringComparer.Ordinal.Compare(left.OrderId, right.OrderId);
                return order != 0 ? order : StringComparer.Ordinal.Compare(left.DeliveryId, right.DeliveryId);
            });
            var open = committed > actual ? committed - actual : 0m;
            var forecast = committed > actual ? committed : actual;
            return new ConstructionProjectControlSnapshot(
                projectId,
                currency,
                committed,
                actual,
                open,
                forecast,
                orders.Count,
                deliveredOrders.Count,
                new ReadOnlyCollection<ProjectControlIntegrationRecord>(records.ToArray()));
        }

        private static decimal Add(decimal left, decimal right, string label)
        {
            try { return checked(left + right); }
            catch (OverflowException ex) { throw new InvalidOperationException("Overflow while aggregating " + label + ".", ex); }
        }
    }
}
