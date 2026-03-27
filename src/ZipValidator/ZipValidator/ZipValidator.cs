using System.IO.Compression;

public class ZipValidator
{
    public long MaxTotalUncompressedSize { get; init; } = 500L * 1024 * 1024; // 500MB

    public double MaxCompressionRatio { get; init; } = 100;

    public int MaxFileCount { get; init; } = 10000;

    private const int BufferSize = 81920;

    public void ExtractSafe(string zipPath, string destination)
    {
        using var stream = File.OpenRead(zipPath);
        ProcessArchive(stream, destination, validateOnly: false);
    }

    public void Validate(string zipPath)
    {
        using var stream = File.OpenRead(zipPath);
        ProcessArchive(stream, destination: Path.GetTempPath(), validateOnly: true);
    }

    public void Validate(Stream zipStream)
    {
        ProcessArchive(zipStream, destination: Path.GetTempPath(), validateOnly: true);
    }

    private void ProcessArchive(Stream zipStream, string? destination, bool validateOnly)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        if (archive.Entries.Count > MaxFileCount)
            throw new InvalidDataException("Too many files in archive.");

        long totalUncompressed = 0;

        foreach (var entry in archive.Entries)
        {
            if (entry.CompressedLength > 0)
            {
                double ratio = (double)entry.Length / entry.CompressedLength;

                if (ratio > MaxCompressionRatio)
                    throw new InvalidDataException("Suspicious compression ratio detected.");
            }

            totalUncompressed += entry.Length;

            if (totalUncompressed > MaxTotalUncompressedSize)
                throw new InvalidDataException("Total uncompressed size exceeded.");
        }

        foreach (var entry in archive.Entries)
        {
            ProcessEntry(entry, destination, validateOnly);
        }
    }

    private void ProcessEntry(ZipArchiveEntry entry, string? destination, bool validateOnly)
    {
        if (string.IsNullOrEmpty(entry.Name))
            return;

        var destFullPath = Path.GetFullPath(Path.Combine(destination!, entry.FullName));

        if (!destFullPath.StartsWith(Path.GetFullPath(destination!), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Zip Slip attack detected.");

        if (!validateOnly)
            Directory.CreateDirectory(Path.GetDirectoryName(destFullPath)!);

        using var stream = entry.Open();
        using var output = validateOnly ? null : File.Create(destFullPath);

        byte[] buffer = new byte[BufferSize];
        long written = 0;

        int read;

        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            written += read;

            if (written > MaxTotalUncompressedSize)
                throw new InvalidDataException("Extraction size limit exceeded.");

            output?.Write(buffer, 0, read);
        }
    }
}