using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Commercial
{
    public enum CommitmentStatus { Draft, Approved, Active, Closed, Cancelled }
    public enum VendorStatus { Prospective, Qualified, Active, Suspended, Archived }

    public sealed class VendorRecord
    {
        public VendorRecord(string id, string name, VendorStatus status) { Id = Require(id, nameof(id)); Name = Require(name, nameof(name)); Status = status; }
        public string Id { get; }
        public string Name { get; }
        public VendorStatus Status { get; }
        private static string Require(string value, string name) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name); return value.Trim(); }
    }

    public sealed class SubcontractCommitment
    {
        public SubcontractCommitment(string id, string vendorId, string wbsCode, decimal originalAmount, decimal approvedChanges, CommitmentStatus status)
        {
            if (originalAmount < 0m || approvedChanges < 0m) throw new ArgumentOutOfRangeException(nameof(originalAmount));
            Id = Require(id, nameof(id)); VendorId = Require(vendorId, nameof(vendorId)); WbsCode = Require(wbsCode, nameof(wbsCode)); OriginalAmount = originalAmount; ApprovedChanges = approvedChanges; Status = status;
        }
        public string Id { get; }
        public string VendorId { get; }
        public string WbsCode { get; }
        public decimal OriginalAmount { get; }
        public decimal ApprovedChanges { get; }
        public decimal CurrentCommitment => checked(OriginalAmount + ApprovedChanges);
        public CommitmentStatus Status { get; }
        private static string Require(string value, string name) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name); return value.Trim(); }
    }

    public sealed class PurchaseOrderDelivery
    {
        public PurchaseOrderDelivery(string purchaseOrderId, string commitmentId, decimal orderedQuantity, decimal deliveredQuantity, decimal unitCost)
        {
            if (orderedQuantity <= 0m) throw new ArgumentOutOfRangeException(nameof(orderedQuantity));
            if (deliveredQuantity < 0m || deliveredQuantity > orderedQuantity) throw new ArgumentOutOfRangeException(nameof(deliveredQuantity));
            if (unitCost < 0m) throw new ArgumentOutOfRangeException(nameof(unitCost));
            PurchaseOrderId = Require(purchaseOrderId, nameof(purchaseOrderId)); CommitmentId = Require(commitmentId, nameof(commitmentId)); OrderedQuantity = orderedQuantity; DeliveredQuantity = deliveredQuantity; UnitCost = unitCost;
        }
        public string PurchaseOrderId { get; }
        public string CommitmentId { get; }
        public decimal OrderedQuantity { get; }
        public decimal DeliveredQuantity { get; }
        public decimal UnitCost { get; }
        public decimal OrderedValue => checked(OrderedQuantity * UnitCost);
        public decimal DeliveredValue => checked(DeliveredQuantity * UnitCost);
        private static string Require(string value, string name) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name); return value.Trim(); }
    }

    public sealed class FieldProgressRecord
    {
        public FieldProgressRecord(string wbsCode, decimal plannedPercent, decimal actualPercent, decimal earnedValue)
        {
            if (string.IsNullOrWhiteSpace(wbsCode)) throw new ArgumentException("WBS code is required.", nameof(wbsCode));
            if (plannedPercent < 0m || plannedPercent > 100m) throw new ArgumentOutOfRangeException(nameof(plannedPercent));
            if (actualPercent < 0m || actualPercent > 100m) throw new ArgumentOutOfRangeException(nameof(actualPercent));
            if (earnedValue < 0m) throw new ArgumentOutOfRangeException(nameof(earnedValue));
            WbsCode = wbsCode.Trim(); PlannedPercent = plannedPercent; ActualPercent = actualPercent; EarnedValue = earnedValue;
        }
        public string WbsCode { get; }
        public decimal PlannedPercent { get; }
        public decimal ActualPercent { get; }
        public decimal EarnedValue { get; }
        public decimal ScheduleVariancePercent => ActualPercent - PlannedPercent;
    }

    public sealed class ProjectControlSnapshot
    {
        internal ProjectControlSnapshot(decimal commitment, decimal actual, decimal delivered, decimal procurementProgress, decimal fieldProgress, decimal earnedValue) { Commitment = commitment; ActualCost = actual; DeliveredValue = delivered; ProcurementProgress = procurementProgress; FieldProgress = fieldProgress; EarnedValue = earnedValue; }
        public decimal Commitment { get; }
        public decimal ActualCost { get; }
        public decimal ActualVsCommitmentVariance => Commitment - ActualCost;
        public decimal DeliveredValue { get; }
        public decimal ProcurementProgress { get; }
        public decimal FieldProgress { get; }
        public decimal EarnedValue { get; }
    }

    public sealed class ErpProjectControlEnvelope
    {
        public ErpProjectControlEnvelope(string projectId, string revision, string idempotencyKey, ProjectControlSnapshot snapshot)
        {
            ProjectId = Require(projectId, nameof(projectId)); Revision = Require(revision, nameof(revision)); IdempotencyKey = Require(idempotencyKey, nameof(idempotencyKey)); Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }
        public string ProjectId { get; }
        public string Revision { get; }
        public string IdempotencyKey { get; }
        public ProjectControlSnapshot Snapshot { get; }
        private static string Require(string value, string name) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", name); return value.Trim(); }
    }

    public sealed class ConstructionLifecycleService
    {
        private readonly HashSet<string> _exportKeys = new HashSet<string>(StringComparer.Ordinal);

        public ProjectControlSnapshot BuildSnapshot(IEnumerable<VendorRecord> vendors, IEnumerable<SubcontractCommitment> commitments, IEnumerable<PurchaseOrderDelivery> deliveries, IEnumerable<decimal> actualCosts, IEnumerable<FieldProgressRecord> progress)
        {
            var vendorList = Snapshot(vendors, nameof(vendors)); var commitmentList = Snapshot(commitments, nameof(commitments)); var deliveryList = Snapshot(deliveries, nameof(deliveries)); var actualList = Snapshot(actualCosts, nameof(actualCosts)); var progressList = Snapshot(progress, nameof(progress));
            var vendorIds = new HashSet<string>(vendorList.Where(v => v.Status == VendorStatus.Qualified || v.Status == VendorStatus.Active).Select(v => v.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var commitment in commitmentList) if (!vendorIds.Contains(commitment.VendorId)) throw new InvalidOperationException("Commitment vendor must be qualified or active.");
            var commitmentIds = new HashSet<string>(commitmentList.Select(c => c.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var delivery in deliveryList) if (!commitmentIds.Contains(delivery.CommitmentId)) throw new InvalidOperationException("Delivery must reference an admitted commitment.");
            if (actualList.Any(x => x < 0m)) throw new InvalidOperationException("Actual costs cannot be negative.");
            decimal committed = commitmentList.Where(c => c.Status == CommitmentStatus.Approved || c.Status == CommitmentStatus.Active || c.Status == CommitmentStatus.Closed).Sum(c => c.CurrentCommitment);
            decimal actual = actualList.Sum(); decimal ordered = deliveryList.Sum(x => x.OrderedValue); decimal delivered = deliveryList.Sum(x => x.DeliveredValue);
            decimal procurement = ordered == 0m ? 0m : delivered / ordered; decimal field = progressList.Count == 0 ? 0m : progressList.Average(x => x.ActualPercent) / 100m; decimal earned = progressList.Sum(x => x.EarnedValue);
            return new ProjectControlSnapshot(committed, actual, delivered, procurement, field, earned);
        }

        public ErpProjectControlEnvelope Export(string projectId, string revision, string idempotencyKey, ProjectControlSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
            var key = idempotencyKey.Trim(); if (!_exportKeys.Add(key)) throw new InvalidOperationException("Duplicate ERP/project-control export rejected.");
            return new ErpProjectControlEnvelope(projectId, revision, key, snapshot);
        }

        private static List<T> Snapshot<T>(IEnumerable<T> source, string name) { if (source == null) throw new ArgumentNullException(name); return source.ToList(); }
    }
}
