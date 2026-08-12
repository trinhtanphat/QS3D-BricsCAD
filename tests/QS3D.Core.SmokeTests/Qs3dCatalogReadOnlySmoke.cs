using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs3dCatalogReadOnlySmoke
    {
        public static void Run()
        {
            CatalogsRejectMutationThroughListInterface();
        }

        private static void CatalogsRejectMutationThroughListInterface()
        {
            RequireReadOnly(Qs3dCatalog.RoomFinishItems, 7, "Phòng");
            RequireReadOnly(Qs3dCatalog.WallItems, 3, "Tường Gạch");
            RequireReadOnly(Qs3dCatalog.DoorItems, 2, "Lỗ Mở Vách");
        }

        private static void RequireReadOnly(IReadOnlyList<string> values, int expectedCount, string expectedFirst)
        {
            if (values.Count != expectedCount)
                throw new InvalidOperationException("QS3D catalog count changed unexpectedly.");
            if (!string.Equals(expectedFirst, values[0], StringComparison.Ordinal))
                throw new InvalidOperationException("QS3D catalog first item changed unexpectedly.");

            if (!(values is IList<string> mutableView))
                throw new InvalidOperationException("QS3D catalog regression smoke requires an IList view to verify mutation is rejected.");

            try
            {
                mutableView[0] = "CORRUPTED";
            }
            catch (NotSupportedException)
            {
                if (!string.Equals(expectedFirst, values[0], StringComparison.Ordinal))
                    throw new InvalidOperationException("Rejected QS3D catalog mutation still changed global state.");
                return;
            }

            throw new InvalidOperationException("Public QS3D catalog can be mutated through its runtime IList implementation.");
        }
    }

    internal static class Qs3dCatalogReadOnlySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Qs3dCatalogReadOnlySmoke.Run();
        }
    }
}
