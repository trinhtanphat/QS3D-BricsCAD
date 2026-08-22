using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Model;

namespace QS3D.Core.SmokeTests
{
    internal static class EntitySnapshotLayerNullInvariantSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var snapshot = new EntitySnapshot("1A", "LINE", null!);
            if (!string.Equals(snapshot.Layer, string.Empty, StringComparison.Ordinal))
                throw new Exception("EntitySnapshot must normalize a null constructor layer to an empty string.");

            snapshot.Layer = " Layer A ";
            if (!string.Equals(snapshot.Layer, " Layer A ", StringComparison.Ordinal))
                throw new Exception("EntitySnapshot layer assignment must preserve valid layer text exactly.");

            snapshot.Layer = null!;
            if (!string.Equals(snapshot.Layer, string.Empty, StringComparison.Ordinal))
                throw new Exception("EntitySnapshot must preserve its non-null Layer invariant after reassignment.");
        }
    }
}
