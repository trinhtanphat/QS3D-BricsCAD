using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class RebarNotationReadOnlyResultSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CompoundNotationRemainsOrderedAndReadOnly();
        }

        private static void CompoundNotationRemainsOrderedAndReadOnly()
        {
            var groups = RebarNotationParser.Parse("2x3D16+D12@200");
            if (groups.Count != 2 || groups[0].Quantity != 6 || Math.Abs(groups[0].DiameterMm - 16d) > 1e-12d ||
                !groups[1].SpacingMm.HasValue || Math.Abs(groups[1].SpacingMm.GetValueOrDefault() - 200d) > 1e-12d ||
                Math.Abs(groups[1].DiameterMm - 12d) > 1e-12d)
                throw new InvalidOperationException("Rebar notation group ordering or parsed values changed while hardening the result boundary.");

            if (!(groups is ICollection<RebarGroup> collection) || !collection.IsReadOnly)
                throw new InvalidOperationException("Rebar notation parser must expose a structural read-only collection boundary.");

            try
            {
                collection.Add(new RebarGroup { DiameterMm = 10d });
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException("Rebar notation parser result accepted structural mutation through ICollection<T>.");
        }
    }
}
