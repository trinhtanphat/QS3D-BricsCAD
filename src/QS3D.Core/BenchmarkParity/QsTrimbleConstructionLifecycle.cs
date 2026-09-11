using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public enum SupplierLifecycleStatus { Prospective, Approved, Suspended, Closed }
    public enum DeliveryStatus { Planned, Ordered, PartiallyDelivered, Delivered, Cancelled }

    public sealed class ConstructionSupplier
    {
        public ConstructionSupplier(string id, string name, SupplierLifecycleStatus status)
        {
            Id = QsModelElementSnapshot.Require(id, "id");
            Name = QsModelElementSnapshot.Require(name, "name");
            Status = status;
        }
        public string Id { get; private set; }
        public string Name { get; private set; }
        public SupplierLifecycleStatus Status { get; private set; }
    }

    public sealed class SubcontractCommitment
    {
        public SubcontractCommitment(string id, string supplierId, string packageId, decimal originalCommitment, decimal approvedVariation)
        {
            Id = QsModelElementSnapshot.Require(id, "id");
            SupplierId = QsModelElementSnapshot.Require(supplierId, "supplierId");
            PackageId = QsModelElementSnapshot.Require(packageId, "packageId");
            if (originalCommitment < 0m || approvedVariation < 0m) throw new ArgumentOutOfRangeException("commitment");
            OriginalCommitment = originalCommitment;
            ApprovedVariation = approvedVariation;
        }
        public string Id { get; private set; }
        public string SupplierId { get; private set; }
        public string PackageId { get; private set; }
        public decimal OriginalCommitment { get; private set; }
        public decimal ApprovedVariation { get; private set; }
        public decimal CurrentCommitment { get { return OriginalCommitment + ApprovedVariation; } }
    }

    public sealed class PurchaseOrderDelivery
    {
        public PurchaseOrderDelivery(string orderId, string supplierId, string packageId, decimal orderValue, decimal deliveredValue, DeliveryStatus status)
        {
            OrderId = QsModelElementSnapshot.Require(orderId, "orderId");
            SupplierId = QsModelElementSnapshot.Require(supplierId, "supplierId");
            PackageId = QsModelElementSnapshot.Require(packageId, "packageId");
            if (orderValue < 0m || deliveredValue < 0m || deliveredValue > orderValue) throw new ArgumentOutOfRangeException("deliveryValue");
            OrderValue = orderValue;
            DeliveredValue = deliveredValue;
            Status = status;
        }
        public string OrderId { get; private set; }
        public string SupplierId { get; private set; }
        public string PackageId { get; private set; }
        public decimal OrderValue { get; private set; }
        public decimal DeliveredValue { get; private set; }
        public DeliveryStatus Status { get; private set; }
        public decimal RemainingValue { get { return OrderValue - DeliveredValue; } }
    }

    public sealed class FieldProgressRecord
    {
        public FieldProgressRecord(string packageId, double plannedProgress, double actualProgress, decimal actualCost)
        {
            PackageId = QsModelElementSnapshot.Require(packageId, "packageId");
            if (plannedProgress < 0d || plannedProgress > 1d || actualProgress < 0d || actualProgress > 1d) throw new ArgumentOutOfRangeException("progress");
            if (actualCost < 0m) throw new ArgumentOutOfRangeException("actualCost");
            PlannedProgress = plannedProgress;
            ActualProgress = actualProgress;
            ActualCost = actualCost;
        }
        public string PackageId { get; private set; }
        public double PlannedProgress { get; private set; }
        public double ActualProgress { get; private set; }
        public decimal ActualCost { get; private set; }
    }

    public sealed class ProjectControlLine
    {
        public ProjectControlLine(string packageId, decimal commitment, decimal ordered, decimal delivered, decimal actualCost, double plannedProgress, double actualProgress)
        {
            PackageId = packageId;
            Commitment = commitment;
            Ordered = ordered;
            Delivered = delivered;
            ActualCost = actualCost;
            PlannedProgress = plannedProgress;
            ActualProgress = actualProgress;
        }
        public string PackageId { get; private set; }
        public decimal Commitment { get; private set; }
        public decimal Ordered { get; private set; }
        public decimal Delivered { get; private set; }
        public decimal ActualCost { get; private set; }
        public double PlannedProgress { get; private set; }
        public double ActualProgress { get; private set; }
        public decimal CommitmentRemaining { get { return Commitment - ActualCost; } }
        public double ScheduleVariance { get { return ActualProgress - PlannedProgress; } }
    }

    public sealed class TrimbleConstructionLifecycleEngine
    {
        public IReadOnlyList<ProjectControlLine> BuildProjectControls(IEnumerable<ConstructionSupplier> suppliers, IEnumerable<SubcontractCommitment> commitments, IEnumerable<PurchaseOrderDelivery> orders, IEnumerable<FieldProgressRecord> progress)
        {
            if (suppliers == null) throw new ArgumentNullException("suppliers");
            if (commitments == null) throw new ArgumentNullException("commitments");
            if (orders == null) throw new ArgumentNullException("orders");
            if (progress == null) throw new ArgumentNullException("progress");

            var supplierMap = suppliers.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var commitmentList = commitments.ToList();
            foreach (var commitment in commitmentList)
            {
                ConstructionSupplier supplier;
                if (!supplierMap.TryGetValue(commitment.SupplierId, out supplier) || supplier.Status != SupplierLifecycleStatus.Approved)
                    throw new InvalidOperationException("Commitment supplier is not approved: " + commitment.SupplierId + ".");
            }

            var orderList = orders.ToList();
            foreach (var order in orderList)
            {
                ConstructionSupplier supplier;
                if (!supplierMap.TryGetValue(order.SupplierId, out supplier) || supplier.Status != SupplierLifecycleStatus.Approved)
                    throw new InvalidOperationException("Order supplier is not approved: " + order.SupplierId + ".");
            }

            var progressMap = progress.GroupBy(x => x.PackageId, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Last(), StringComparer.OrdinalIgnoreCase);
            var packageIds = commitmentList.Select(x => x.PackageId).Concat(orderList.Select(x => x.PackageId)).Concat(progressMap.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
            var result = new List<ProjectControlLine>();
            foreach (var packageId in packageIds)
            {
                FieldProgressRecord record;
                progressMap.TryGetValue(packageId, out record);
                result.Add(new ProjectControlLine(
                    packageId,
                    commitmentList.Where(x => string.Equals(x.PackageId, packageId, StringComparison.OrdinalIgnoreCase)).Sum(x => x.CurrentCommitment),
                    orderList.Where(x => string.Equals(x.PackageId, packageId, StringComparison.OrdinalIgnoreCase) && x.Status != DeliveryStatus.Cancelled).Sum(x => x.OrderValue),
                    orderList.Where(x => string.Equals(x.PackageId, packageId, StringComparison.OrdinalIgnoreCase) && x.Status != DeliveryStatus.Cancelled).Sum(x => x.DeliveredValue),
                    record == null ? 0m : record.ActualCost,
                    record == null ? 0d : record.PlannedProgress,
                    record == null ? 0d : record.ActualProgress));
            }
            return new ReadOnlyCollection<ProjectControlLine>(result);
        }
    }
}