using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace FatCatPet;

/// <summary>
/// 程序入口：单实例检测、创建宠物窗口、初始化系统托盘。
/// </summary>
public partial class App : Application
{
    private const string MutexName = "FatCatPet_SingleInstance";

    private Mutex? _mutex;
    private NotifyIcon? _tray;
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 单实例：已有实例在运行则直接退出
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        _window = new MainWindow();
        // 用户关不掉窗口（没有关闭按钮），但若窗口被关闭则隐藏而不是退出
        _window.Closing += (_, args) =>
        {
            args.Cancel = true;
            _window.Hide();
        };

        InitTray();
        _window.Show();
    }

    private void InitTray()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("显示 / 隐藏宠物", null, (_, _) => TogglePet());
        menu.Items.Add(new ToolStripSeparator());

        var clickThroughItem = new ToolStripMenuItem("鼠标穿透") { CheckOnClick = true };
        clickThroughItem.CheckedChanged += (_, _) =>
            Win32Interop.SetClickThrough(_window!, clickThroughItem.Checked);
        menu.Items.Add(clickThroughItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) =>
        {
            _tray!.Visible = false;
            Shutdown();
        });

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "FatCat 桌宠",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => TogglePet();
    }

    private void TogglePet()
    {
        if (_window is null) return;
        if (_window.IsVisible) _window.Hide();
        else _window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_tray is not null) _tray.Visible = false;
        _tray?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
