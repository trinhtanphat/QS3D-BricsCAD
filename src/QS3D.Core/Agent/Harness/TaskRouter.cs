using System;
using System.Collections.Generic;

namespace QS3D.Core.Agent.Harness
{
    public sealed class TaskRouter
    {
        public TaskIntent Classify(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Task prompt is required.", nameof(prompt));

            var domains = new List<TaskDomain>();
            var evidence = new List<string>();

            if (HasAny(prompt, "fix", "refactor", "implement", "code", "source", "bug", "test"))
                Add(domains, evidence, TaskDomain.Source, "source-change");

            if (HasAny(prompt, " ci", "ci ", "pipeline", "preflight", "github actions", "workflow failure", "build failure"))
                Add(domains, evidence, TaskDomain.ContinuousIntegration, "ci");

            if (HasAny(prompt, "github", "pull request", " pr ", "issue", "carrier", "branch", "merge"))
                Add(domains, evidence, TaskDomain.GithubCarrier, "github-carrier");

            if (HasAny(prompt, "mcp", "transport", "cloudflare", "tunnel", "oauth"))
                Add(domains, evidence, TaskDomain.McpTransport, "mcp-transport");

            if (HasAny(prompt, "durability", "durable", "persistence", "persist", "retry identity", "save", "reopen"))
                Add(domains, evidence, TaskDomain.PersistenceDurability, "persistence-durability");

            if (HasAny(prompt, "bricscad", "host runtime", "v25", "v26"))
                Add(domains, evidence, TaskDomain.BricsCadHost, "bricscad-host");

            if (HasAny(prompt, "release", "package", "installer", "publish"))
                Add(domains, evidence, TaskDomain.ReleasePackage, "release-package");

            if (HasAny(prompt, "cad inspect", "inspect drawing", "inspect dwg", "selection snapshot", "entity snapshot"))
                Add(domains, evidence, TaskDomain.CadInspect, "cad-inspect");

            if (HasAny(prompt, "cad mutate", "modify drawing", "modify dwg", "create entity", "delete entity", "move entity"))
                Add(domains, evidence, TaskDomain.CadMutate, "cad-mutate");

            if (domains.Count == 0)
                Add(domains, evidence, TaskDomain.Source, "default-source");

            return new TaskIntent(prompt, domains, evidence);
        }

        private static bool HasAny(string text, params string[] terms)
        {
            for (var i = 0; i < terms.Length; i++)
            {
                if (text.IndexOf(terms[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static void Add(List<TaskDomain> domains, List<string> evidence, TaskDomain domain, string marker)
        {
            if (!domains.Contains(domain))
                domains.Add(domain);
            if (!evidence.Contains(marker))
                evidence.Add(marker);
        }
    }
}
