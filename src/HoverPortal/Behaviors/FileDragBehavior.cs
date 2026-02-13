// ============================================================================
// HoverPortal - Drag & Drop Behavior
// 遵循 dev-rules-1: 使用 Attached Behavior 模式，与 View 解耦
// 修复 C-3: 使用 per-element 附加属性替代静态字段，消除多实例竞争
// ============================================================================

using System;
using System.Windows;
using System.Windows.Input;
using HoverPortal.Models;

namespace HoverPortal.Behaviors;

/// <summary>
/// 文件拖拽行为
/// 允许从预览窗口拖出文件到其他应用程序
/// 每个元素独立追踪拖拽状态，避免多实例竞争
/// </summary>
public static class FileDragBehavior
{
    // ===== 公开附加属性 =====
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
    
    // ===== 私有 per-element 拖拽状态附加属性 =====
    private static readonly DependencyProperty DragStartPointProperty =
        DependencyProperty.RegisterAttached(
            "DragStartPoint",
            typeof(Point),
            typeof(FileDragBehavior),
            new PropertyMetadata(default(Point)));
    
    private static readonly DependencyProperty IsDraggingProperty =
        DependencyProperty.RegisterAttached(
            "IsDragging",
            typeof(bool),
            typeof(FileDragBehavior),
            new PropertyMetadata(false));
    
    // ===== 公开 Getter/Setter =====
    public static bool GetEnableDrag(DependencyObject obj) => (bool)obj.GetValue(EnableDragProperty);
    public static void SetEnableDrag(DependencyObject obj, bool value) => obj.SetValue(EnableDragProperty, value);
    
    public static object GetDragData(DependencyObject obj) => obj.GetValue(DragDataProperty);
    public static void SetDragData(DependencyObject obj, object value) => obj.SetValue(DragDataProperty, value);
    
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
        if (sender is not DependencyObject element) return;
        element.SetValue(DragStartPointProperty, e.GetPosition(null));
        element.SetValue(IsDraggingProperty, false);
    }
    
    private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not DependencyObject element) return;
        
        bool isDragging = (bool)element.GetValue(IsDraggingProperty);
        if (e.LeftButton != MouseButtonState.Pressed || isDragging)
            return;
        
        var startPoint = (Point)element.GetValue(DragStartPointProperty);
        var position = e.GetPosition(null);
        var diff = startPoint - position;
        
        // 检查是否超过拖拽阈值
        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            element.SetValue(IsDraggingProperty, true);
            
            var dragData = GetDragData(element);
            
            if (dragData is FileItem fileItem)
            {
                StartFileDrag(fileItem);
            }
            
            element.SetValue(IsDraggingProperty, false);
        }
    }
    
    private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DependencyObject element) return;
        element.SetValue(IsDraggingProperty, false);
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
