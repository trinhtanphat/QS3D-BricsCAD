using System;
using System.Threading;
using QS3D.Core.Agent;

namespace QS3D.Core.SmokeTests
{
    internal static class McpMutationWriterLeaseSmoke
    {
        internal static void Run()
        {
            SerializesConcurrentWriters();
            ReportsBusyOwnerOnTimeout();
        }

        private static void SerializesConcurrentWriters()
        {
            using (McpMutationWriterLease.Acquire("agent-a", "cad_create_line", 1000))
            {
                var entered = new ManualResetEventSlim(false);
                var completed = new ManualResetEventSlim(false);
                Exception? error = null;

                var thread = new Thread(() =>
                {
                    try
                    {
                        using (McpMutationWriterLease.Acquire("agent-b", "cad_create_circle", 1000))
                        {
                            entered.Set();
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        completed.Set();
                    }
                });
                thread.IsBackground = true;
                thread.Start();

                if (entered.Wait(150))
                    throw new InvalidOperationException("Second MCP writer entered while the first writer lease was still held.");

                var snapshot = McpMutationWriterLease.Snapshot();
                if (!snapshot.Held || snapshot.Owner != "agent-a" || snapshot.Operation != "cad_create_line")
                    throw new InvalidOperationException("Writer lease snapshot did not preserve the active owner/operation.");
                if (snapshot.WaiterCount < 1)
                    throw new InvalidOperationException("Writer lease snapshot did not expose the queued second writer.");

                // Release happens when leaving this using scope. The worker is joined below.
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(25);
                });

                if (error != null)
                    throw new InvalidOperationException("Second writer failed before the first lease was released.", error);

                // Do not wait for completion here: the worker must still be blocked by this lease.
                if (completed.IsSet)
                    throw new InvalidOperationException("Second writer completed before the first lease was released.");
            }

            using (var acquired = McpMutationWriterLease.Acquire("agent-c", "cad_create_arc", 1000))
            {
                var snapshot = McpMutationWriterLease.Snapshot();
                if (!snapshot.Held || snapshot.Owner != "agent-c")
                    throw new InvalidOperationException("Writer lease was not transferable after release.");
            }

            if (McpMutationWriterLease.Snapshot().Held)
                throw new InvalidOperationException("Writer lease remained held after disposal.");
        }

        private static void ReportsBusyOwnerOnTimeout()
        {
            using (McpMutationWriterLease.Acquire("agent-owner", "cad_command_sequence", 1000))
            {
                Exception? error = null;
                var done = new ManualResetEventSlim(false);
                var thread = new Thread(() =>
                {
                    try
                    {
                        using (McpMutationWriterLease.Acquire("agent-waiter", "cad_create_line", 75)) { }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        done.Set();
                    }
                });
                thread.IsBackground = true;
                thread.Start();
                if (!done.Wait(1500)) throw new InvalidOperationException("Timed writer did not complete.");

                var busy = error as McpMutationWriteBusyException;
                if (busy == null)
                    throw new InvalidOperationException("Concurrent writer timeout did not return McpMutationWriteBusyException.", error);
                if (busy.ActiveOwner != "agent-owner" || busy.ActiveOperation != "cad_command_sequence")
                    throw new InvalidOperationException("Busy error did not identify the active writer.");
            }
        }
    }
}
