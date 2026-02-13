// ============================================================================
// HoverPortal.Tests - PreviewViewModel 单元测试
// 覆盖: LoadFolder, NavigateToFolder, GoBack, NavigateToParent,
//       ClearNavigationHistory, 边界情况
// ============================================================================

using FluentAssertions;
using HoverPortal.ViewModels;
using Xunit;

namespace HoverPortal.Tests.ViewModels;

public class PreviewViewModelTests : IDisposable
{
    private readonly string _testRoot;
    private readonly PreviewViewModel _vm;

    public PreviewViewModelTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), $"HoverPortal_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRoot);
        _vm = new PreviewViewModel();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    // ========== LoadFolder ==========

    [Fact]
    public void LoadFolder_EmptyDirectory_SetsIsEmpty()
    {
        var emptyDir = Path.Combine(_testRoot, "empty");
        Directory.CreateDirectory(emptyDir);

        _vm.LoadFolder(emptyDir);

        _vm.IsEmpty.Should().BeTrue();
        _vm.Files.Should().BeEmpty();
    }

    [Fact]
    public void LoadFolder_WithFiles_PopulatesFiles()
    {
        CreateFiles(_testRoot, "a.txt", "b.doc", "c.pdf");

        _vm.LoadFolder(_testRoot);

        _vm.IsEmpty.Should().BeFalse();
        _vm.Files.Should().HaveCount(3);
    }

    [Fact]
    public void LoadFolder_WithSubdirectories_IncludesDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_testRoot, "SubDir1"));
        Directory.CreateDirectory(Path.Combine(_testRoot, "SubDir2"));
        CreateFiles(_testRoot, "file.txt");

        _vm.LoadFolder(_testRoot);

        _vm.Files.Should().HaveCount(3);
        _vm.Files.Count(f => f.IsDirectory).Should().Be(2);
        _vm.Files.Count(f => !f.IsDirectory).Should().Be(1);
    }

    [Fact]
    public void LoadFolder_NonExistentPath_SetsIsEmpty()
    {
        var fakePath = Path.Combine(_testRoot, "does_not_exist");

        _vm.LoadFolder(fakePath);

        _vm.IsEmpty.Should().BeTrue();
        _vm.Files.Should().BeEmpty();
    }

    [Fact]
    public void LoadFolder_MaxItems_CappedAt20()
    {
        // Create 25 files — should only show 20
        for (int i = 0; i < 25; i++)
        {
            File.WriteAllText(Path.Combine(_testRoot, $"file_{i:D3}.txt"), "");
        }

        _vm.LoadFolder(_testRoot);

        _vm.Files.Should().HaveCount(20);
    }

    [Fact]
    public void LoadFolder_SetsFolderNameAndPath()
    {
        var subDir = Path.Combine(_testRoot, "MyFolder");
        Directory.CreateDirectory(subDir);

        _vm.LoadFolder(subDir);

        _vm.FolderPath.Should().Be(subDir);
        _vm.FolderName.Should().Be("MyFolder");
    }

    [Fact]
    public void LoadFolder_ClearsPreviousFiles()
    {
        CreateFiles(_testRoot, "a.txt", "b.txt");
        _vm.LoadFolder(_testRoot);
        _vm.Files.Should().HaveCount(2);

        // Create a new dir with only 1 file
        var otherDir = Path.Combine(_testRoot, "other");
        Directory.CreateDirectory(otherDir);
        CreateFiles(otherDir, "c.txt");

        _vm.LoadFolder(otherDir);
        _vm.Files.Should().HaveCount(1);
    }

    // ========== NavigateToFolder + CanGoBack ==========

    [Fact]
    public void NavigateToFolder_PushesHistory_CanGoBackTrue()
    {
        var dirA = CreateSubDir("A");
        var dirB = CreateSubDir("B");

        _vm.LoadFolder(dirA);
        _vm.NavigateToFolder(dirB);

        _vm.CanGoBack.Should().BeTrue();
        _vm.FolderPath.Should().Be(dirB);
    }

    [Fact]
    public void NavigateToFolder_MultipleNavigations_AllPushed()
    {
        var dirs = new[] { "A", "B", "C" }.Select(CreateSubDir).ToArray();

        _vm.LoadFolder(dirs[0]);
        _vm.NavigateToFolder(dirs[1]);
        _vm.NavigateToFolder(dirs[2]);

        _vm.FolderPath.Should().Be(dirs[2]);
        _vm.CanGoBack.Should().BeTrue();
    }

    // ========== GoBack ==========

    [Fact]
    public void GoBack_PopsHistory_NavigatesToPrevious()
    {
        var dirA = CreateSubDir("A");
        var dirB = CreateSubDir("B");

        _vm.LoadFolder(dirA);
        _vm.NavigateToFolder(dirB);

        // Use reflection to invoke private GoBack (it's a RelayCommand)
        InvokeGoBack();

        _vm.FolderPath.Should().Be(dirA);
    }

    [Fact]
    public void GoBack_EmptyHistory_NoOp()
    {
        var dirA = CreateSubDir("A");
        _vm.LoadFolder(dirA);

        // GoBack with no history should not crash
        InvokeGoBack();

        _vm.FolderPath.Should().Be(dirA);
        _vm.CanGoBack.Should().BeFalse();
    }

    [Fact]
    public void GoBack_LastItem_CanGoBackBecomesFalse()
    {
        var dirA = CreateSubDir("A");
        var dirB = CreateSubDir("B");

        _vm.LoadFolder(dirA);
        _vm.NavigateToFolder(dirB);

        _vm.CanGoBack.Should().BeTrue();

        InvokeGoBack();

        _vm.CanGoBack.Should().BeFalse();
    }

    [Fact]
    public void GoBack_MultipleSteps_UnwindsCorrectly()
    {
        var dirs = new[] { "A", "B", "C", "D" }.Select(CreateSubDir).ToArray();

        _vm.LoadFolder(dirs[0]);
        _vm.NavigateToFolder(dirs[1]);
        _vm.NavigateToFolder(dirs[2]);
        _vm.NavigateToFolder(dirs[3]);

        InvokeGoBack(); // D -> C
        _vm.FolderPath.Should().Be(dirs[2]);

        InvokeGoBack(); // C -> B
        _vm.FolderPath.Should().Be(dirs[1]);

        InvokeGoBack(); // B -> A
        _vm.FolderPath.Should().Be(dirs[0]);
        _vm.CanGoBack.Should().BeFalse();
    }

    // ========== NavigateToParent ==========

    [Fact]
    public void NavigateToParent_AtInitialDirectory_NoOp()
    {
        var dirA = CreateSubDir("A");
        _vm.LoadFolder(dirA);

        InvokeNavigateToParent();

        _vm.FolderPath.Should().Be(dirA, "should not navigate above initial directory");
    }

    [Fact]
    public void NavigateToParent_AfterNavigation_GoesUp()
    {
        var dirA = CreateSubDir("A");
        var dirB = Path.Combine(dirA, "B");
        Directory.CreateDirectory(dirB);

        _vm.LoadFolder(dirA);
        _vm.NavigateToFolder(dirB);

        InvokeNavigateToParent();

        // Should navigate to parent of B, which is A
        _vm.FolderPath.Should().Be(dirA);
    }

    // ========== ClearNavigationHistory ==========

    [Fact]
    public void ClearNavigationHistory_ResetsAllState()
    {
        var dirs = new[] { "X", "Y" }.Select(CreateSubDir).ToArray();

        _vm.LoadFolder(dirs[0]);
        _vm.NavigateToFolder(dirs[1]);
        _vm.CanGoBack.Should().BeTrue();

        _vm.ClearNavigationHistory();

        _vm.CanGoBack.Should().BeFalse();
    }

    [Fact]
    public void ClearNavigationHistory_AllowsNewInitialFolder()
    {
        var dirA = CreateSubDir("A");
        var dirB = CreateSubDir("B");

        _vm.LoadFolder(dirA);
        _vm.ClearNavigationHistory();

        // After clearing, loading B becomes the new initial
        _vm.LoadFolder(dirB);

        // Should not be able to go above new initial
        InvokeNavigateToParent();
        _vm.FolderPath.Should().Be(dirB);
    }

    // ========== Helpers ==========

    private string CreateSubDir(string name)
    {
        var dir = Path.Combine(_testRoot, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CreateFiles(string dir, params string[] fileNames)
    {
        foreach (var name in fileNames)
        {
            File.WriteAllText(Path.Combine(dir, name), "test content");
        }
    }

    /// <summary>
    /// Invoke the private GoBack method via the GoBackCommand
    /// </summary>
    private void InvokeGoBack()
    {
        _vm.GoBackCommand.Execute(null);
    }

    /// <summary>
    /// Invoke the private NavigateToParent method via the NavigateToParentCommand
    /// </summary>
    private void InvokeNavigateToParent()
    {
        _vm.NavigateToParentCommand.Execute(null);
    }
}
