using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    internal static class SemanticPropertyEditPolicy
    {
        private static readonly HashSet<string> SourceDerivedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LengthM",
            "AreaM2",
            "VolumeM3",
            "PerimeterM",
            "Layer",
            MeasuredSolidQuantityPolicy.VolumeProperty,
            MeasuredSolidQuantityPolicy.SurfaceAreaProperty
        };

        private static readonly HashSet<string> ReservedIdentityKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "ElementId",
            "Category",
            "FamilyId",
            "FloorId",
            "ZoneId"
        };

        internal static string RequireEditablePropertyKey(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Property name is required.", nameof(propertyName));
            var key = propertyName.Trim();
            if (SourceDerivedKeys.Contains(key) || key.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Property is derived from CAD/source geometry and cannot be edited as a generic semantic property: " + key + ".");
            if (ReservedIdentityKeys.Contains(key) || LooksLikeIdentityReferenceKey(key))
                throw new InvalidOperationException("Semantic identity/reference field cannot be edited as a generic property: " + key + ".");
            if (key.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Native/generated ownership state cannot be edited as a generic semantic property: " + key + ".");
            return key;
        }

        private static bool LooksLikeIdentityReferenceKey(string key)
        {
            return key.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Ids", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Ref", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("Refs", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("RefId", StringComparison.OrdinalIgnoreCase) ||
                   key.EndsWith("RefIds", StringComparison.OrdinalIgnoreCase);
        }
    }
}
