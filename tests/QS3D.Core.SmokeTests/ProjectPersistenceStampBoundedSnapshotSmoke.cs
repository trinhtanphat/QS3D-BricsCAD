using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampBoundedSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsSemanticSnapshotGrowthBeyondMaterializationBudget();
        }

        private static void RejectsSemanticSnapshotGrowthBeyondMaterializationBudget()
        {
            var type = typeof(ProjectPersistenceStamp);
            var budgetField = type.GetField("MaximumSnapshotCharacters", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new Exception("ProjectPersistenceStamp bounded semantic snapshot budget is missing.");
            var budgetValue = budgetField.GetRawConstantValue();
            if (!(budgetValue is int budget) || budget != 64 * 1024 * 1024)
                throw new Exception("ProjectPersistenceStamp semantic snapshot budget must remain exactly 64 Mi-characters.");

            var requireCapacity = type.GetMethod(
                "RequireSnapshotCapacity",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(StringBuilder), typeof(long) },
                modifiers: null)
                ?? throw new Exception("ProjectPersistenceStamp bounded semantic snapshot admission helper is missing.");

            var snapshot = new StringBuilder("seed");
            requireCapacity.Invoke(null, new object[] { snapshot, (long)budget - snapshot.Length });
            if (snapshot.ToString() != "seed")
                throw new Exception("Persistence stamp capacity admission unexpectedly mutated the semantic snapshot.");

            try
            {
                requireCapacity.Invoke(null, new object[] { snapshot, (long)budget - snapshot.Length + 1L });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException invalid)
            {
                if (invalid.Message.IndexOf("64 Mi-character", StringComparison.Ordinal) < 0)
                    throw new Exception("Persistence stamp bounded snapshot rejection lost its deterministic diagnostic.", invalid);
                return;
            }

            throw new Exception("Persistence stamp accepted cumulative semantic snapshot growth beyond its bounded materialization budget.");
        }
    }
}
