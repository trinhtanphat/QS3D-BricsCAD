using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Agent.Harness;

namespace QS3D.Core.SmokeTests
{
    internal static class AgentHarnessCoreSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Run();
        }

        internal static void Run()
        {
            RoutesMcpDurabilityThroughPrerequisites();
            DoesNotEagerlyLoadMcpOrCadSkillsForSourceOnlyTask();
            RejectsDuplicateSkillIds();
            RejectsSkillDependencyCycles();
            HardDeniesUnsafeRepositoryPermissions();
            CadMutationRequiresConfirmationByDefault();
            RejectsIllegalLifecycleTransition();
            TraceIsMonotonicRedactedAndReasoningFree();
            EngineCreatesDeterministicInitialSnapshot();
        }

        private static void RoutesMcpDurabilityThroughPrerequisites()
        {
            var intent = new TaskRouter().Classify("fix MCP save durability and CI");
            True(intent.Domains.Contains(TaskDomain.McpTransport));
            True(intent.Domains.Contains(TaskDomain.PersistenceDurability));
            True(intent.Domains.Contains(TaskDomain.ContinuousIntegration));

            var skills = new SkillRouter(SkillCatalog.CreateDefault()).Route(intent).Select(x => x.Id).ToArray();
            SequenceEqual(
                new[] { "repository-lifecycle", "tdd-source", "ci-remediation", "mcp-transport", "persistence-durability" },
                skills);
        }

        private static void DoesNotEagerlyLoadMcpOrCadSkillsForSourceOnlyTask()
        {
            var intent = new TaskRouter().Classify("refactor quantity report source code");
            var skills = new SkillRouter(SkillCatalog.CreateDefault()).Route(intent).Select(x => x.Id).ToArray();

            SequenceEqual(new[] { "repository-lifecycle", "tdd-source" }, skills);
            True(!skills.Contains("mcp-transport"));
            True(!skills.Contains("bricscad-host"));
            True(!skills.Contains("cad-safety"));
        }

        private static void RejectsDuplicateSkillIds()
        {
            Throws<InvalidOperationException>(() => new SkillCatalog(new[]
            {
                Skill("same", TaskDomain.Source),
                Skill("SAME", TaskDomain.ContinuousIntegration)
            }));
        }

        private static void RejectsSkillDependencyCycles()
        {
            var catalog = new SkillCatalog(new[]
            {
                Skill("a", TaskDomain.Source, "b"),
                Skill("b", TaskDomain.Source, "a")
            });

            Throws<InvalidOperationException>(() => new SkillRouter(catalog).Route(new TaskIntent(
                "cycle",
                new[] { TaskDomain.Source },
                new[] { "test" })));
        }

        private static void HardDeniesUnsafeRepositoryPermissions()
        {
            var policy = new HarnessPolicy();
            Equal(PermissionDecision.Deny, policy.Resolve(HarnessPermission.SecretExport));
            Equal(PermissionDecision.Deny, policy.Resolve(HarnessPermission.ForcePushProtected));
            Equal(PermissionDecision.Deny, policy.Resolve(HarnessPermission.BypassCi));
            Equal(PermissionDecision.Deny, policy.Resolve(HarnessPermission.BypassReservation));
            Equal(PermissionDecision.Deny, policy.Resolve(HarnessPermission.WriteOutsideWorkspace));
            Equal(PermissionDecision.Deny, policy.Resolve(HarnessPermission.UntypedDestructiveExternal));
        }

        private static void CadMutationRequiresConfirmationByDefault()
        {
            var policy = new HarnessPolicy();
            Equal(PermissionDecision.Auto, policy.Resolve(HarnessPermission.CadInspect));
            Equal(PermissionDecision.Confirm, policy.Resolve(HarnessPermission.CadMutate));
            Equal(PermissionDecision.Confirm, policy.Resolve(HarnessPermission.SaveActiveDrawing));
        }

        private static void RejectsIllegalLifecycleTransition()
        {
            var lifecycle = new HarnessLifecycle();
            Equal(HarnessState.Created, lifecycle.CurrentState);
            Throws<InvalidOperationException>(() => lifecycle.TransitionTo(HarnessState.Completed));

            lifecycle.TransitionTo(HarnessState.ContextResolving);
            lifecycle.TransitionTo(HarnessState.Ready);
            lifecycle.TransitionTo(HarnessState.Running);
            lifecycle.TransitionTo(HarnessState.WaitingPermission);
            lifecycle.TransitionTo(HarnessState.Running);
            lifecycle.TransitionTo(HarnessState.Completed);
            Equal(HarnessState.Completed, lifecycle.CurrentState);
        }

        private static void TraceIsMonotonicRedactedAndReasoningFree()
        {
            var session = new HarnessSession("session-1");
            var first = session.AppendTrace("session.started", "started", "main@abc", new Dictionary<string, string>
            {
                ["branch"] = "agent/example",
                ["token"] = "super-secret-value"
            });
            var second = session.AppendTrace("task.classified", "classified", null, null);

            Equal(1L, first.Sequence);
            Equal(2L, second.Sequence);
            True(second.TimestampUtc >= first.TimestampUtc);
            Equal("[REDACTED]", first.Metadata["token"]);
            Equal("agent/example", first.Metadata["branch"]);

            var names = typeof(HarnessTraceEvent)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(x => x.Name)
                .ToArray();
            True(!names.Any(x => x.IndexOf("reasoning", StringComparison.OrdinalIgnoreCase) >= 0));
            True(!names.Any(x => x.IndexOf("secret", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static void EngineCreatesDeterministicInitialSnapshot()
        {
            var engine = HarnessEngine.CreateDefault();
            var first = engine.CreateInitialSnapshot("fix MCP save durability and CI", "session-a");
            var second = engine.CreateInitialSnapshot("fix MCP save durability and CI", "session-b");

            SequenceEqual(first.Intent.Domains.Select(x => x.ToString()).ToArray(), second.Intent.Domains.Select(x => x.ToString()).ToArray());
            SequenceEqual(first.Skills.Select(x => x.Id).ToArray(), second.Skills.Select(x => x.Id).ToArray());
            Equal(HarnessState.Ready, first.State);
            True(first.Trace.Count >= 3);
            True(first.Trace.All(x => !string.IsNullOrWhiteSpace(x.Summary)));
        }

        private static SkillDescriptor Skill(string id, TaskDomain trigger, params string[] prerequisites)
        {
            return new SkillDescriptor(
                id,
                1,
                new[] { trigger },
                prerequisites,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        private static void SequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
        {
            if (expected.Count != actual.Count)
                throw new Exception("Expected sequence length " + expected.Count + ", got " + actual.Count + ".");

            for (var i = 0; i < expected.Count; i++)
            {
                if (!Equals(expected[i], actual[i]))
                    throw new Exception("Expected sequence item " + i + " to be " + expected[i] + ", got " + actual[i] + ".");
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value)
                throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
