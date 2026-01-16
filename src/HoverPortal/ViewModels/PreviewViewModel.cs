// ============================================================================
// HoverPortal - Preview Window ViewModel
// 遵循 dev-rules-1: MVVM 架构，View 与 Logic 严格分离
// ============================================================================

using System;
using System.Collections.Generic;
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
    
    [ObservableProperty]
    private bool _canGoBack;
    
    /// <summary>
    /// 导航历史栈
    /// </summary>
    private readonly Stack<string> _navigationHistory = new();
    
    /// <summary>
    /// 初始目录路径（悬浮窗打开时的目录）
    /// </summary>
    private string? _initialFolderPath;
    
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
        
        // 记录初始目录（仅在首次加载时）
        if (_initialFolderPath == null)
        {
            _initialFolderPath = path;
        }
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
        catch (UnauthorizedAccessException ex)
        {
            // 没有访问权限
            System.Diagnostics.Debug.WriteLine($"[PreviewViewModel] Unauthorized access: {ex.Message}");
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PreviewViewModel] Error loading folder: {ex.Message}");
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
                // 导航到子文件夹
                NavigateToFolder(item.FullPath);
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
    
    /// <summary>
    /// 导航到指定文件夹（保存历史）
    /// </summary>
    public void NavigateToFolder(string path)
    {
        // 保存当前路径到历史栈
        if (!string.IsNullOrEmpty(FolderPath))
        {
            _navigationHistory.Push(FolderPath);
        }
        
        // 加载新文件夹
        LoadFolder(path);
        
        // 更新返回按钮状态
        CanGoBack = _navigationHistory.Count > 0;
    }
    
    /// <summary>
    /// 返回上级目录
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        if (_navigationHistory.Count == 0) return;
        
        var previousPath = _navigationHistory.Pop();
        LoadFolder(previousPath);
        CanGoBack = _navigationHistory.Count > 0;
    }
    
    /// <summary>
    /// 导航到父文件夹（文件系统层级）
    /// 注意：在第一级目录（初始目录）时不允许返回到更上层
    /// </summary>
    [RelayCommand]
    private void NavigateToParent()
    {
        if (string.IsNullOrEmpty(FolderPath)) return;
        
        // 如果当前是初始目录，则不允许右键返回到更上层
        if (string.Equals(FolderPath, _initialFolderPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        
        var parentDir = Directory.GetParent(FolderPath);
        if (parentDir != null && parentDir.Exists)
        {
            NavigateToFolder(parentDir.FullName);
            
            // 如果已返回到初始目录，清除导航历史使返回按钮消失
            if (string.Equals(FolderPath, _initialFolderPath, StringComparison.OrdinalIgnoreCase))
            {
                _navigationHistory.Clear();
                CanGoBack = false;
            }
        }
    }
    
    /// <summary>
    /// 清除导航历史（窗口隐藏时调用）
    /// </summary>
    public void ClearNavigationHistory()
    {
        _navigationHistory.Clear();
        _initialFolderPath = null;
        CanGoBack = false;
    }
}
