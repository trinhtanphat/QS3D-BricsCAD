using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Measurement;

namespace QS3D.Core.Scheduling
{
    public enum ScheduleDependencyType
    {
        FinishToStart = 0,
        StartToStart = 1,
        FinishToFinish = 2,
        StartToFinish = 3
    }

    public enum ActivityAllocationBasis
    {
        AbsoluteQuantity = 0
    }

    /// <summary>
    /// Immutable planned activity expressed in project-local calendar time. Project schedule
    /// values deliberately use DateTimeKind.Unspecified plus an explicit schedule timezone and
    /// calendar identity; workstation timezone and UTC conversion are not implicit inputs.
    /// </summary>
    public sealed class ScheduleActivity
    {
        public ScheduleActivity(
            string id,
            string name,
            DateTime plannedStartLocal,
            DateTime plannedFinishLocal,
            string calendarId,
            string calendarVersion,
            string? wbsCode = null,
            string? externalSourceId = null)
        {
            Id = ScheduleContract.RequireToken(id, nameof(id));
            Name = ScheduleContract.RequireText(name, nameof(name));
            PlannedStartLocal = ScheduleContract.RequireProjectLocalDateTime(plannedStartLocal, nameof(plannedStartLocal));
            PlannedFinishLocal = ScheduleContract.RequireProjectLocalDateTime(plannedFinishLocal, nameof(plannedFinishLocal));
            if (PlannedFinishLocal < PlannedStartLocal)
                throw new ArgumentException("Schedule activity finish must not precede start.", nameof(plannedFinishLocal));

            CalendarId = ScheduleContract.RequireToken(calendarId, nameof(calendarId));
            CalendarVersion = ScheduleContract.RequireToken(calendarVersion, nameof(calendarVersion));
            WbsCode = wbsCode == null ? null : ScheduleContract.RequireToken(wbsCode, nameof(wbsCode));
            ExternalSourceId = externalSourceId == null ? null : ScheduleContract.RequireToken(externalSourceId, nameof(externalSourceId));
        }

        public string Id { get; }
        public string Name { get; }
        public DateTime PlannedStartLocal { get; }
        public DateTime PlannedFinishLocal { get; }
        public string CalendarId { get; }
        public string CalendarVersion { get; }
        public string? WbsCode { get; }
        public string? ExternalSourceId { get; }

        public string ToCanonicalString()
        {
            var builder = new StringBuilder();
            ScheduleContract.AppendToken(builder, "ACT2");
            ScheduleContract.AppendToken(builder, Id);
            ScheduleContract.AppendToken(builder, Name);
            ScheduleContract.AppendProjectLocalDateTime(builder, PlannedStartLocal);
            ScheduleContract.AppendProjectLocalDateTime(builder, PlannedFinishLocal);
            ScheduleContract.AppendToken(builder, CalendarId);
            ScheduleContract.AppendToken(builder, CalendarVersion);
            ScheduleContract.AppendNullableToken(builder, WbsCode);
            ScheduleContract.AppendNullableToken(builder, ExternalSourceId);
            return builder.ToString();
        }
    }

    /// <summary>
    /// Logical relationship between two activities. Dependencies are schedule-side facts and
    /// never mutate model geometry, measurement traces, rates or estimate history.
    /// </summary>
    public sealed class ScheduleDependency
    {
        public ScheduleDependency(
            string predecessorActivityId,
            string successorActivityId,
            ScheduleDependencyType type = ScheduleDependencyType.FinishToStart,
            TimeSpan? lag = null)
        {
            PredecessorActivityId = ScheduleContract.RequireToken(predecessorActivityId, nameof(predecessorActivityId));
            SuccessorActivityId = ScheduleContract.RequireToken(successorActivityId, nameof(successorActivityId));
            if (string.Equals(PredecessorActivityId, SuccessorActivityId, StringComparison.Ordinal))
                throw new ArgumentException("Schedule dependency cannot reference the same predecessor and successor.");
            if (!Enum.IsDefined(typeof(ScheduleDependencyType), type))
                throw new ArgumentOutOfRangeException(nameof(type));

            Type = type;
            Lag = lag ?? TimeSpan.Zero;
        }

        public string PredecessorActivityId { get; }
        public string SuccessorActivityId { get; }
        public ScheduleDependencyType Type { get; }
        public TimeSpan Lag { get; }

        public string ToCanonicalString()
        {
            var builder = new StringBuilder();
            ScheduleContract.AppendToken(builder, "DEP1");
            ScheduleContract.AppendToken(builder, PredecessorActivityId);
            ScheduleContract.AppendToken(builder, SuccessorActivityId);
            ScheduleContract.AppendNumber(builder, (int)Type);
            ScheduleContract.AppendLong(builder, Lag.Ticks);
            return builder.ToString();
        }
    }

    /// <summary>
    /// Immutable absolute-quantity allocation from a frozen MeasurementTrace to a planned
    /// activity. MeasurementSnapshotId and trace fingerprint preserve upstream provenance.
    /// </summary>
    public sealed class ScheduleQuantityLink
    {
        public ScheduleQuantityLink(
            string allocationId,
            string activityId,
            string measurementSnapshotId,
            MeasurementTrace measurement,
            double allocatedValue)
        {
            if (measurement == null) throw new ArgumentNullException(nameof(measurement));

            AllocationId = ScheduleContract.RequireToken(allocationId, nameof(allocationId));
            ActivityId = ScheduleContract.RequireToken(activityId, nameof(activityId));
            MeasurementSnapshotId = ScheduleContract.RequireToken(measurementSnapshotId, nameof(measurementSnapshotId));
            Basis = ActivityAllocationBasis.AbsoluteQuantity;
            SemanticIdentity = measurement.SemanticIdentity;
            SourceIdentity = measurement.SourceIdentity;
            QuantityKey = measurement.QuantityKey;
            MeasuredValue = measurement.NetValue;
            Unit = measurement.Unit;
            MeasurementFingerprint = ScheduleContract.Sha256(measurement.ToCanonicalString());
            AllocatedValue = ScheduleContract.RequirePositiveFinite(allocatedValue, nameof(allocatedValue));

            var tolerance = ScheduleContract.NumericTolerance(MeasuredValue);
            if (AllocatedValue - MeasuredValue > tolerance)
                throw new ArgumentOutOfRangeException(nameof(allocatedValue), "Schedule allocation cannot exceed the frozen measured quantity.");
        }

        public string AllocationId { get; }
        public string ActivityId { get; }
        public string MeasurementSnapshotId { get; }
        public ActivityAllocationBasis Basis { get; }
        public string SemanticIdentity { get; }
        public string SourceIdentity { get; }
        public string QuantityKey { get; }
        public double MeasuredValue { get; }
        public double AllocatedValue { get; }
        public string Unit { get; }
        public string MeasurementFingerprint { get; }

        public string MeasurementIdentity => SemanticIdentity + "\u001f" + SourceIdentity + "\u001f" + QuantityKey;

        public string ToCanonicalString()
        {
            var builder = new StringBuilder();
            ScheduleContract.AppendToken(builder, "QLN2");
            ScheduleContract.AppendToken(builder, AllocationId);
            ScheduleContract.AppendToken(builder, ActivityId);
            ScheduleContract.AppendToken(builder, MeasurementSnapshotId);
            ScheduleContract.AppendNumber(builder, (int)Basis);
            ScheduleContract.AppendToken(builder, SemanticIdentity);
            ScheduleContract.AppendToken(builder, SourceIdentity);
            ScheduleContract.AppendToken(builder, QuantityKey);
            ScheduleContract.AppendNumber(builder, MeasuredValue);
            ScheduleContract.AppendNumber(builder, AllocatedValue);
            ScheduleContract.AppendToken(builder, Unit);
            ScheduleContract.AppendToken(builder, MeasurementFingerprint);
            return builder.ToString();
        }
    }

    /// <summary>
    /// Deterministic immutable schedule/allocation version. Project calendar semantics are
    /// explicit, dependency cycles fail closed, and aggregate allocations cannot exceed the
    /// referenced frozen measurement quantity.
    /// </summary>
    public sealed class ScheduleSnapshot
    {
        private const int MaximumEntries = 10000;

        public ScheduleSnapshot(
            string scheduleId,
            string scheduleVersionId,
            string allocationVersionId,
            string projectTimeZoneId,
            DateTime dataDate,
            IEnumerable<ScheduleActivity> activities,
            IEnumerable<ScheduleDependency>? dependencies = null,
            IEnumerable<ScheduleQuantityLink>? quantityLinks = null)
        {
            ScheduleId = ScheduleContract.RequireToken(scheduleId, nameof(scheduleId));
            ScheduleVersionId = ScheduleContract.RequireToken(scheduleVersionId, nameof(scheduleVersionId));
            AllocationVersionId = ScheduleContract.RequireToken(allocationVersionId, nameof(allocationVersionId));
            ProjectTimeZoneId = ScheduleContract.RequireText(projectTimeZoneId, nameof(projectTimeZoneId));
            DataDate = ScheduleContract.RequireProjectDate(dataDate, nameof(dataDate));

            Activities = SnapshotActivities(activities, nameof(activities));
            var activityIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Activities.Count; i++)
                activityIds.Add(Activities[i].Id);

            Dependencies = SnapshotDependencies(dependencies, activityIds, nameof(dependencies));
            QuantityLinks = SnapshotQuantityLinks(quantityLinks, activityIds, nameof(quantityLinks));

            ValidateAcyclic(Activities, Dependencies);
            ValidateAllocations(QuantityLinks);
        }

        public string ScheduleId { get; }
        public string ScheduleVersionId { get; }
        public string AllocationVersionId { get; }
        public string ProjectTimeZoneId { get; }
        public DateTime DataDate { get; }
        public IReadOnlyList<ScheduleActivity> Activities { get; }
        public IReadOnlyList<ScheduleDependency> Dependencies { get; }
        public IReadOnlyList<ScheduleQuantityLink> QuantityLinks { get; }

        public double GetAllocatedValue(string semanticIdentity, string sourceIdentity, string quantityKey)
        {
            var identity = ScheduleContract.MeasurementIdentity(semanticIdentity, sourceIdentity, quantityKey);
            var sum = 0d;
            var compensation = 0d;
            for (var i = 0; i < QuantityLinks.Count; i++)
            {
                if (!string.Equals(QuantityLinks[i].MeasurementIdentity, identity, StringComparison.Ordinal))
                    continue;
                ScheduleContract.AddCompensated(QuantityLinks[i].AllocatedValue, ref sum, ref compensation);
            }

            var total = ScheduleContract.CompensatedTotal(sum, compensation);
            return total == 0d ? 0d : total;
        }

        public string ToCanonicalString()
        {
            var builder = new StringBuilder();
            ScheduleContract.AppendToken(builder, "SCH2");
            ScheduleContract.AppendToken(builder, ScheduleId);
            ScheduleContract.AppendToken(builder, ScheduleVersionId);
            ScheduleContract.AppendToken(builder, AllocationVersionId);
            ScheduleContract.AppendToken(builder, ProjectTimeZoneId);
            ScheduleContract.AppendProjectDate(builder, DataDate);
            ScheduleContract.AppendNumber(builder, Activities.Count);
            for (var i = 0; i < Activities.Count; i++)
                ScheduleContract.AppendToken(builder, Activities[i].ToCanonicalString());
            ScheduleContract.AppendNumber(builder, Dependencies.Count);
            for (var i = 0; i < Dependencies.Count; i++)
                ScheduleContract.AppendToken(builder, Dependencies[i].ToCanonicalString());
            ScheduleContract.AppendNumber(builder, QuantityLinks.Count);
            for (var i = 0; i < QuantityLinks.Count; i++)
                ScheduleContract.AppendToken(builder, QuantityLinks[i].ToCanonicalString());
            return builder.ToString();
        }

        private static IReadOnlyList<ScheduleActivity> SnapshotActivities(IEnumerable<ScheduleActivity> source, string parameterName)
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            var items = Snapshot(source, parameterName, "activities");
            items.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            for (var i = 1; i < items.Count; i++)
            {
                if (string.Equals(items[i - 1].Id, items[i].Id, StringComparison.Ordinal))
                    throw new ArgumentException("Schedule activity IDs must be unique.", parameterName);
            }
            return new ReadOnlyCollection<ScheduleActivity>(items.ToArray());
        }

        private static IReadOnlyList<ScheduleDependency> SnapshotDependencies(
            IEnumerable<ScheduleDependency>? source,
            HashSet<string> activityIds,
            string parameterName)
        {
            if (source == null)
                return new ReadOnlyCollection<ScheduleDependency>(Array.Empty<ScheduleDependency>());

            var items = Snapshot(source, parameterName, "dependencies");
            for (var i = 0; i < items.Count; i++)
            {
                var dependency = items[i];
                if (!activityIds.Contains(dependency.PredecessorActivityId) || !activityIds.Contains(dependency.SuccessorActivityId))
                    throw new ArgumentException("Schedule dependency references an unknown activity.", parameterName);
            }

            items.Sort(CompareDependencies);
            for (var i = 1; i < items.Count; i++)
            {
                if (CompareDependencies(items[i - 1], items[i]) == 0)
                    throw new ArgumentException("Schedule dependencies must not contain duplicates.", parameterName);
            }
            return new ReadOnlyCollection<ScheduleDependency>(items.ToArray());
        }

        private static IReadOnlyList<ScheduleQuantityLink> SnapshotQuantityLinks(
            IEnumerable<ScheduleQuantityLink>? source,
            HashSet<string> activityIds,
            string parameterName)
        {
            if (source == null)
                return new ReadOnlyCollection<ScheduleQuantityLink>(Array.Empty<ScheduleQuantityLink>());

            var items = Snapshot(source, parameterName, "quantity links");
            var allocationIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < items.Count; i++)
            {
                if (!activityIds.Contains(items[i].ActivityId))
                    throw new ArgumentException("Schedule quantity link references an unknown activity.", parameterName);
                if (!allocationIds.Add(items[i].AllocationId))
                    throw new ArgumentException("Schedule allocation IDs must be unique.", parameterName);
            }

            items.Sort(CompareQuantityLinks);
            for (var i = 1; i < items.Count; i++)
            {
                var previous = items[i - 1];
                var current = items[i];
                if (string.Equals(previous.ActivityId, current.ActivityId, StringComparison.Ordinal) &&
                    string.Equals(previous.MeasurementIdentity, current.MeasurementIdentity, StringComparison.Ordinal))
                {
                    throw new ArgumentException("An activity may allocate a frozen measurement identity only once per allocation version.", parameterName);
                }
            }
            return new ReadOnlyCollection<ScheduleQuantityLink>(items.ToArray());
        }

        private static List<T> Snapshot<T>(IEnumerable<T> source, string parameterName, string collectionName) where T : class
        {
            var knownCount = ReadKnownCount(source, parameterName, collectionName);
            var items = new List<T>();

            using (var enumerator = source.GetEnumerator())
            {
                var acquiredCount = ReadKnownCount(source, parameterName, collectionName);
                ValidateReboundCount(knownCount, acquiredCount, null, parameterName, collectionName);

                while (enumerator.MoveNext())
                {
                    if (knownCount.HasValue && items.Count >= knownCount.Value)
                        throw CountChangedError(parameterName, collectionName);
                    if (items.Count >= MaximumEntries)
                        throw CollectionCountError(parameterName, collectionName);
                    var item = enumerator.Current;
                    if (item == null)
                        throw new ArgumentException("Schedule " + collectionName + " cannot contain null entries.", parameterName);
                    items.Add(item);
                }
            }

            if (knownCount.HasValue && knownCount.Value != items.Count)
                throw CountChangedError(parameterName, collectionName);

            var reboundCount = ReadKnownCount(source, parameterName, collectionName);
            ValidateReboundCount(knownCount, reboundCount, items.Count, parameterName, collectionName);
            return items;
        }

        private static int? ReadKnownCount<T>(IEnumerable<T> source, string parameterName, string collectionName)
        {
            int? knownCount = null;
            if (source is ICollection<T> collection)
                ValidateKnownCount(collection.Count, ref knownCount, parameterName, collectionName);
            if (source is IReadOnlyCollection<T> readOnlyCollection)
                ValidateKnownCount(readOnlyCollection.Count, ref knownCount, parameterName, collectionName);
            if (source is System.Collections.ICollection nonGenericCollection)
                ValidateKnownCount(nonGenericCollection.Count, ref knownCount, parameterName, collectionName);
            return knownCount;
        }

        private static void ValidateReboundCount(
            int? knownCount,
            int? reboundCount,
            int? materializedCount,
            string parameterName,
            string collectionName)
        {
            if (reboundCount.HasValue != knownCount.HasValue ||
                (reboundCount.HasValue && reboundCount.Value != knownCount!.Value) ||
                (materializedCount.HasValue && reboundCount.HasValue && reboundCount.Value != materializedCount.Value))
            {
                throw CountChangedError(parameterName, collectionName);
            }
        }

        private static void ValidateKnownCount(int count, ref int? knownCount, string parameterName, string collectionName)
        {
            if (count < 0)
                throw new ArgumentException("Schedule " + collectionName + " count cannot be negative.", parameterName);
            if (count > MaximumEntries)
                throw CollectionCountError(parameterName, collectionName);
            if (knownCount.HasValue && knownCount.Value != count)
                throw new ArgumentException("Schedule " + collectionName + " count contracts disagree.", parameterName);
            knownCount = count;
        }

        private static ArgumentException CountChangedError(string parameterName, string collectionName)
        {
            return new ArgumentException("Schedule " + collectionName + " count changed during enumeration.", parameterName);
        }

        private static ArgumentException CollectionCountError(string parameterName, string collectionName)
        {
            return new ArgumentException("Schedule accepts at most " + MaximumEntries + " " + collectionName + ".", parameterName);
        }

        private static int CompareDependencies(ScheduleDependency left, ScheduleDependency right)
        {
            var compare = StringComparer.Ordinal.Compare(left.PredecessorActivityId, right.PredecessorActivityId);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.SuccessorActivityId, right.SuccessorActivityId);
            if (compare != 0) return compare;
            compare = ((int)left.Type).CompareTo((int)right.Type);
            if (compare != 0) return compare;
            return left.Lag.Ticks.CompareTo(right.Lag.Ticks);
        }

        private static int CompareQuantityLinks(ScheduleQuantityLink left, ScheduleQuantityLink right)
        {
            var compare = StringComparer.Ordinal.Compare(left.ActivityId, right.ActivityId);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.SemanticIdentity, right.SemanticIdentity);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.SourceIdentity, right.SourceIdentity);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.QuantityKey, right.QuantityKey);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.MeasurementSnapshotId, right.MeasurementSnapshotId);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.MeasurementFingerprint, right.MeasurementFingerprint);
            if (compare != 0) return compare;
            return StringComparer.Ordinal.Compare(left.AllocationId, right.AllocationId);
        }

        private static void ValidateAcyclic(IReadOnlyList<ScheduleActivity> activities, IReadOnlyList<ScheduleDependency> dependencies)
        {
            var indegree = new Dictionary<string, int>(StringComparer.Ordinal);
            var outgoing = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (var i = 0; i < activities.Count; i++)
            {
                indegree.Add(activities[i].Id, 0);
                outgoing.Add(activities[i].Id, new List<string>());
            }

            for (var i = 0; i < dependencies.Count; i++)
            {
                var dependency = dependencies[i];
                outgoing[dependency.PredecessorActivityId].Add(dependency.SuccessorActivityId);
                indegree[dependency.SuccessorActivityId] = checked(indegree[dependency.SuccessorActivityId] + 1);
            }

            var ready = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var pair in indegree)
            {
                if (pair.Value == 0) ready.Add(pair.Key);
            }

            var visited = 0;
            while (ready.Count > 0)
            {
                var current = ready.Min;
                ready.Remove(current);
                visited++;
                var successors = outgoing[current];
                for (var i = 0; i < successors.Count; i++)
                {
                    var successor = successors[i];
                    var next = indegree[successor] - 1;
                    indegree[successor] = next;
                    if (next == 0) ready.Add(successor);
                }
            }

            if (visited != activities.Count)
                throw new ArgumentException("Schedule dependencies contain a cycle.", nameof(dependencies));
        }

        private static void ValidateAllocations(IReadOnlyList<ScheduleQuantityLink> links)
        {
            var states = new Dictionary<string, AllocationState>(StringComparer.Ordinal);
            for (var i = 0; i < links.Count; i++)
            {
                var link = links[i];
                AllocationState state;
                if (!states.TryGetValue(link.MeasurementIdentity, out state))
                {
                    state = new AllocationState(
                        link.MeasurementSnapshotId,
                        link.MeasurementFingerprint,
                        link.MeasuredValue,
                        link.Unit);
                    states.Add(link.MeasurementIdentity, state);
                }
                else if (!string.Equals(state.MeasurementSnapshotId, link.MeasurementSnapshotId, StringComparison.Ordinal) ||
                         !string.Equals(state.Fingerprint, link.MeasurementFingerprint, StringComparison.Ordinal) ||
                         !state.MeasuredValue.Equals(link.MeasuredValue) ||
                         !string.Equals(state.Unit, link.Unit, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Schedule quantity links contain conflicting frozen provenance for the same measurement identity.", nameof(links));
                }

                ScheduleContract.AddCompensated(link.AllocatedValue, ref state.Sum, ref state.Compensation);
                var allocated = ScheduleContract.CompensatedTotal(state.Sum, state.Compensation);
                if (allocated - state.MeasuredValue > ScheduleContract.NumericTolerance(state.MeasuredValue))
                    throw new ArgumentException("Schedule quantity allocation exceeds the frozen measured quantity.", nameof(links));
            }
        }

        private sealed class AllocationState
        {
            public AllocationState(string measurementSnapshotId, string fingerprint, double measuredValue, string unit)
            {
                MeasurementSnapshotId = measurementSnapshotId;
                Fingerprint = fingerprint;
                MeasuredValue = measuredValue;
                Unit = unit;
            }

            public string MeasurementSnapshotId { get; }
            public string Fingerprint { get; }
            public double MeasuredValue { get; }
            public string Unit { get; }
            public double Sum;
            public double Compensation;
        }
    }

    internal static class ScheduleContract
    {
        internal static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Schedule text is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Schedule text must be canonical without surrounding whitespace.", parameterName);
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (char.IsControl(character))
                    throw new ArgumentException("Schedule text cannot contain control characters.", parameterName);
                if (char.IsHighSurrogate(character))
                {
                    if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                        throw new ArgumentException("Schedule text must contain well-formed UTF-16.", parameterName);
                    i++;
                }
                else if (char.IsLowSurrogate(character))
                {
                    throw new ArgumentException("Schedule text must contain well-formed UTF-16.", parameterName);
                }
            }
            return value;
        }

        internal static string RequireToken(string value, string parameterName)
        {
            value = RequireText(value, parameterName);
            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                    throw new ArgumentException("Schedule token cannot contain whitespace.", parameterName);
            }
            return value;
        }

        internal static DateTime RequireProjectLocalDateTime(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Unspecified)
                throw new ArgumentException("Project schedule date/time must use DateTimeKind.Unspecified with explicit project timezone/calendar metadata.", parameterName);
            return value;
        }

        internal static DateTime RequireProjectDate(DateTime value, string parameterName)
        {
            value = RequireProjectLocalDateTime(value, parameterName);
            if (value.TimeOfDay != TimeSpan.Zero)
                throw new ArgumentException("Project data date must be a local calendar date at 00:00:00.", parameterName);
            return value;
        }

        internal static double RequirePositiveFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(parameterName, "Schedule allocation must be finite and greater than zero.");
            return value;
        }

        internal static double NumericTolerance(double value)
        {
            return Math.Abs(value) * 1e-12;
        }

        internal static string MeasurementIdentity(string semanticIdentity, string sourceIdentity, string quantityKey)
        {
            return RequireToken(semanticIdentity, nameof(semanticIdentity)) + "\u001f" +
                   RequireToken(sourceIdentity, nameof(sourceIdentity)) + "\u001f" +
                   RequireToken(quantityKey, nameof(quantityKey));
        }

        internal static void AddCompensated(double value, ref double sum, ref double compensation)
        {
            RequireFiniteAggregationState(value, sum, compensation);

            var next = sum + value;
            if (!IsFinite(next))
                throw NonFiniteAggregation();

            var correction = Math.Abs(sum) >= Math.Abs(value)
                ? (sum - next) + value
                : (value - next) + sum;
            if (!IsFinite(correction))
                throw NonFiniteAggregation();

            var nextCompensation = compensation + correction;
            if (!IsFinite(nextCompensation))
                throw NonFiniteAggregation();

            sum = next;
            compensation = nextCompensation;
            CompensatedTotal(sum, compensation);
        }

        internal static double CompensatedTotal(double sum, double compensation)
        {
            if (!IsFinite(sum) || !IsFinite(compensation))
                throw NonFiniteAggregation();
            var total = sum + compensation;
            if (!IsFinite(total))
                throw NonFiniteAggregation();
            return total;
        }

        private static void RequireFiniteAggregationState(double value, double sum, double compensation)
        {
            if (!IsFinite(value) || !IsFinite(sum) || !IsFinite(compensation))
                throw NonFiniteAggregation();
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static ArgumentException NonFiniteAggregation()
        {
            return new ArgumentException("Schedule compensated numeric aggregation must remain finite.");
        }

        internal static string Sha256(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(bytes.Length * 2);
                for (var i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        internal static void AppendToken(StringBuilder builder, string value)
        {
            builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(value);
        }

        internal static void AppendNullableToken(StringBuilder builder, string? value)
        {
            if (value == null)
            {
                AppendToken(builder, "-");
                return;
            }
            AppendToken(builder, "+" + value);
        }

        internal static void AppendProjectLocalDateTime(StringBuilder builder, DateTime value)
        {
            AppendToken(builder, value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
        }

        internal static void AppendProjectDate(StringBuilder builder, DateTime value)
        {
            AppendToken(builder, value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        internal static void AppendNumber(StringBuilder builder, double value)
        {
            AppendToken(builder, value.ToString("R", CultureInfo.InvariantCulture));
        }

        internal static void AppendNumber(StringBuilder builder, int value)
        {
            AppendToken(builder, value.ToString(CultureInfo.InvariantCulture));
        }

        internal static void AppendLong(StringBuilder builder, long value)
        {
            AppendToken(builder, value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
