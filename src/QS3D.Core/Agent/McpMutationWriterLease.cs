using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace QS3D.Core.Agent
{
    public sealed class McpMutationWriteBusyException : InvalidOperationException
    {
        public McpMutationWriteBusyException(string activeOwner, string activeOperation)
            : base(BuildMessage(activeOwner, activeOperation))
        {
            ActiveOwner = activeOwner ?? string.Empty;
            ActiveOperation = activeOperation ?? string.Empty;
        }

        public string ActiveOwner { get; }
        public string ActiveOperation { get; }

        private static string BuildMessage(string owner, string operation)
        {
            var safeOwner = string.IsNullOrWhiteSpace(owner) ? "unknown" : owner;
            var safeOperation = string.IsNullOrWhiteSpace(operation) ? "unknown" : operation;
            return "Another MCP mutation writer is active (owner=" + safeOwner + ", operation=" + safeOperation + "). Wait for it to finish before retrying.";
        }
    }

    public sealed class McpMutationWriterLeaseSnapshot
    {
        internal McpMutationWriterLeaseSnapshot(bool held, string owner, string operation, int waiterCount)
        {
            Held = held;
            Owner = owner ?? string.Empty;
            Operation = operation ?? string.Empty;
            WaiterCount = waiterCount;
        }

        public bool Held { get; }
        public string Owner { get; }
        public string Operation { get; }
        public int WaiterCount { get; }
    }

    public static class McpMutationWriterLease
    {
        private static readonly object Sync = new object();
        private static readonly LinkedList<Waiter> Waiters = new LinkedList<Waiter>();
        private static bool _held;
        private static long _leaseId;
        private static string _owner = string.Empty;
        private static string _operation = string.Empty;

        public static IDisposable Acquire(string owner, string operation, int timeoutMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Mutation writer owner is required.", nameof(owner));
            if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Mutation writer operation is required.", nameof(operation));
            if (timeoutMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));

            var waiter = new Waiter(owner.Trim(), operation.Trim());
            var clock = Stopwatch.StartNew();
            lock (Sync)
            {
                waiter.Node = Waiters.AddLast(waiter);
                while (true)
                {
                    if (!_held && ReferenceEquals(Waiters.First, waiter.Node))
                    {
                        Waiters.RemoveFirst();
                        waiter.Node = null;
                        _held = true;
                        _owner = waiter.Owner;
                        _operation = waiter.Operation;
                        _leaseId++;
                        return new Releaser(_leaseId);
                    }

                    var remaining = timeoutMilliseconds - (int)Math.Min(int.MaxValue, clock.ElapsedMilliseconds);
                    if (remaining <= 0)
                    {
                        RemoveWaiter(waiter);
                        var activeOwner = _owner;
                        var activeOperation = _operation;
                        if (!_held && Waiters.First != null)
                        {
                            activeOwner = Waiters.First.Value.Owner;
                            activeOperation = Waiters.First.Value.Operation;
                        }
                        Monitor.PulseAll(Sync);
                        throw new McpMutationWriteBusyException(activeOwner, activeOperation);
                    }

                    Monitor.Wait(Sync, Math.Min(remaining, 100));
                }
            }
        }

        public static McpMutationWriterLeaseSnapshot Snapshot()
        {
            lock (Sync)
            {
                return new McpMutationWriterLeaseSnapshot(_held, _owner, _operation, Waiters.Count);
            }
        }

        private static void RemoveWaiter(Waiter waiter)
        {
            if (waiter.Node == null) return;
            Waiters.Remove(waiter.Node);
            waiter.Node = null;
        }

        private static void Release(long leaseId)
        {
            lock (Sync)
            {
                if (!_held || leaseId != _leaseId) return;
                _held = false;
                _owner = string.Empty;
                _operation = string.Empty;
                Monitor.PulseAll(Sync);
            }
        }

        private sealed class Waiter
        {
            internal Waiter(string owner, string operation)
            {
                Owner = owner;
                Operation = operation;
            }

            internal string Owner { get; }
            internal string Operation { get; }
            internal LinkedListNode<Waiter>? Node { get; set; }
        }

        private sealed class Releaser : IDisposable
        {
            private readonly long _leaseId;
            private int _disposed;

            internal Releaser(long leaseId)
            {
                _leaseId = leaseId;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
                Release(_leaseId);
            }
        }
    }
}
