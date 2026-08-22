using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleExpressionPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var rejected = false;
            try
            {
                _ = new QuantityRule("RULE-INVALID", ElementCategory.ArchitecturalWall, "Area", "Length + \u0001Height", "1");
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            if (!rejected)
                throw new InvalidOperationException("QuantityRule accepted an XML-illegal expression character.");

            const string expression = "Length * Height + 1";
            var rule = new QuantityRule("RULE-VALID", ElementCategory.ArchitecturalWall, "Area", "  " + expression + "  ", "1");
            if (!string.Equals(expression, rule.Expression, StringComparison.Ordinal))
                throw new InvalidOperationException("QuantityRule expression required/trim semantics changed.");

            var project = new ProjectState("RULE-EXPR", "Quantity rule expression persistability");
            project.QuantityRules.Add(rule);

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-rule-expression-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                if (loaded.QuantityRules.Count != 1)
                    throw new InvalidOperationException("QuantityRule did not round-trip through QSDB.");
                if (!string.Equals(expression, loaded.QuantityRules[0].Expression, StringComparison.Ordinal))
                    throw new InvalidOperationException("QuantityRule expression changed across QSDB SaveNew/Load.");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }
    }
}
