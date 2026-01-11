// ============================================================================
// HoverPortal - Drag & Drop Behavior
// 遵循 dev-rules-1: 使用 Attached Behavior 模式，与 View 解耦
// ============================================================================

using System;
using System.Windows;
using System.Windows.Input;
using HoverPortal.Models;

namespace HoverPortal.Behaviors;

/// <summary>
/// 文件拖拽行为
/// 允许从预览窗口拖出文件到其他应用程序
/// </summary>
public static class FileDragBehavior
{
    // ===== 附加属性 =====
    public static readonly DependencyProperty EnableDragProperty =
        DependencyProperty.RegisterAttached(
            "EnableDrag",
            typeof(bool),
            typeof(FileDragBehavior),
            new PropertyMetadata(false, OnEnableDragChanged));
    
    public static readonly DependencyProperty DragDataProperty =
        DependencyProperty.RegisterAttached(
            "DragData",
            typeof(object),
            typeof(FileDragBehavior),
            new PropertyMetadata(null));
    
    // ===== Getter/Setter =====
    public static bool GetEnableDrag(DependencyObject obj) => (bool)obj.GetValue(EnableDragProperty);
    public static void SetEnableDrag(DependencyObject obj, bool value) => obj.SetValue(EnableDragProperty, value);
    
    public static object GetDragData(DependencyObject obj) => obj.GetValue(DragDataProperty);
    public static void SetDragData(DependencyObject obj, object value) => obj.SetValue(DragDataProperty, value);
    
    // ===== 拖拽状态 =====
    private static Point _startPoint;
    private static bool _isDragging;
    
    private static void OnEnableDragChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;
        
        if ((bool)e.NewValue)
        {
            element.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            element.PreviewMouseMove += OnPreviewMouseMove;
            element.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
        }
        else
        {
            element.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            element.PreviewMouseMove -= OnPreviewMouseMove;
            element.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
        }
    }
    
    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
        _isDragging = false;
    }
    
    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging)
            return;
        
        var position = e.GetPosition(null);
        var diff = _startPoint - position;
        
        // 检查是否超过拖拽阈值
        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            _isDragging = true;
            
            if (sender is DependencyObject element)
            {
                var dragData = GetDragData(element);
                
                if (dragData is FileItem fileItem)
                {
                    StartFileDrag(fileItem);
                }
            }
            
            _isDragging = false;
        }
    }
    
    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
    }
    
    /// <summary>
    /// 开始文件拖拽操作
    /// </summary>
    private static void StartFileDrag(FileItem fileItem)
    {
        if (string.IsNullOrEmpty(fileItem.FullPath))
            return;
        
        try
        {
            // 创建包含文件路径的 DataObject
            var dataObject = new DataObject();
            
            // 添加文件列表 (Windows Shell 格式)
            var files = new string[] { fileItem.FullPath };
            dataObject.SetData(DataFormats.FileDrop, files);
            
            // 执行拖拽操作
            DragDrop.DoDragDrop(
                Application.Current.MainWindow, // 使用任意 UI 元素作为源
                dataObject,
                DragDropEffects.Copy | DragDropEffects.Move
            );
        }
        catch (Exception)
        {
            // 拖拽操作可能因各种原因失败，静默处理
        }
    }
}
