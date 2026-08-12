using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Formulas;

namespace QS3D.Core.SmokeTests
{
    internal static class ExpressionReferencedVariablesReadOnlySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var variables = new ExpressionEvaluator().GetReferencedVariables("Width + height + WIDTH + max(Depth, 2)");
            var ordered = variables.ToList();

            if (ordered.Count != 3 ||
                !string.Equals(ordered[0], "Width", StringComparison.Ordinal) ||
                !string.Equals(ordered[1], "height", StringComparison.Ordinal) ||
                !string.Equals(ordered[2], "Depth", StringComparison.Ordinal))
                throw new Exception("Expression referenced-variable result must preserve first-reference order and case-insensitive deduplication.");

            if (variables is List<string>)
                throw new Exception("Expression referenced-variable result must not expose its mutable List backing.");

            if (variables is ICollection<string> collection)
            {
                if (!collection.IsReadOnly)
                    throw new Exception("Expression referenced-variable result must report generic collection mutation as read-only.");

                try
                {
                    collection.Add("Injected");
                }
                catch (NotSupportedException)
                {
                    return;
                }

                throw new Exception("Expression referenced-variable result accepted mutation through ICollection<string>.");
            }
        }
    }
}
