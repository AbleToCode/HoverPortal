// ============================================================================
// HoverPortal - File Item Model
// ============================================================================

using System.IO;
using System.Windows.Media;
using HoverPortal.Services;

namespace HoverPortal.Models;

/// <summary>
/// 文件/文件夹项模型
/// </summary>
public class FileItem
{
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public bool IsDirectory { get; init; }
    public string Extension { get; init; } = string.Empty;
    
    // ===== 缓存的图标 =====
    private ImageSource? _cachedIcon;
    private bool _iconLoaded;
    
    /// <summary>
    /// 获取系统文件图标 (带懒加载)
    /// </summary>
    public ImageSource? Icon
    {
        get
        {
            if (!_iconLoaded)
            {
                _cachedIcon = IconExtractor.GetIcon(FullPath, IsDirectory, largeIcon: true);
                _iconLoaded = true;
            }
            return _cachedIcon;
        }
    }
    
    /// <summary>
    /// 获取文件图标 Emoji (备用方案)
    /// </summary>
    public string IconEmoji => GetIconEmoji();
    
    private string GetIconEmoji()
    {
        if (IsDirectory)
            return "📁";
        
        return Extension.ToLowerInvariant() switch
        {
            ".txt" or ".md" or ".log" => "📄",
            ".doc" or ".docx" => "📝",
            ".xls" or ".xlsx" => "📊",
            ".ppt" or ".pptx" => "📽️",
            ".pdf" => "📕",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "🖼️",
            ".mp3" or ".wav" or ".flac" or ".m4a" => "🎵",
            ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" => "🎬",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "📦",
            ".exe" or ".msi" => "⚙️",
            ".dll" or ".sys" => "🔧",
            ".cs" or ".js" or ".ts" or ".py" or ".java" or ".cpp" or ".h" => "💻",
            ".html" or ".htm" or ".css" => "🌐",
            ".json" or ".xml" or ".yaml" or ".yml" => "📋",
            ".sql" or ".db" => "🗃️",
            ".psd" or ".ai" or ".sketch" => "🎨",
            ".lnk" => "🔗",
            _ => "📄"
        };
    }
    
    /// <summary>
    /// 从文件系统路径创建 FileItem
    /// </summary>
    public static FileItem FromPath(string path)
    {
        var isDir = Directory.Exists(path);
        var name = Path.GetFileName(path);
        
        return new FileItem
        {
            Name = string.IsNullOrEmpty(name) ? path : name,
            FullPath = path,
            IsDirectory = isDir,
            Extension = isDir ? string.Empty : Path.GetExtension(path)
        };
    }
}
