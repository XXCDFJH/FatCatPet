using System.Windows;
using System.Windows.Threading;

namespace FatCatPet;

public enum PetState { Stay, Walk, Sleep, Clicked }

/// <summary>
/// 宠物行为状态机：
/// 待机(Stay) → 随机决定 走动(Walk) 或 睡觉(Sleep) → 回到待机；
/// 点击时播放 Click 一次性动画，结束后回到点击前的状态。
/// </summary>
public sealed class PetEngine : IDisposable
{
    // 动画名必须与 pet_sheet.json 中的键一致
    public const string AnimStay = "FatCat_Stay";
    public const string AnimWalk = "FatCat_Walk";
    public const string AnimSleep = "FatCat_Sleep";
    public const string AnimClick = "FatCat_Click";

    /// <summary>精灵图默认朝左。若实际素材朝右，改为 false 即可。</summary>
    public const bool SpriteFacesLeft = true;

    private const double MoveStep = 4.0;        // 每帧移动像素（30fps ≈ 120px/s）
    private const double ScreenMargin = 8.0;

    private readonly MainWindow _window;
    private readonly SpriteSheetPlayer _player;
    private readonly DispatcherTimer _moveTimer;
    private readonly DispatcherTimer _stateTimer;
    private readonly Random _rng = new();

    private PetState _state = PetState.Stay;
    private PetState _stateBeforeClick = PetState.Stay;
    private Action? _onTimeout;
    private double _targetX, _targetY;
    private bool _dragging;

    public PetEngine(MainWindow window, SpriteSheetPlayer player)
    {
        _window = window;
        _player = player;

        _moveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000.0 / 30) };
        _moveTimer.Tick += OnMoveTick;

        _stateTimer = new DispatcherTimer();
        _stateTimer.Tick += (_, _) =>
        {
            _stateTimer.Stop();
            _onTimeout?.Invoke();
        };

        _player.AnimationFinished += OnAnimationFinished;
    }

    public void Start() => EnterState(PetState.Stay);

    /// <summary>用户点击宠物：播放反应动画，结束后回到之前状态。</summary>
    public void OnPetClicked()
    {
        if (_state == PetState.Clicked || _dragging) return;
        _stateBeforeClick = _state;
        _stateTimer.Stop();
        _moveTimer.Stop();
        EnterState(PetState.Clicked);
    }

    public void OnDragStarted()
    {
        _dragging = true;
        _stateTimer.Stop();
        _moveTimer.Stop();
    }

    public void OnDragEnded()
    {
        _dragging = false;
        EnterState(PetState.Stay);
    }

    private void EnterState(PetState state)
    {
        _state = state;
        _moveTimer.Stop();

        switch (state)
        {
            case PetState.Stay:
                _player.Play(AnimStay);
                // 3~7 秒后随机决定下一步动作
                Schedule(3, 7, () =>
                    EnterState(_rng.NextDouble() < 0.6 ? PetState.Walk : PetState.Sleep));
                break;

            case PetState.Walk:
                _player.Play(AnimWalk);
                PickWalkTarget();
                _moveTimer.Start();
                Schedule(12, 20, () => EnterState(PetState.Stay)); // 兜底：超时停下
                break;

            case PetState.Sleep:
                _player.Play(AnimSleep);
                Schedule(20, 40, () => EnterState(PetState.Stay)); // 睡 20~40 秒
                break;

            case PetState.Clicked:
                _player.Play(AnimClick, forceRestart: true);
                break; // 播放完毕由 AnimationFinished 事件接回
        }
    }

    /// <summary>在 [minSeconds, maxSeconds] 随机时长后执行一次动作。</summary>
    private void Schedule(double minSeconds, double maxSeconds, Action action)
    {
        _onTimeout = action;
        _stateTimer.Stop();
        _stateTimer.Interval =
            TimeSpan.FromSeconds(_rng.NextDouble() * (maxSeconds - minSeconds) + minSeconds);
        _stateTimer.Start();
    }

    /// <summary>在屏幕工作区内随机选一个目标点，并更新朝向。</summary>
    private void PickWalkTarget()
    {
        var wa = SystemParameters.WorkArea;
        double minX = wa.Left + ScreenMargin;
        double minY = wa.Top + ScreenMargin;
        double maxX = Math.Max(minX, wa.Right - _window.Width - ScreenMargin);
        double maxY = Math.Max(minY, wa.Bottom - _window.Height - ScreenMargin);
        _targetX = minX + _rng.NextDouble() * (maxX - minX);
        _targetY = minY + _rng.NextDouble() * (maxY - minY);

        bool movingRight = _targetX > _window.Left;
        _window.SetFlip(movingRight == SpriteFacesLeft);
    }

    private void OnMoveTick(object? sender, EventArgs e)
    {
        double dx = _targetX - _window.Left;
        double dy = _targetY - _window.Top;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist <= MoveStep)
        {
            _window.Left = _targetX;
            _window.Top = _targetY;
            _moveTimer.Stop();
            EnterState(PetState.Stay);
            return;
        }

        _window.Left += dx / dist * MoveStep;
        _window.Top += dy / dist * MoveStep;
    }

    private void OnAnimationFinished(string name)
    {
        if (name == AnimClick && _state == PetState.Clicked)
            EnterState(_stateBeforeClick);
    }

    public void Dispose()
    {
        _moveTimer.Stop();
        _stateTimer.Stop();
        _player.Dispose();
    }
}
