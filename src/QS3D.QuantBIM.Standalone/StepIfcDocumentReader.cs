using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace QS3D.QuantBIM.Standalone;

public sealed class StepIfcDocumentReader : IIfcDocumentReader
{
    private static readonly Regex Schema = new(@"FILE_SCHEMA\s*\(\s*\(\s*'(?<schema>[^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Product = new(@"#\d+\s*=\s*(?<type>IFC[A-Z0-9_]+)\s*\(\s*'(?<gid>[^']+)'\s*,[^,]*,\s*(?:'(?<name>[^']*)'|\$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly HashSet<string> NonProducts = new(StringComparer.OrdinalIgnoreCase)
    {
        "IFCPROJECT", "IFCSITE", "IFCBUILDING", "IFCBUILDINGSTOREY", "IFCOWNERHISTORY",
        "IFCPERSON", "IFCORGANIZATION", "IFCAPPLICATION", "IFCUNITASSIGNMENT"
    };

    public async Task<IfcDocument> OpenAsync(IfcOpenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Path);
        var fullPath = Path.GetFullPath(request.Path);
        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
        var sha = Convert.ToHexString(SHA256.HashData(bytes));
        if (request.ExpectedSha256 is not null && !sha.Equals(request.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("IFC source SHA-256 does not match the expected identity.");

        var text = System.Text.Encoding.UTF8.GetString(bytes);
        var schema = Schema.Match(text);
        if (!schema.Success)
            throw new InvalidDataException("IFC FILE_SCHEMA header is missing or unsupported.");

        var components = new List<IfcComponent>();
        var globalIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in Product.Matches(text))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var type = match.Groups["type"].Value.ToUpperInvariant();
            if (NonProducts.Contains(type)) continue;
            var globalId = match.Groups["gid"].Value;
            if (!globalIds.Add(globalId))
                throw new InvalidDataException($"Duplicate IFC GlobalId '{globalId}'.");
            var name = match.Groups["name"].Success ? match.Groups["name"].Value : string.Empty;
            components.Add(new IfcComponent(globalId, type, name, null,
                new Dictionary<string, string>(StringComparer.Ordinal),
                new QuantityEvidence(sha, globalId, "IFC_STEP_IDENTITY", Array.Empty<QuantityValue>())));
        }

        components.Sort((a, b) => StringComparer.Ordinal.Compare(a.GlobalId, b.GlobalId));
        return new IfcDocument(sha[..16], schema.Groups["schema"].Value.ToUpperInvariant(), fullPath, sha, components);
    }
}
