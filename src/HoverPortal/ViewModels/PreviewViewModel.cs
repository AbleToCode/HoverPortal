// ============================================================================
// HoverPortal - Preview Window ViewModel
// 遵循 dev-rules-1: MVVM 架构，View 与 Logic 严格分离
// ============================================================================

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HoverPortal.Models;

namespace HoverPortal.ViewModels;

/// <summary>
/// 预览窗口 ViewModel
/// </summary>
public partial class PreviewViewModel : ObservableObject
{
    private const int MaxDisplayItems = 20; // 最多显示的文件数量
    
    [ObservableProperty]
    private string _folderName = string.Empty;
    
    [ObservableProperty]
    private string _folderPath = string.Empty;
    
    [ObservableProperty]
    private bool _isEmpty = true;
    
    /// <summary>
    /// 文件列表
    /// </summary>
    public ObservableCollection<FileItem> Files { get; } = new();
    
    /// <summary>
    /// 加载指定文件夹的内容
    /// </summary>
    public void LoadFolder(string path)
    {
        Files.Clear();
        FolderPath = path;
        FolderName = Path.GetFileName(path);
        
        if (string.IsNullOrEmpty(FolderName))
        {
            FolderName = path; // 可能是驱动器根目录
        }
        
        try
        {
            if (!Directory.Exists(path))
            {
                IsEmpty = true;
                return;
            }
            
            // 获取文件夹内容 (先目录后文件，按名称排序)
            var items = Directory.EnumerateDirectories(path)
                .Select(FileItem.FromPath)
                .Concat(Directory.EnumerateFiles(path).Select(FileItem.FromPath))
                .Take(MaxDisplayItems)
                .ToList();
            
            foreach (var item in items)
            {
                Files.Add(item);
            }
            
            IsEmpty = Files.Count == 0;
        }
        catch (UnauthorizedAccessException)
        {
            // 没有访问权限
            IsEmpty = true;
        }
        catch (Exception)
        {
            IsEmpty = true;
        }
    }
    
    /// <summary>
    /// 打开文件或文件夹
    /// </summary>
    [RelayCommand]
    private void OpenFile(FileItem? item)
    {
        if (item == null) return;
        
        try
        {
            if (item.IsDirectory)
            {
                // 打开资源管理器
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.FullPath,
                    UseShellExecute = true
                });
            }
            else
            {
                // 使用默认程序打开文件
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.FullPath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open file: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 在资源管理器中打开文件夹
    /// </summary>
    [RelayCommand]
    private void OpenInExplorer()
    {
        if (string.IsNullOrEmpty(FolderPath)) return;
        
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = FolderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open explorer: {ex.Message}");
        }
    }
}
