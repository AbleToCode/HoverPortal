// ============================================================================
// HoverPortal - Settings Window Code-Behind
// Phase 5: 个性化与设置
// ============================================================================

using System.Windows;
using System.Windows.Input;
using HoverPortal.ViewModels;

namespace HoverPortal.Views;

/// <summary>
/// SettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    
    public SettingsWindow()
    {
        InitializeComponent();
        
        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;
        
        // 订阅关闭事件
        _viewModel.RequestClose += () => this.Close();
        
        // 窗口加载时初始化
        Loaded += async (s, e) => await _viewModel.InitializeAsync();
    }
    
    /// <summary>
    /// 允许拖动窗口标题栏
    /// </summary>
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }
    
    /// <summary>
    /// 关闭按钮点击
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelCommand.Execute(null);
    }
}
