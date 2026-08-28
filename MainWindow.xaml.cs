using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
// WinForms 全局 using 会与 WPF 类型冲突，显式指定别名
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace FatCatPet;

/// <summary>
/// 透明无边框宠物窗口：负责显示动画帧、拖拽移动、点击互动。
/// </summary>
public partial class MainWindow : Window
{
    private const double DragThreshold = 6.0;

    private PetEngine? _engine;
    private Point _pressPoint;
    private bool _isDragging;
    private bool _suppressClick;

    public MainWindow()
    {
        InitializeComponent();

        string baseDir = AppContext.BaseDirectory;
        var player = new SpriteSheetPlayer(
            Path.Combine(baseDir, "Assets", "pet_sheet.png"),
            Path.Combine(baseDir, "Assets", "pet_sheet.json"));

        // 窗口尺寸 = 精灵图帧尺寸（1:1 显示）
        Width = player.FrameWidth;
        Height = player.FrameHeight;
        PetImage.Source = player.CurrentFrame;
        player.FrameChanged += frame => PetImage.Source = frame;

        _engine = new PetEngine(this, player);

        Loaded += (_, _) =>
        {
            // 初始位置：工作区右下角
            var wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - 60;
            Top = wa.Bottom - Height - 60;
            Win32Interop.HideFromAltTab(this);
            _engine.Start();
        };

        Closed += (_, _) => _engine.Dispose();
    }

    /// <summary>水平镜像翻转（走路方向与精灵图朝向相反时使用）。</summary>
    public void SetFlip(bool flip)
        => ((ScaleTransform)PetImage.RenderTransform).ScaleX = flip ? -1 : 1;

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pressPoint = e.GetPosition(this);
        _isDragging = false;
        _suppressClick = false;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isDragging) return;
        var p = e.GetPosition(this);
        if (Math.Abs(p.X - _pressPoint.X) > DragThreshold ||
            Math.Abs(p.Y - _pressPoint.Y) > DragThreshold)
        {
            // 超过阈值判定为拖拽
            _isDragging = true;
            _suppressClick = true;
            _engine?.OnDragStarted();
            try { DragMove(); }        // 阻塞直到鼠标松开
            catch (InvalidOperationException) { }
            _isDragging = false;
            _engine?.OnDragEnded();
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_suppressClick) return;    // 拖拽结束不算点击
        _engine?.OnPetClicked();
    }
}
