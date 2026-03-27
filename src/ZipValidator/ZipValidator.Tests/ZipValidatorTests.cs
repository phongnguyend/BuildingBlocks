using System.IO.Compression;
using Xunit;

namespace ZipValidatorTests;

public class ZipValidatorTests : IDisposable
{
    private readonly string _tempDir;

    public ZipValidatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ZipValidatorTests", Guid.CreateVersion7().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string CreateTempPath(string name) => Path.Combine(_tempDir, name);

    #region Helpers

    /// <summary>
    /// Creates a zip file with the specified entries. Each entry is a (relative path, content bytes) tuple.
    /// </summary>
    private static byte[] CreateZipBytes(params (string entryName, byte[] content)[] entries)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (entryName, content) in entries)
            {
                var entry = archive.CreateEntry(entryName);
                using var entryStream = entry.Open();
                entryStream.Write(content, 0, content.Length);
            }
        }

        ms.Position = 0;
        return ms.ToArray();
    }

    private string WriteZipFile(string fileName, byte[] zipBytes)
    {
        var path = CreateTempPath(fileName);
        File.WriteAllBytes(path, zipBytes);
        return path;
    }

    #endregion

    #region Successful extraction

    [Fact]
    public void ExtractSafe_SingleFile_ExtractsSuccessfully()
    {
        var content = "Hello, World!"u8.ToArray();
        var zipBytes = CreateZipBytes(("hello.txt", content));
        var zipPath = WriteZipFile("single.zip", zipBytes);
        var destination = CreateTempPath("out_single");

        var validator = new ZipValidator();
        validator.ExtractSafe(zipPath, destination);

        var extractedPath = Path.Combine(destination, "hello.txt");
        Assert.True(File.Exists(extractedPath));
        Assert.Equal(content, File.ReadAllBytes(extractedPath));
    }

    [Fact]
    public void ExtractSafe_MultipleFiles_ExtractsAll()
    {
        var entries = new (string, byte[])[]
        {
            ("file1.txt", "Content 1"u8.ToArray()),
            ("file2.txt", "Content 2"u8.ToArray()),
            ("subdir/file3.txt", "Content 3"u8.ToArray()),
        };

        var zipBytes = CreateZipBytes(entries);
        var zipPath = WriteZipFile("multi.zip", zipBytes);
        var destination = CreateTempPath("out_multi");

        var validator = new global::ZipValidator();
        validator.ExtractSafe(zipPath, destination);

        Assert.True(File.Exists(Path.Combine(destination, "file1.txt")));
        Assert.True(File.Exists(Path.Combine(destination, "file2.txt")));
        Assert.True(File.Exists(Path.Combine(destination, "subdir", "file3.txt")));
        Assert.Equal("Content 3"u8.ToArray(), File.ReadAllBytes(Path.Combine(destination, "subdir", "file3.txt")));
    }

    [Fact]
    public void ExtractSafe_EmptyZip_ExtractsWithoutError()
    {
        var zipBytes = CreateZipBytes();
        var zipPath = WriteZipFile("empty.zip", zipBytes);
        var destination = CreateTempPath("out_empty");

        var validator = new global::ZipValidator();
        validator.ExtractSafe(zipPath, destination);

        // Destination directory might not even be created since there are no entries
        // but no exception should be thrown
    }

    [Fact]
    public void ExtractSafe_DirectoryEntries_AreSkipped()
    {
        // Directory entries have an empty Name (only FullName ending with /)
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("mydir/");
            var entry = archive.CreateEntry("mydir/file.txt");
            using var entryStream = entry.Open();
            entryStream.Write("data"u8);
        }

        ms.Position = 0;
        var zipPath = WriteZipFile("dirs.zip", ms.ToArray());
        var destination = CreateTempPath("out_dirs");

        var validator = new global::ZipValidator();
        validator.ExtractSafe(zipPath, destination);

        Assert.True(File.Exists(Path.Combine(destination, "mydir", "file.txt")));
    }

    #endregion

    #region Nested zip extraction

    [Fact]
    public void ExtractSafe_NestedZip_ExtractsAsFile()
    {
        var innerContent = "inner file content"u8.ToArray();
        var innerZipBytes = CreateZipBytes(("inner.txt", innerContent));
        var outerZipBytes = CreateZipBytes(("nested.zip", innerZipBytes));

        var zipPath = WriteZipFile("nested_outer.zip", outerZipBytes);
        var destination = CreateTempPath("out_nested");

        var validator = new global::ZipValidator();
        validator.ExtractSafe(zipPath, destination);

        var extractedPath = Path.Combine(destination, "nested.zip");
        Assert.True(File.Exists(extractedPath));
        Assert.Equal(innerZipBytes, File.ReadAllBytes(extractedPath));
    }

    #endregion

    #region MaxFileCount

    [Fact]
    public void ExtractSafe_TooManyFiles_Throws()
    {
        var entries = new (string, byte[])[6];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = ($"file{i}.txt", "x"u8.ToArray());
        }

        var zipBytes = CreateZipBytes(entries);
        var zipPath = WriteZipFile("toomany.zip", zipBytes);
        var destination = CreateTempPath("out_toomany");

        var validator = new global::ZipValidator { MaxFileCount = 5 };
        var ex = Assert.Throws<InvalidDataException>(() => validator.ExtractSafe(zipPath, destination));
        Assert.Contains("Too many files", ex.Message);
    }

    [Fact]
    public void ExtractSafe_ExactMaxFileCount_Succeeds()
    {
        var entries = new (string, byte[])[5];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = ($"file{i}.txt", "x"u8.ToArray());
        }

        var zipBytes = CreateZipBytes(entries);
        var zipPath = WriteZipFile("exactcount.zip", zipBytes);
        var destination = CreateTempPath("out_exactcount");

        var validator = new global::ZipValidator { MaxFileCount = 5 };
        validator.ExtractSafe(zipPath, destination);

        for (int i = 0; i < 5; i++)
        {
            Assert.True(File.Exists(Path.Combine(destination, $"file{i}.txt")));
        }
    }

    #endregion

    #region MaxTotalUncompressedSize

    [Fact]
    public void ExtractSafe_TotalUncompressedSizeExceeded_Throws()
    {
        // Create a file larger than the limit
        var largeContent = new byte[1024];
        Array.Fill(largeContent, (byte)'A');

        var zipBytes = CreateZipBytes(("large.txt", largeContent));
        var zipPath = WriteZipFile("toolarge.zip", zipBytes);
        var destination = CreateTempPath("out_toolarge");

        var validator = new global::ZipValidator { MaxTotalUncompressedSize = 512 };
        var ex = Assert.Throws<InvalidDataException>(() => validator.ExtractSafe(zipPath, destination));
        Assert.Contains("Total uncompressed size exceeded", ex.Message);
    }

    [Fact]
    public void ExtractSafe_MultiplFilesExceedTotalSize_Throws()
    {
        var content = new byte[300];
        Array.Fill(content, (byte)'B');

        var zipBytes = CreateZipBytes(
            ("a.txt", content),
            ("b.txt", content)
        );
        var zipPath = WriteZipFile("combined_large.zip", zipBytes);
        var destination = CreateTempPath("out_combined_large");

        // Each file is 300 bytes, total 600 > 500
        var validator = new global::ZipValidator { MaxTotalUncompressedSize = 500 };
        var ex = Assert.Throws<InvalidDataException>(() => validator.ExtractSafe(zipPath, destination));
        Assert.Contains("Total uncompressed size exceeded", ex.Message);
    }

    [Fact]
    public void ExtractSafe_ExactMaxTotalSize_Succeeds()
    {
        var content = new byte[256];
        Array.Fill(content, (byte)'C');

        var zipBytes = CreateZipBytes(("exact.txt", content));
        var zipPath = WriteZipFile("exactsize.zip", zipBytes);
        var destination = CreateTempPath("out_exactsize");

        var validator = new global::ZipValidator { MaxTotalUncompressedSize = 256 };
        validator.ExtractSafe(zipPath, destination);

        Assert.True(File.Exists(Path.Combine(destination, "exact.txt")));
    }

    #endregion

    #region Compression ratio

    [Fact]
    public void ExtractSafe_SuspiciousCompressionRatio_Throws()
    {
        // Create a zip with a highly compressible payload (all zeros)
        // We set MaxCompressionRatio very low to trigger the check
        var content = new byte[10000];
        Array.Fill(content, (byte)0);

        var zipBytes = CreateZipBytes(("zeros.bin", content));
        var zipPath = WriteZipFile("highratio.zip", zipBytes);
        var destination = CreateTempPath("out_highratio");

        // All zeros compress extremely well; set a very low threshold
        var validator = new global::ZipValidator { MaxCompressionRatio = 2 };
        var ex = Assert.Throws<InvalidDataException>(() => validator.ExtractSafe(zipPath, destination));
        Assert.Contains("Suspicious compression ratio detected", ex.Message);
    }

    [Fact]
    public void ExtractSafe_NormalCompressionRatio_Succeeds()
    {
        // Random-ish data won't compress well
        var rng = new Random(42);
        var content = new byte[1000];
        rng.NextBytes(content);

        var zipBytes = CreateZipBytes(("random.bin", content));
        var zipPath = WriteZipFile("normalratio.zip", zipBytes);
        var destination = CreateTempPath("out_normalratio");

        var validator = new global::ZipValidator { MaxCompressionRatio = 100 };
        validator.ExtractSafe(zipPath, destination);

        Assert.True(File.Exists(Path.Combine(destination, "random.bin")));
    }

    #endregion

    #region Zip Slip

    [Fact]
    public void ExtractSafe_ZipSlipAttack_Throws()
    {
        // Manually craft a zip with a path traversal entry
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("../../../evil.txt");
            using var entryStream = entry.Open();
            entryStream.Write("malicious"u8);
        }

        ms.Position = 0;
        var zipPath = WriteZipFile("zipslip.zip", ms.ToArray());
        var destination = CreateTempPath("out_zipslip");

        var validator = new global::ZipValidator();
        var ex = Assert.Throws<InvalidDataException>(() => validator.ExtractSafe(zipPath, destination));
        Assert.Contains("Zip Slip attack detected", ex.Message);
    }

    #endregion

    #region Extraction size limit during write

    [Fact]
    public void ExtractSafe_ExtractionSizeLimitDuringWrite_Throws()
    {
        // Use random data so it won't trigger the compression ratio check
        var rng = new Random(99);
        var content = new byte[2048];
        rng.NextBytes(content);

        var zipBytes = CreateZipBytes(("big.txt", content));
        var zipPath = WriteZipFile("writelimit.zip", zipBytes);
        var destination = CreateTempPath("out_writelimit");

        var validator = new global::ZipValidator { MaxTotalUncompressedSize = 1024 };
        var ex = Assert.Throws<InvalidDataException>(() => validator.ExtractSafe(zipPath, destination));
        Assert.Contains("Total uncompressed size exceeded", ex.Message);
    }

    #endregion

    #region Subdirectory structure

    [Fact]
    public void ExtractSafe_DeepDirectoryStructure_ExtractsCorrectly()
    {
        var content = "nested content"u8.ToArray();
        var zipBytes = CreateZipBytes(("a/b/c/d/file.txt", content));
        var zipPath = WriteZipFile("deepdir.zip", zipBytes);
        var destination = CreateTempPath("out_deepdir");

        var validator = new global::ZipValidator();
        validator.ExtractSafe(zipPath, destination);

        var expectedPath = Path.Combine(destination, "a", "b", "c", "d", "file.txt");
        Assert.True(File.Exists(expectedPath));
        Assert.Equal(content, File.ReadAllBytes(expectedPath));
    }

    #endregion

    #region Custom configuration

    [Fact]
    public void ExtractSafe_LargeMaxFileCount_AcceptsMany()
    {
        var entries = new (string, byte[])[100];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = ($"file{i:D4}.txt", "y"u8.ToArray());
        }

        var zipBytes = CreateZipBytes(entries);
        var zipPath = WriteZipFile("many.zip", zipBytes);
        var destination = CreateTempPath("out_many");

        var validator = new global::ZipValidator { MaxFileCount = 100 };
        validator.ExtractSafe(zipPath, destination);

        for (int i = 0; i < 100; i++)
        {
            Assert.True(File.Exists(Path.Combine(destination, $"file{i:D4}.txt")));
        }
    }

    #endregion

    #region Binary content

    [Fact]
    public void ExtractSafe_BinaryContent_ExtractsCorrectly()
    {
        var rng = new Random(123);
        var binaryContent = new byte[4096];
        rng.NextBytes(binaryContent);

        var zipBytes = CreateZipBytes(("data.bin", binaryContent));
        var zipPath = WriteZipFile("binary.zip", zipBytes);
        var destination = CreateTempPath("out_binary");

        var validator = new global::ZipValidator();
        validator.ExtractSafe(zipPath, destination);

        var extractedPath = Path.Combine(destination, "data.bin");
        Assert.True(File.Exists(extractedPath));
        Assert.Equal(binaryContent, File.ReadAllBytes(extractedPath));
    }

    #endregion

    #region Multiple nested zips at same level

    [Fact]
    public void ExtractSafe_MultipleNestedZips_ExtractsAsFiles()
    {
        var content1 = "first inner"u8.ToArray();
        var content2 = "second inner"u8.ToArray();

        var inner1 = CreateZipBytes(("file1.txt", content1));
        var inner2 = CreateZipBytes(("file2.txt", content2));
        var outer = CreateZipBytes(("a.zip", inner1), ("b.zip", inner2));

        var zipPath = WriteZipFile("multi_nested.zip", outer);
        var destination = CreateTempPath("out_multi_nested");

        var validator = new global::ZipValidator();
        validator.ExtractSafe(zipPath, destination);

        Assert.True(File.Exists(Path.Combine(destination, "a.zip")));
        Assert.True(File.Exists(Path.Combine(destination, "b.zip")));
        Assert.Equal(inner1, File.ReadAllBytes(Path.Combine(destination, "a.zip")));
        Assert.Equal(inner2, File.ReadAllBytes(Path.Combine(destination, "b.zip")));
    }

    #endregion

    #region Empty file entry

    [Fact]
    public void ExtractSafe_EmptyFileEntry_ExtractsEmptyFile()
    {
        var zipBytes = CreateZipBytes(("empty.txt", Array.Empty<byte>()));
        var zipPath = WriteZipFile("emptyfile.zip", zipBytes);
        var destination = CreateTempPath("out_emptyfile");

        var validator = new global::ZipValidator();
        validator.ExtractSafe(zipPath, destination);

        var extractedPath = Path.Combine(destination, "empty.txt");
        Assert.True(File.Exists(extractedPath));
        Assert.Empty(File.ReadAllBytes(extractedPath));
    }

    #endregion

    #region Validate - Successful validation

    [Fact]
    public void Validate_SingleFile_Succeeds()
    {
        var content = "Hello, World!"u8.ToArray();
        var zipBytes = CreateZipBytes(("hello.txt", content));
        var zipPath = WriteZipFile("validate_single.zip", zipBytes);

        var validator = new ZipValidator();
        validator.Validate(zipPath);
    }

    [Fact]
    public void Validate_MultipleFiles_Succeeds()
    {
        var entries = new (string, byte[])[]
        {
            ("file1.txt", "Content 1"u8.ToArray()),
            ("file2.txt", "Content 2"u8.ToArray()),
            ("subdir/file3.txt", "Content 3"u8.ToArray()),
        };

        var zipBytes = CreateZipBytes(entries);
        var zipPath = WriteZipFile("validate_multi.zip", zipBytes);

        var validator = new ZipValidator();
        validator.Validate(zipPath);
    }

    [Fact]
    public void Validate_EmptyZip_Succeeds()
    {
        var zipBytes = CreateZipBytes();
        var zipPath = WriteZipFile("validate_empty.zip", zipBytes);

        var validator = new ZipValidator();
        validator.Validate(zipPath);
    }

    [Fact]
    public void Validate_DirectoryEntries_Succeeds()
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            archive.CreateEntry("mydir/");
            var entry = archive.CreateEntry("mydir/file.txt");
            using var entryStream = entry.Open();
            entryStream.Write("data"u8);
        }

        ms.Position = 0;
        var zipPath = WriteZipFile("validate_dirs.zip", ms.ToArray());

        var validator = new ZipValidator();
        validator.Validate(zipPath);
    }

    [Fact]
    public void Validate_BinaryContent_Succeeds()
    {
        var rng = new Random(123);
        var binaryContent = new byte[4096];
        rng.NextBytes(binaryContent);

        var zipBytes = CreateZipBytes(("data.bin", binaryContent));
        var zipPath = WriteZipFile("validate_binary.zip", zipBytes);

        var validator = new ZipValidator();
        validator.Validate(zipPath);
    }

    [Fact]
    public void Validate_DoesNotCreateFiles()
    {
        var content = "should not be extracted"u8.ToArray();
        var zipBytes = CreateZipBytes(("test.txt", content));
        var zipPath = WriteZipFile("validate_noextract.zip", zipBytes);
        var destination = CreateTempPath("out_validate_noextract");

        var validator = new ZipValidator();
        validator.Validate(zipPath);

        Assert.False(Directory.Exists(destination));
    }

    #endregion

    #region Validate - Stream overload

    [Fact]
    public void Validate_StreamOverload_Succeeds()
    {
        var content = "stream content"u8.ToArray();
        var zipBytes = CreateZipBytes(("file.txt", content));

        using var stream = new MemoryStream(zipBytes);
        var validator = new ZipValidator();
        validator.Validate(stream);
    }

    [Fact]
    public void Validate_StreamOverload_TooManyFiles_Throws()
    {
        var entries = new (string, byte[])[6];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = ($"file{i}.txt", "x"u8.ToArray());
        }

        var zipBytes = CreateZipBytes(entries);

        using var stream = new MemoryStream(zipBytes);
        var validator = new ZipValidator { MaxFileCount = 5 };
        var ex = Assert.Throws<InvalidDataException>(() => validator.Validate(stream));
        Assert.Contains("Too many files", ex.Message);
    }

    #endregion

    #region Validate - Nested zip

    [Fact]
    public void Validate_NestedZip_Succeeds()
    {
        var innerContent = "inner file content"u8.ToArray();
        var innerZipBytes = CreateZipBytes(("inner.txt", innerContent));
        var outerZipBytes = CreateZipBytes(("nested.zip", innerZipBytes));

        var zipPath = WriteZipFile("validate_nested.zip", outerZipBytes);

        var validator = new ZipValidator();
        validator.Validate(zipPath);
    }

    [Fact]
    public void Validate_MultipleNestedZips_Succeeds()
    {
        var content1 = "first inner"u8.ToArray();
        var content2 = "second inner"u8.ToArray();

        var inner1 = CreateZipBytes(("file1.txt", content1));
        var inner2 = CreateZipBytes(("file2.txt", content2));
        var outer = CreateZipBytes(("a.zip", inner1), ("b.zip", inner2));

        var zipPath = WriteZipFile("validate_multi_nested.zip", outer);

        var validator = new ZipValidator();
        validator.Validate(zipPath);
    }

    #endregion

    #region Validate - MaxFileCount

    [Fact]
    public void Validate_TooManyFiles_Throws()
    {
        var entries = new (string, byte[])[6];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = ($"file{i}.txt", "x"u8.ToArray());
        }

        var zipBytes = CreateZipBytes(entries);
        var zipPath = WriteZipFile("validate_toomany.zip", zipBytes);

        var validator = new ZipValidator { MaxFileCount = 5 };
        var ex = Assert.Throws<InvalidDataException>(() => validator.Validate(zipPath));
        Assert.Contains("Too many files", ex.Message);
    }

    [Fact]
    public void Validate_ExactMaxFileCount_Succeeds()
    {
        var entries = new (string, byte[])[5];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = ($"file{i}.txt", "x"u8.ToArray());
        }

        var zipBytes = CreateZipBytes(entries);
        var zipPath = WriteZipFile("validate_exactcount.zip", zipBytes);

        var validator = new ZipValidator { MaxFileCount = 5 };
        validator.Validate(zipPath);
    }

    #endregion

    #region Validate - MaxTotalUncompressedSize

    [Fact]
    public void Validate_TotalUncompressedSizeExceeded_Throws()
    {
        var largeContent = new byte[1024];
        Array.Fill(largeContent, (byte)'A');

        var zipBytes = CreateZipBytes(("large.txt", largeContent));
        var zipPath = WriteZipFile("validate_toolarge.zip", zipBytes);

        var validator = new ZipValidator { MaxTotalUncompressedSize = 512 };
        var ex = Assert.Throws<InvalidDataException>(() => validator.Validate(zipPath));
        Assert.Contains("Total uncompressed size exceeded", ex.Message);
    }

    [Fact]
    public void Validate_MultipleFilesExceedTotalSize_Throws()
    {
        var content = new byte[300];
        Array.Fill(content, (byte)'B');

        var zipBytes = CreateZipBytes(
            ("a.txt", content),
            ("b.txt", content)
        );
        var zipPath = WriteZipFile("validate_combined_large.zip", zipBytes);

        var validator = new ZipValidator { MaxTotalUncompressedSize = 500 };
        var ex = Assert.Throws<InvalidDataException>(() => validator.Validate(zipPath));
        Assert.Contains("Total uncompressed size exceeded", ex.Message);
    }

    [Fact]
    public void Validate_ExactMaxTotalSize_Succeeds()
    {
        var content = new byte[256];
        Array.Fill(content, (byte)'C');

        var zipBytes = CreateZipBytes(("exact.txt", content));
        var zipPath = WriteZipFile("validate_exactsize.zip", zipBytes);

        var validator = new ZipValidator { MaxTotalUncompressedSize = 256 };
        validator.Validate(zipPath);
    }

    #endregion

    #region Validate - Compression ratio

    [Fact]
    public void Validate_SuspiciousCompressionRatio_Throws()
    {
        var content = new byte[10000];
        Array.Fill(content, (byte)0);

        var zipBytes = CreateZipBytes(("zeros.bin", content));
        var zipPath = WriteZipFile("validate_highratio.zip", zipBytes);

        var validator = new ZipValidator { MaxCompressionRatio = 2 };
        var ex = Assert.Throws<InvalidDataException>(() => validator.Validate(zipPath));
        Assert.Contains("Suspicious compression ratio detected", ex.Message);
    }

    [Fact]
    public void Validate_NormalCompressionRatio_Succeeds()
    {
        var rng = new Random(42);
        var content = new byte[1000];
        rng.NextBytes(content);

        var zipBytes = CreateZipBytes(("random.bin", content));
        var zipPath = WriteZipFile("validate_normalratio.zip", zipBytes);

        var validator = new ZipValidator { MaxCompressionRatio = 100 };
        validator.Validate(zipPath);
    }

    #endregion

    #region Validate - Zip Slip

    [Fact]
    public void Validate_ZipSlipAttack_Throws()
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("../../../evil.txt");
            using var entryStream = entry.Open();
            entryStream.Write("malicious"u8);
        }

        ms.Position = 0;
        var zipPath = WriteZipFile("validate_zipslip.zip", ms.ToArray());

        var validator = new ZipValidator();
        var ex = Assert.Throws<InvalidDataException>(() => validator.Validate(zipPath));
        Assert.Contains("Zip Slip attack detected", ex.Message);
    }

    #endregion

    #region Validate - Empty file entry

    [Fact]
    public void Validate_EmptyFileEntry_Succeeds()
    {
        var zipBytes = CreateZipBytes(("empty.txt", Array.Empty<byte>()));
        var zipPath = WriteZipFile("validate_emptyfile.zip", zipBytes);

        var validator = new ZipValidator();
        validator.Validate(zipPath);
    }

    #endregion

    #region Validate - Deep directory structure

    [Fact]
    public void Validate_DeepDirectoryStructure_Succeeds()
    {
        var content = "nested content"u8.ToArray();
        var zipBytes = CreateZipBytes(("a/b/c/d/file.txt", content));
        var zipPath = WriteZipFile("validate_deepdir.zip", zipBytes);

        var validator = new ZipValidator();
        validator.Validate(zipPath);
    }

    #endregion
}
