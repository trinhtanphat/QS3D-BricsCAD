using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Agent.Harness
{
    public sealed class SkillRouter
    {
        private readonly SkillCatalog _catalog;

        public SkillRouter(SkillCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public IReadOnlyList<SkillDescriptor> Route(TaskIntent intent)
        {
            if (intent == null)
                throw new ArgumentNullException(nameof(intent));

            var result = new List<SkillDescriptor>();
            var state = new Dictionary<string, VisitState>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < _catalog.Skills.Count; i++)
            {
                var skill = _catalog.Skills[i];
                if (skill.Triggers.Any(intent.Domains.Contains))
                    Visit(skill, state, result);
            }

            return result.ToArray();
        }

        private void Visit(
            SkillDescriptor skill,
            IDictionary<string, VisitState> state,
            IList<SkillDescriptor> result)
        {
            VisitState current;
            if (state.TryGetValue(skill.Id, out current))
            {
                if (current == VisitState.Visiting)
                    throw new InvalidOperationException("Skill dependency cycle detected at '" + skill.Id + "'.");
                if (current == VisitState.Visited)
                    return;
            }

            state[skill.Id] = VisitState.Visiting;
            for (var i = 0; i < skill.Prerequisites.Count; i++)
                Visit(_catalog.Find(skill.Prerequisites[i]), state, result);

            state[skill.Id] = VisitState.Visited;
            result.Add(skill);
        }

        private enum VisitState
        {
            Visiting,
            Visited
        }
    }
}
