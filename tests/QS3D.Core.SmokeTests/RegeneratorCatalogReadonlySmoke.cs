using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegeneratorCatalogReadonlySmoke
    {
        internal static void Run()
        {
            var catalog = RegeneratorCatalog.CreateDefault();

            Equal(5, catalog.Count, "count");
            TypeAt<OpeningRegenerator>(catalog, 0);
            TypeAt<WallRegenerator>(catalog, 1);
            TypeAt<StructuralRegenerator>(catalog, 2);
            TypeAt<RoomRegenerator>(catalog, 3);
            TypeAt<GenericTakeoffRegenerator>(catalog, 4);

            if (catalog is IElementRegenerator[])
                throw new Exception("RegeneratorCatalogReadonlySmoke: default catalog must not expose a mutable array.");
            if (!(catalog is IList<IElementRegenerator> list))
                throw new Exception("RegeneratorCatalogReadonlySmoke: expected IList compatibility for mutation guard verification.");

            try
            {
                list[0] = catalog[1];
            }
            catch (NotSupportedException)
            {
                TypeAt<OpeningRegenerator>(catalog, 0);
                return;
            }

            throw new Exception("RegeneratorCatalogReadonlySmoke: index replacement was accepted.");
        }

        private static void TypeAt<T>(IReadOnlyList<IElementRegenerator> catalog, int index)
        {
            if (catalog[index].GetType() != typeof(T))
                throw new Exception(
                    "RegeneratorCatalogReadonlySmoke type at " + index + ": expected=" +
                    typeof(T).Name + ", actual=" + catalog[index].GetType().Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception("RegeneratorCatalogReadonlySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class RegeneratorCatalogReadonlySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RegeneratorCatalogReadonlySmoke.Run();
    }
}
