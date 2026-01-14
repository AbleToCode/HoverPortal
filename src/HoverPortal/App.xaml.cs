// ============================================================================
// HoverPortal - Application Entry Point
// 遵循 dev-rules-1: 处理命令行参数以支持静默启动
// ============================================================================

using System;
using System.Linq;
using System.Windows;
using HoverPortal.Services;

namespace HoverPortal
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 是否为静默启动模式 (开机自启动)
        /// </summary>
        public static bool IsSilentStartup { get; private set; }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 检查是否为开机自启动 (--startup 参数)
            IsSilentStartup = e.Args.Contains("--startup", StringComparer.OrdinalIgnoreCase);
            
            System.Diagnostics.Debug.WriteLine($"[App] Starting with IsSilentStartup={IsSilentStartup}");
            
            // 同步设置和注册表状态
            SyncStartupSettings();
            
            // 创建并显示主窗口
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            
            if (IsSilentStartup)
            {
                // 静默启动: 不显示窗口，直接启动服务到托盘
                mainWindow.StartSilently();
            }
            else
            {
                // 正常启动: 显示主窗口
                mainWindow.Show();
            }
        }
        
        /// <summary>
        /// 同步设置和注册表状态
        /// 确保设置文件和注册表的一致性
        /// </summary>
        private async void SyncStartupSettings()
        {
            try
            {
                var settings = await SettingsService.Instance.LoadAsync();
                StartupManager.SyncWithSettings(settings.LaunchAtStartup);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Failed to sync startup settings: {ex.Message}");
            }
        }
    }
}
