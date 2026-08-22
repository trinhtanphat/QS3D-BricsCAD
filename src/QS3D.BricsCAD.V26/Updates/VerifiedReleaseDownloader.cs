using System;
using System.Threading.Tasks;

namespace QS3D.BricsCAD.V25.Updates
{
    internal sealed class VerifiedReleaseDownload
    {
        internal VerifiedReleaseDownload(string path, string sha256)
        {
            Path = path;
            Sha256 = sha256;
        }

        internal string Path { get; }
        internal string Sha256 { get; }
    }

    internal sealed class VerifiedReleaseDownloader
    {
        internal Task<VerifiedReleaseDownload> DownloadAsync(UpdateReleaseInfo release)
        {
            if (release == null) throw new ArgumentNullException(nameof(release));

            return Task.FromException<VerifiedReleaseDownload>(new InvalidOperationException(
                "Tải package preview trực tiếp bị tắt cho BricsCAD V26. Hãy mở đúng trang GitHub Release; QS3D chỉ tự cài V26 qua manifest và package ký số đã xác minh."));
        }
    }
}
