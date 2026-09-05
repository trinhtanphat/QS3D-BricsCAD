using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

const long MaxAssemblyBytes = 256L * 1024L * 1024L;
const string Marker = "QS3D_ASSEMBLY_VERSION:";

try
{
    using Stream stdin = Console.OpenStandardInput();
    using var image = new MemoryStream();
    var buffer = new byte[64 * 1024];
    long total = 0;

    while (true)
    {
        int read = stdin.Read(buffer, 0, buffer.Length);
        if (read == 0)
        {
            break;
        }

        total = checked(total + read);
        if (total > MaxAssemblyBytes)
        {
            throw new InvalidDataException($"Assembly image exceeds the {MaxAssemblyBytes}-byte safety limit.");
        }

        image.Write(buffer, 0, read);
    }

    if (image.Length == 0)
    {
        throw new InvalidDataException("Assembly image is empty.");
    }

    image.Position = 0;
    using var peReader = new PEReader(image, PEStreamOptions.LeaveOpen);
    if (!peReader.HasMetadata)
    {
        throw new BadImageFormatException("PE image does not contain managed metadata.");
    }

    MetadataReader metadata = peReader.GetMetadataReader();
    if (!metadata.IsAssembly)
    {
        throw new BadImageFormatException("Managed metadata does not describe an assembly.");
    }

    Version version = metadata.GetAssemblyDefinition().Version;
    Console.Out.WriteLine($"{Marker}{version}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"V26 held-byte assembly metadata probe failed: {ex.GetType().Name}: {ex.Message}");
    return 2;
}
