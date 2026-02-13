// ============================================================================
// HoverPortal.Tests - FileItem 单元测试
// 覆盖: FromPath (文件/目录/根路径), IconEmoji 映射
// ============================================================================

using FluentAssertions;
using HoverPortal.Models;
using Xunit;

namespace HoverPortal.Tests.Models;

public class FileItemTests : IDisposable
{
    private readonly string _testRoot;

    public FileItemTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), $"HoverPortal_FileItem_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    // ========== FromPath - File ==========

    [Fact]
    public void FromPath_File_SetsAllProperties()
    {
        var filePath = Path.Combine(_testRoot, "document.pdf");
        File.WriteAllText(filePath, "");

        var item = FileItem.FromPath(filePath);

        item.Name.Should().Be("document.pdf");
        item.FullPath.Should().Be(filePath);
        item.IsDirectory.Should().BeFalse();
        item.Extension.Should().Be(".pdf");
    }

    [Fact]
    public void FromPath_FileWithNoExtension_ExtensionEmpty()
    {
        var filePath = Path.Combine(_testRoot, "Makefile");
        File.WriteAllText(filePath, "");

        var item = FileItem.FromPath(filePath);

        item.Name.Should().Be("Makefile");
        item.Extension.Should().BeEmpty();
    }

    // ========== FromPath - Directory ==========

    [Fact]
    public void FromPath_Directory_SetsIsDirectory()
    {
        var dirPath = Path.Combine(_testRoot, "MyFolder");
        Directory.CreateDirectory(dirPath);

        var item = FileItem.FromPath(dirPath);

        item.Name.Should().Be("MyFolder");
        item.FullPath.Should().Be(dirPath);
        item.IsDirectory.Should().BeTrue();
        item.Extension.Should().BeEmpty();
    }

    // ========== FromPath - Edge cases ==========

    [Fact]
    public void FromPath_NonExistentFile_IsDirectoryFalse()
    {
        var fakePath = Path.Combine(_testRoot, "ghost.txt");
        // Don't create it — it neither exists as file nor directory

        var item = FileItem.FromPath(fakePath);

        item.IsDirectory.Should().BeFalse();
        item.Extension.Should().Be(".txt");
    }

    // ========== IconEmoji ==========

    [Fact]
    public void IconEmoji_Directory_ReturnsFolderEmoji()
    {
        var item = new FileItem { IsDirectory = true, Extension = "" };
        item.IconEmoji.Should().Be("📁");
    }

    [Theory]
    [InlineData(".cs", "💻")]
    [InlineData(".js", "💻")]
    [InlineData(".ts", "💻")]
    [InlineData(".py", "💻")]
    [InlineData(".java", "💻")]
    [InlineData(".cpp", "💻")]
    [InlineData(".h", "💻")]
    public void IconEmoji_CodeFiles_ReturnsCodeEmoji(string ext, string expected)
    {
        var item = new FileItem { IsDirectory = false, Extension = ext };
        item.IconEmoji.Should().Be(expected);
    }

    [Theory]
    [InlineData(".jpg", "🖼️")]
    [InlineData(".jpeg", "🖼️")]
    [InlineData(".png", "🖼️")]
    [InlineData(".gif", "🖼️")]
    [InlineData(".bmp", "🖼️")]
    [InlineData(".webp", "🖼️")]
    public void IconEmoji_ImageFiles_ReturnsImageEmoji(string ext, string expected)
    {
        var item = new FileItem { IsDirectory = false, Extension = ext };
        item.IconEmoji.Should().Be(expected);
    }

    [Theory]
    [InlineData(".txt", "📄")]
    [InlineData(".md", "📄")]
    [InlineData(".log", "📄")]
    public void IconEmoji_TextFiles_ReturnsDocEmoji(string ext, string expected)
    {
        var item = new FileItem { IsDirectory = false, Extension = ext };
        item.IconEmoji.Should().Be(expected);
    }

    [Theory]
    [InlineData(".doc", "📝")]
    [InlineData(".docx", "📝")]
    public void IconEmoji_WordFiles_ReturnsWordEmoji(string ext, string expected)
    {
        var item = new FileItem { IsDirectory = false, Extension = ext };
        item.IconEmoji.Should().Be(expected);
    }

    [Theory]
    [InlineData(".xls", "📊")]
    [InlineData(".xlsx", "📊")]
    public void IconEmoji_ExcelFiles_ReturnsExcelEmoji(string ext, string expected)
    {
        var item = new FileItem { IsDirectory = false, Extension = ext };
        item.IconEmoji.Should().Be(expected);
    }

    [Theory]
    [InlineData(".mp3", "🎵")]
    [InlineData(".wav", "🎵")]
    [InlineData(".flac", "🎵")]
    [InlineData(".m4a", "🎵")]
    public void IconEmoji_AudioFiles_ReturnsMusicEmoji(string ext, string expected)
    {
        var item = new FileItem { IsDirectory = false, Extension = ext };
        item.IconEmoji.Should().Be(expected);
    }

    [Theory]
    [InlineData(".mp4", "🎬")]
    [InlineData(".avi", "🎬")]
    [InlineData(".mkv", "🎬")]
    [InlineData(".mov", "🎬")]
    [InlineData(".wmv", "🎬")]
    public void IconEmoji_VideoFiles_ReturnsVideoEmoji(string ext, string expected)
    {
        var item = new FileItem { IsDirectory = false, Extension = ext };
        item.IconEmoji.Should().Be(expected);
    }

    [Theory]
    [InlineData(".zip", "📦")]
    [InlineData(".rar", "📦")]
    [InlineData(".7z", "📦")]
    [InlineData(".tar", "📦")]
    [InlineData(".gz", "📦")]
    public void IconEmoji_ArchiveFiles_ReturnsArchiveEmoji(string ext, string expected)
    {
        var item = new FileItem { IsDirectory = false, Extension = ext };
        item.IconEmoji.Should().Be(expected);
    }

    [Theory]
    [InlineData(".exe", "⚙️")]
    [InlineData(".msi", "⚙️")]
    public void IconEmoji_ExecutableFiles_ReturnsGearEmoji(string ext, string expected)
    {
        var item = new FileItem { IsDirectory = false, Extension = ext };
        item.IconEmoji.Should().Be(expected);
    }

    [Theory]
    [InlineData(".html", "🌐")]
    [InlineData(".htm", "🌐")]
    [InlineData(".css", "🌐")]
    public void IconEmoji_WebFiles_ReturnsWebEmoji(string ext, string expected)
    {
        var item = new FileItem { IsDirectory = false, Extension = ext };
        item.IconEmoji.Should().Be(expected);
    }

    [Fact]
    public void IconEmoji_UnknownExtension_ReturnsDefaultDocEmoji()
    {
        var item = new FileItem { IsDirectory = false, Extension = ".xyz" };
        item.IconEmoji.Should().Be("📄");
    }

    [Fact]
    public void IconEmoji_LinkFile_ReturnsLinkEmoji()
    {
        var item = new FileItem { IsDirectory = false, Extension = ".lnk" };
        item.IconEmoji.Should().Be("🔗");
    }

    [Fact]
    public void IconEmoji_PdfFile_ReturnsBookEmoji()
    {
        var item = new FileItem { IsDirectory = false, Extension = ".pdf" };
        item.IconEmoji.Should().Be("📕");
    }
}
