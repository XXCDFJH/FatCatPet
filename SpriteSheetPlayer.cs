using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FatCatPet;

/// <summary>单个动画的元数据（对应 JSON 中 animations 的每一项）。</summary>
public sealed class AnimMeta
{
    public int Row { get; set; }
    public int Frames { get; set; }
    public int Fps { get; set; }
    public bool Loop { get; set; }
}

/// <summary>精灵图元数据文件（pet_sheet.json）的结构。</summary>
public sealed class SpriteSheetData
{
    public string Image { get; set; } = "";
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public Dictionary<string, AnimMeta> Animations { get; set; } = new();
}

/// <summary>
/// 精灵图播放器：读取 pet_sheet.json，按动画名逐帧播放。
/// 帧间通过复用 CroppedBitmap 的 SourceRect 实现，避免每帧创建新对象。
/// </summary>
public sealed class SpriteSheetPlayer : IDisposable
{
    private readonly SpriteSheetData _data;
    private readonly BitmapImage _sheet;
    private readonly DispatcherTimer _timer;

    private AnimMeta? _currentAnim;
    private string? _currentAnimName;
    private int _frameIndex;
    private BitmapSource _currentFrame;

    /// <summary>一次性动画（loop=false）播放完毕时触发，参数为动画名。</summary>
    public event Action<string>? AnimationFinished;

    /// <summary>每切换一帧触发一次，参数为当前帧图像（需每帧新建，SourceRect 修改不生效）。</summary>
    public event Action<BitmapSource>? FrameChanged;

    public SpriteSheetPlayer(string pngPath, string jsonPath)
    {
        _data = JsonSerializer.Deserialize<SpriteSheetData>(
                    File.ReadAllText(jsonPath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException($"精灵图元数据加载失败: {jsonPath}");

        _sheet = new BitmapImage();
        _sheet.BeginInit();
        _sheet.CacheOption = BitmapCacheOption.OnLoad;   // 立即解码并释放文件锁
        _sheet.UriSource = new Uri(Path.GetFullPath(pngPath));
        _sheet.EndInit();
        _sheet.Freeze();

        _currentFrame = new CroppedBitmap(_sheet, new Int32Rect(0, 0, _data.FrameWidth, _data.FrameHeight));

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += OnTick;
    }

    public int FrameWidth => _data.FrameWidth;
    public int FrameHeight => _data.FrameHeight;
    public BitmapSource CurrentFrame => _currentFrame;

    /// <summary>播放指定动画；forceRestart=true 时即使已在播该动画也从头开始。</summary>
    public void Play(string name, bool forceRestart = false)
    {
        if (!_data.Animations.TryGetValue(name, out var anim))
            throw new KeyNotFoundException($"未知动画: {name}");

        if (_currentAnimName == name && !forceRestart)
            return;

        _currentAnimName = name;
        _currentAnim = anim;
        _frameIndex = 0;
        _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / anim.Fps);
        _timer.Start();
        ShowFrame(0);
    }

    public void Dispose() => _timer.Stop();

    private void OnTick(object? sender, EventArgs e)
    {
        if (_currentAnim is null) return;

        _frameIndex++;
        if (_frameIndex >= _currentAnim.Frames)
        {
            if (_currentAnim.Loop)
            {
                _frameIndex = 0;
            }
            else
            {
                // 一次性动画：停在最后一帧
                _frameIndex = _currentAnim.Frames - 1;
                _timer.Stop();
                var name = _currentAnimName;
                AnimationFinished?.Invoke(name!);
                return;
            }
        }
        ShowFrame(_frameIndex);
    }

    private void ShowFrame(int index)
    {
        // 注意：不能复用 CroppedBitmap 并修改 SourceRect（实测修改不生效），必须每帧新建
        _currentFrame = new CroppedBitmap(_sheet, new Int32Rect(
            index * _data.FrameWidth,
            _currentAnim!.Row * _data.FrameHeight,
            _data.FrameWidth,
            _data.FrameHeight));
        FrameChanged?.Invoke(_currentFrame);
    }
}
