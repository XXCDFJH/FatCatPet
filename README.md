# FatCat 桌宠（FatCatPet）   AI率 95%

Windows 虚拟桌面宠物，基于 **WPF + .NET 9**，使用精灵图帧动画。透明无边框置顶窗口，不占任务栏、不抢焦点。

## 功能

- **行为状态机**：待机 → 随机选择 走动 / 睡觉 → 循环
- **点击反应**：单击播放一次性反应动画，结束回到原状态
- **拖拽移动**：按住拖动宠物到任意位置（超过 6px 判定为拖拽）
- **自动翻转**：向右走时水平镜像（精灵图默认朝左）
- **系统托盘**：显示/隐藏、鼠标穿透开关、退出
- **窗口特性**：置顶、透明、Alt+Tab 隐藏、点击不抢焦点、单实例运行

## 项目文件说明

| 文件 | 说明 |
|---|---|
| `FatCatPet.csproj` | 工程配置：net9.0-windows、WPF+WinForms、发布时精简语言资源 |
| `App.xaml` / `App.xaml.cs` | 程序入口：单实例 Mutex、托盘图标与菜单 |
| `MainWindow.xaml` / `MainWindow.xaml.cs` | 透明无边框置顶窗口、拖拽/点击判定、帧更新 |
| `SpriteSheetPlayer.cs` | 精灵图播放器：读 JSON 元数据逐帧播放 |
| `PetEngine.cs` | 行为状态机、平滑移动、朝向翻转 |
| `Win32Interop.cs` | P/Invoke：Alt+Tab 隐藏、鼠标穿透 |
| `Assets/pet_sheet.png` | 精灵图（6400×792，每行一个动画，帧 256×198） |
| `Assets/pet_sheet.json` | 动画元数据（行号/帧数/帧率/是否循环） |
| `启动FatCat桌宠.bat` | 双击启动（无 exe 时自动先构建） |
| `发布版/` | 自包含发布产物，拷到其他电脑即可运行 |

## 动画清单

| 动画名 | 帧数 | 帧率 | 循环 |
|---|---|---|---|
| FatCat_Stay | 25 | 10 | 是 |
| FatCat_Walk | 13 | 12 | 是 |
| FatCat_Sleep | 25 | 10 | 是 |
| FatCat_Click | 9 | 10 | 否 |

## 构建与发布

```powershell
# 开发构建（输出到 bin\Debug）
dotnet build

# 自包含发布（输出到 bin\Release\net9.0-windows\win-x64\publish\，
# 整文件夹拷贝到任意 Win10/11 x64 电脑直接运行）
dotnet publish -c Release -r win-x64 --self-contained true
```

## 常用调整（PetEngine.cs）

| 调整项 | 位置 |
|---|---|
| 走路速度（默认 4px/帧 ≈ 120px/s） | `MoveStep` |
| 睡觉时长（默认 20~40 秒） | 睡觉分支的 `Schedule(20, 40, ...)` |
| 走路/睡觉概率（默认 60%/40%） | 待机分支的 `0.6` |
| 素材朝向（默认朝左） | `SpriteFacesLeft` |

## 已知技术要点

- WPF 的 `CroppedBitmap` 创建后修改 `SourceRect` **不生效**，帧动画必须每帧新建实例（本项目已验证）
- `UseWindowsForms=true` 会引入全局 using 与 WPF 类型冲突（`Point`/`MouseEventArgs`），需显式别名
