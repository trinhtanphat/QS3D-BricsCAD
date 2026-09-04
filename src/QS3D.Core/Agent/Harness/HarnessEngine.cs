using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Agent.Harness
{
    public sealed class HarnessExecutionSnapshot
    {
        internal HarnessExecutionSnapshot(
            TaskIntent intent,
            IEnumerable<SkillDescriptor> skills,
            HarnessState state,
            IEnumerable<HarnessTraceEvent> trace)
        {
            Intent = intent ?? throw new ArgumentNullException(nameof(intent));
            Skills = (skills ?? throw new ArgumentNullException(nameof(skills))).ToArray();
            State = state;
            Trace = (trace ?? throw new ArgumentNullException(nameof(trace))).ToArray();
        }

        public TaskIntent Intent { get; }
        public IReadOnlyList<SkillDescriptor> Skills { get; }
        public HarnessState State { get; }
        public IReadOnlyList<HarnessTraceEvent> Trace { get; }
    }

    public sealed class HarnessEngine
    {
        private readonly TaskRouter _taskRouter;
        private readonly SkillRouter _skillRouter;
        private readonly HarnessPolicy _policy;

        public HarnessEngine(TaskRouter taskRouter, SkillRouter skillRouter, HarnessPolicy policy)
        {
            _taskRouter = taskRouter ?? throw new ArgumentNullException(nameof(taskRouter));
            _skillRouter = skillRouter ?? throw new ArgumentNullException(nameof(skillRouter));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public HarnessPolicy Policy => _policy;

        public static HarnessEngine CreateDefault()
        {
            var catalog = SkillCatalog.CreateDefault();
            return new HarnessEngine(new TaskRouter(), new SkillRouter(catalog), new HarnessPolicy());
        }

        public HarnessExecutionSnapshot CreateInitialSnapshot(string prompt, string sessionId)
        {
            var session = new HarnessSession(sessionId);
            var lifecycle = new HarnessLifecycle();

            session.AppendTrace("session.started", "Harness session started.", null, null);
            lifecycle.TransitionTo(HarnessState.ContextResolving);
            session.AppendTrace("context.resolving", "Resolving task context.", null, null);

            var intent = _taskRouter.Classify(prompt);
            session.AppendTrace(
                "task.classified",
                "Task classified into " + intent.Domains.Count + " domain(s).",
                null,
                new Dictionary<string, string> { ["domains"] = string.Join(",", intent.Domains.Select(value => value.ToString())) });

            var skills = _skillRouter.Route(intent);
            for (var i = 0; i < skills.Count; i++)
            {
                session.AppendTrace(
                    "skill.loaded",
                    "Loaded skill " + skills[i].Id + ".",
                    null,
                    new Dictionary<string, string> { ["skill"] = skills[i].Id, ["version"] = skills[i].Version.ToString() });
            }

            lifecycle.TransitionTo(HarnessState.Ready);
            session.AppendTrace("session.ready", "Harness session is ready.", null, null);

            return new HarnessExecutionSnapshot(intent, skills, lifecycle.CurrentState, session.Trace);
        }
    }
}
