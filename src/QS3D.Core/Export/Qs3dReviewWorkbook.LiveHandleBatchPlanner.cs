using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Export
{
    /// <summary>
    /// Partitions an already validated full-project Handle scope without imposing a
    /// project-wide limit. Each host lookup remains below CadHandleService's bounded
    /// per-call input contract.
    /// </summary>
    public static class Qs3dReviewLiveHandleBatchPlanner
    {
        public const int MaximumBatchSize = 10000;

        public static IReadOnlyList<IReadOnlyList<string>> Create(
            IEnumerable<string> handles,
            int batchSize)
        {
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            if (batchSize <= 0 || batchSize > MaximumBatchSize)
                throw new ArgumentOutOfRangeException(nameof(batchSize));

            var result = new List<IReadOnlyList<string>>();
            var current = new List<string>(batchSize);
            foreach (var handle in handles)
            {
                if (handle == null) throw new ArgumentException("Handle batch input contains null.", nameof(handles));
                current.Add(handle);
                if (current.Count != batchSize) continue;
                result.Add(new ReadOnlyCollection<string>(current.ToArray()));
                current.Clear();
            }
            if (current.Count > 0)
                result.Add(new ReadOnlyCollection<string>(current.ToArray()));
            return new ReadOnlyCollection<IReadOnlyList<string>>(result);
        }
    }
}
