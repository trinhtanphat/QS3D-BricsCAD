using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class QuantBimSelectionInterchangeAdmission
    {
        internal QuantBimSelectionInterchangeAdmission(
            QuantBimSelectionInterchangePackage package,
            IfcSelectionSet selection)
        {
            Package = package ?? throw new ArgumentNullException("package");
            Selection = selection ?? throw new ArgumentNullException("selection");
        }

        public QuantBimSelectionInterchangePackage Package { get; private set; }
        public IfcSelectionSet Selection { get; private set; }
    }

    /// <summary>
    /// Admits an authenticated V2 interchange package only when it belongs to the exact
    /// standalone IFC generation currently open in the workbench.
    /// </summary>
    public sealed class QuantBimSelectionInterchangeV2Admission
    {
        public QuantBimSelectionInterchangeAdmission Admit(
            QuantBimStandaloneIfcSession session,
            string encoded)
        {
            if (session == null) throw new ArgumentNullException("session");
            var package = QuantBimSelectionInterchangeV2Codec.Decode(encoded);

            if (!string.Equals(session.Path, package.DocumentPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QuantBIM interchange document path does not match the open IFC generation.");
            if (!string.Equals(session.Revision, package.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM interchange revision does not match the open IFC generation.");

            var canonicalGuids = new List<string>(package.SelectionGuids.Count);
            foreach (var guid in package.SelectionGuids)
            {
                var matches = session.Document.Elements
                    .Where(x => string.Equals(x.Guid, guid, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matches.Count != 1)
                    throw new InvalidOperationException("QuantBIM interchange selection GUID must resolve exactly once in the open IFC generation: " + guid + ".");
                canonicalGuids.Add(matches[0].Guid);
            }

            var selection = new IfcSelectionSet(package.SelectionName, canonicalGuids);
            if (selection.Guids.Count != package.SelectionGuids.Count)
                throw new InvalidOperationException("QuantBIM interchange selection collapses to duplicate IFC identity.");

            return new QuantBimSelectionInterchangeAdmission(package, selection);
        }
    }
}
