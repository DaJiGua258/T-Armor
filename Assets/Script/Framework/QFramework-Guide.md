# QFramework 使用规范文档

> 适用版本：QFramework v1.0 · Unity 2022.3+  
> 本文档基于 `Assets/Script/Framework/QFramework.cs` 源码及 T-Armor 项目实际结构编写。

---

## 目录

1. [架构总览](#1-架构总览)
2. [Architecture 注册规范](#2-architecture-注册规范)
3. [各层实现模板](#3-各层实现模板)
4. [Command 使用规范](#4-command-使用规范)
5. [Query 使用规范](#5-query-使用规范)
6. [BindableProperty 使用规范](#6-bindableproperty-使用规范)
7. [事件系统使用规范](#7-事件系统使用规范)
8. [层级通信规则与禁止事项](#8-层级通信规则与禁止事项)
9. [结合项目代码的改造示例](#9-结合项目代码的改造示例)

---

## 1. 架构总览

### 1.1 四层架构图

```
┌─────────────────────────────────────────────────────────────┐
│  表现层  IController  (MonoBehaviour)                        │
│  职责：接收用户输入、驱动 UI / 表现，响应状态变化             │
│  ✔ GetSystem   ✔ GetModel   ✔ SendCommand                   │
│  ✔ RegisterEvent   ✔ SendQuery                              │
└───────────────────┬─────────────────────────────────────────┘
                    │ SendCommand ↓           ↑ RegisterEvent
┌───────────────────▼─────────────────────────────────────────┐
│  系统层  ISystem                                             │
│  职责：多个表现层共享的业务逻辑（计时、成就、战斗判定等）      │
│  ✔ GetSystem   ✔ GetModel   ✔ RegisterEvent                 │
│  ✔ SendEvent                                                │
└───────────────────┬─────────────────────────────────────────┘
                    │ GetModel ↓              ↑ SendEvent
┌───────────────────▼─────────────────────────────────────────┐
│  数据层  IModel                                              │
│  职责：数据定义、增删查改方法                                 │
│  ✔ GetUtility   ✔ SendEvent                                 │
└───────────────────┬─────────────────────────────────────────┘
                    │ GetUtility ↓
┌───────────────────▼─────────────────────────────────────────┐
│  工具层  IUtility                                            │
│  职责：基础设施（存储、序列化、网络、SDK 封装等）             │
│  ✗ 不能调用任何其他层                                        │
└─────────────────────────────────────────────────────────────┘

                    Command（横切关注点）
                    ✔ GetSystem   ✔ GetModel
                    ✔ SendEvent   ✔ SendCommand
```

### 1.2 层级权限对照表

| 能力 | IController | ISystem | IModel | IUtility | ICommand | IQuery |
|------|:-----------:|:-------:|:------:|:--------:|:--------:|:------:|
| GetSystem | ✔ | ✔ | ✗ | ✗ | ✔ | ✔ |
| GetModel | ✔ | ✔ | ✗ | ✗ | ✔ | ✔ |
| GetUtility | ✔ | ✔ | ✔ | ✗ | ✔ | ✗ |
| SendCommand | ✔ | ✗ | ✗ | ✗ | ✔ | ✗ |
| SendEvent | ✗ | ✔ | ✔ | ✗ | ✔ | ✗ |
| RegisterEvent | ✔ | ✔ | ✗ | ✗ | ✗ | ✗ |
| SendQuery | ✔ | ✗ | ✗ | ✗ | ✔ | ✔ |

> `IController` 的 `SendEvent` 通道被刻意关闭——Controller 需要改变状态时只能走 Command，Command 内部再 SendEvent。

---

## 2. Architecture 注册规范

### 2.1 创建项目 Architecture

每个游戏/场景有且仅有一个 `Architecture` 子类。在 `Init()` 中完成所有注册，**顺序固定：先 Model，再 System，最后 Utility**（框架在 `InitArchitecture` 中按此顺序调用 `Init()`）。

```csharp
using QFramework;

public class GameArchitecture : Architecture<GameArchitecture>
{
    protected override void Init()
    {
        // 1. 先注册 Model
        RegisterModel(new PlayerModel());
        RegisterModel(new EnemyModel());

        // 2. 再注册 System
        RegisterSystem(new ScoreSystem());
        RegisterSystem(new TimerSystem());

        // 3. 最后注册 Utility
        RegisterUtility(new JsonStorageUtility());
    }
}
```

### 2.2 访问 Architecture 入口

```csharp
// 通过静态属性访问（首次访问时自动初始化）
IArchitecture arch = GameArchitecture.Interface;
```

### 2.3 Architecture 销毁

场景卸载或游戏结束时主动调用，确保 Model/System 的 `OnDeinit()` 被执行：

```csharp
private void OnDestroy()
{
    GameArchitecture.Interface.Deinit();
}
```

---

## 3. 各层实现模板

### 3.1 IModel — 数据层

```csharp
using QFramework;

public class PlayerModel : AbstractModel
{
    // 对外暴露 BindableProperty，Controller/System 可订阅变化
    public BindableProperty<int> HP { get; } = new BindableProperty<int>(100);
    public BindableProperty<int> Ammo { get; } = new BindableProperty<int>(30);

    protected override void OnInit()
    {
        // 从 Utility 加载持久化数据（如有）
        // var storage = this.GetUtility<IStorageUtility>();
        // HP.SetValueWithoutEvent(storage.Load<int>("HP", 100));
    }

    protected override void OnDeinit()
    {
        // 持久化保存
    }
}
```

**规则：**
- 所有可变数据尽量使用 `BindableProperty<T>` 包装，避免外部直接赋值。
- Model 只提供数据读写方法，**不包含任何业务判断逻辑**。
- 需要保存/读取数据时，通过 `this.GetUtility<T>()` 获取工具。

---

### 3.2 ISystem — 系统层

```csharp
using QFramework;

public class ScoreSystem : AbstractSystem
{
    private int _score;

    protected override void OnInit()
    {
        // 监听事件，响应来自 Command 或其他 System 的通知
        this.RegisterEvent<EnemyKilledEvent>(OnEnemyKilled);
    }

    private void OnEnemyKilled(EnemyKilledEvent e)
    {
        _score += e.RewardScore;
        // 向上层通知（Controller 会监听此事件刷新 UI）
        this.SendEvent(new ScoreChangedEvent { Score = _score });
    }

    protected override void OnDeinit()
    {
        _score = 0;
    }
}
```

**规则：**
- System 不能持有任何 `IController` 的引用，向上层通信只通过 `SendEvent`。
- System 可以调用 `GetModel<T>()` 读取数据，但**不能直接修改 Model 的属性**（应由 Command 完成）。
- System 之间可以互相 `GetSystem<T>()` 但要警惕循环依赖。

---

### 3.3 IController — 表现层

```csharp
using QFramework;
using UnityEngine;

public class PlayerHUD : MonoBehaviour, IController
{
    // 必须实现：告知框架该 Controller 归属哪个 Architecture
    public IArchitecture GetArchitecture() => GameArchitecture.Interface;

    private void Start()
    {
        // 订阅 BindableProperty，立即触发一次以初始化显示
        this.GetModel<PlayerModel>().HP
            .RegisterWithInitValue(OnHPChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        // 订阅架构事件
        this.RegisterEvent<ScoreChangedEvent>(OnScoreChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnHPChanged(int hp)
    {
        // 更新 HP 显示
    }

    private void OnScoreChanged(ScoreChangedEvent e)
    {
        // 更新分数显示
    }

    // 玩家操作触发 Command，而不是直接修改数据
    public void OnFireButtonClick()
    {
        this.SendCommand<ShootCommand>();
    }
}
```

**规则：**
- 每个实现 `IController` 的 MonoBehaviour **必须实现** `GetArchitecture()` 方法。
- 所有改变状态的操作（射击、移动、扣血）均通过 `SendCommand` 发起。
- 监听事件后必须在合适的生命周期节点**解除注册**（见第 7 节）。
- Controller 可以调用 `GetModel/GetSystem` **只做数据查询**，不做状态写入。

---

### 3.4 IUtility — 工具层

```csharp
using QFramework;

// 定义接口（推荐，便于替换实现）
public interface IStorageUtility : IUtility
{
    void Save<T>(string key, T value);
    T Load<T>(string key, T defaultValue);
}

// 具体实现
public class PlayerPrefsStorageUtility : IStorageUtility
{
    public void Save<T>(string key, T value)
    {
        PlayerPrefs.SetString(key, JsonUtility.ToJson(value));
    }

    public T Load<T>(string key, T defaultValue)
    {
        if (!PlayerPrefs.HasKey(key)) return defaultValue;
        return JsonUtility.FromJson<T>(PlayerPrefs.GetString(key));
    }
}
```

**规则：**
- Utility 只实现 `IUtility` 接口，**无需继承任何基类**。
- Utility 不能调用 GetSystem/GetModel/GetUtility，也不能 SendEvent/SendCommand。
- 可以集成第三方库，封装 SDK（如广告、分析、本地存储）。

---

## 4. Command 使用规范

### 4.1 无返回值 Command

```csharp
using QFramework;

public class ShootCommand : AbstractCommand
{
    // 禁止在 Command 中持有成员状态字段
    // private int count; // ✗ 错误示范

    protected override void OnExecute()
    {
        var playerModel = this.GetModel<PlayerModel>();

        // 前置条件判断
        if (playerModel.Ammo.Value <= 0) return;

        // 修改数据
        playerModel.Ammo.Value--;

        // 通知上层
        this.SendEvent<PlayerShootEvent>();
    }
}
```

### 4.2 有返回值 Command

```csharp
public class TryPickupCommand : AbstractCommand<bool>
{
    public int ItemId;

    protected override bool OnExecute()
    {
        var inventoryModel = this.GetModel<InventoryModel>();
        if (inventoryModel.IsFull) return false;

        inventoryModel.AddItem(ItemId);
        this.SendEvent(new ItemPickedUpEvent { ItemId = ItemId });
        return true;
    }
}

// 调用方（Controller 中）：
bool success = this.SendCommand(new TryPickupCommand { ItemId = 101 });
```

### 4.3 发送方式

```csharp
// 无参数 Command（需要有无参构造函数）
this.SendCommand<ShootCommand>();

// 有参数 Command（通过属性赋值传参）
this.SendCommand(new MoveCommand { Direction = Vector2.right });

// 有返回值 Command
int result = this.SendCommand(new CalcDamageCommand { BaseDamage = 10 });
```

**硬性规则：**
- `ICommand` **不能持有成员状态**（字段）——Command 是一次性执行单元，数据应通过构造/属性传入。
- `IController` 改变任何 ISystem 或 IModel 的状态，**必须且只能用 Command**。
- `ISystem` 不能发送 Command（权限未开放）。

---

## 5. Query 使用规范

Query 用于**跨多个 Model/System 的复合只读查询**，返回计算结果而不改变任何状态。

```csharp
using QFramework;

// 查询：玩家当前 DPS（需要从多个 Model 计算）
public class PlayerDpsQuery : AbstractQuery<float>
{
    protected override float OnDo()
    {
        var playerModel = this.GetModel<PlayerModel>();
        var weaponModel = this.GetModel<WeaponModel>();

        return weaponModel.Damage.Value * playerModel.AttackSpeedMultiplier.Value;
    }
}

// 调用方（Controller 中）：
float dps = this.SendQuery(new PlayerDpsQuery());
```

**使用时机：**

| 场景 | 推荐方式 |
|------|----------|
| 读取单个 Model 的单个属性 | `this.GetModel<T>().Property.Value` |
| 读取单个 Model 的计算属性 | 在 Model 中提供方法，直接调用 |
| 跨多个 Model/System 的组合计算 | `this.SendQuery(new XxxQuery())` |

---

## 6. BindableProperty 使用规范

### 6.1 声明与初始化

```csharp
// 在 Model 中声明
public BindableProperty<int> HP { get; } = new BindableProperty<int>(100);
public BindableProperty<string> PlayerName { get; } = new BindableProperty<string>("Player");
```

### 6.2 订阅变化

```csharp
// 仅监听后续变化（不立即触发）
model.HP.Register(hp => Debug.Log($"HP changed: {hp}"))
    .UnRegisterWhenGameObjectDestroyed(gameObject);

// 立即触发一次当前值，再监听后续变化（推荐用于 UI 初始化）
model.HP.RegisterWithInitValue(hp => hpText.text = hp.ToString())
    .UnRegisterWhenGameObjectDestroyed(gameObject);
```

### 6.3 修改值

```csharp
// 在 Command 中修改（触发事件）
model.HP.Value = 80;

// 静默修改（不触发监听回调，适用于数据初始化）
model.HP.SetValueWithoutEvent(100);
```

### 6.4 自定义比较器

默认使用 `.Equals()` 判断值是否变化。对于自定义类型，提供比较器避免误触发：

```csharp
public BindableProperty<Vector3> Position { get; } =
    new BindableProperty<Vector3>(Vector3.zero)
        .WithComparer((a, b) => (a - b).sqrMagnitude < 0.0001f);
```

> 常见基础类型（int、float、Vector2/3 等）已在框架启动时自动注册 `==` 比较器，无需手动配置。

---

## 7. 事件系统使用规范

### 7.1 定义事件

事件使用 `struct`（值类型），字段只读：

```csharp
// 无数据事件（用 struct + 无字段）
public struct PlayerDeadEvent { }

// 有数据事件
public struct EnemyKilledEvent
{
    public int EnemyId;
    public int RewardScore;
}
```

### 7.2 发送事件

```csharp
// 无数据事件（在 Command / System / Model 中）
this.SendEvent<PlayerDeadEvent>();

// 有数据事件
this.SendEvent(new EnemyKilledEvent { EnemyId = 5, RewardScore = 100 });
```

### 7.3 订阅与解除

```csharp
// 在 Controller 或 System 的 Start/OnInit 中订阅
this.RegisterEvent<EnemyKilledEvent>(OnEnemyKilled)
    .UnRegisterWhenGameObjectDestroyed(gameObject);  // GameObject 销毁时自动解除

// 其他解除时机
.UnRegisterWhenDisabled(this);                        // OnDisable 时解除
.UnRegisterWhenCurrentSceneUnloaded();               // 场景卸载时解除
```

### 7.4 解除时机对照表

| 解除方式 | 适用场景 |
|---------|---------|
| `UnRegisterWhenGameObjectDestroyed(go)` | 大多数 MonoBehaviour，生命周期与 GameObject 一致 |
| `UnRegisterWhenDisabled(component)` | 需要在隐藏时停止响应、显示时重新订阅的 UI 组件 |
| `UnRegisterWhenCurrentSceneUnloaded()` | 非 MonoBehaviour 的全局监听，随场景卸载清理 |
| 手动 `UnRegisterEvent<T>(handler)` | 精确控制解除时机的特殊场景 |

### 7.5 OrEvent（多事件合并）

当同一回调需要由多个事件触发时使用：

```csharp
model.HP.Or(model.Ammo)
    .Register(() => RefreshUI())
    .UnRegisterWhenGameObjectDestroyed(gameObject);
```

---

## 8. 层级通信规则与禁止事项

### 8.1 核心通信规则

```
上层获取下层  →  直接调用 GetSystem<T>() / GetModel<T>()（只读查询）
上层改变下层  →  必须通过 SendCommand<T>()
下层通知上层  →  SendEvent<T>() 或 BindableProperty 变化回调
```

### 8.2 禁止事项速查

| 场景 | 错误做法 | 正确做法 |
|------|---------|---------|
| Controller 减少玩家 HP | `playerModel.HP.Value -= 10;` ✗ | `this.SendCommand(new TakeDamageCommand{Damage=10});` ✔ |
| System 更新 UI | 持有 UI 组件引用并调用 ✗ | `this.SendEvent(new UIUpdateEvent{...});` ✔ |
| System 发送 Command | `this.SendCommand<...>()` ✗ | System 只能 SendEvent，需要状态变更则在 Command 中处理 ✔ |
| Model 调用 GetSystem | `this.GetSystem<ScoreSystem>()` ✗ | Model 只持有数据，通过 SendEvent 通知 ✔ |
| Model 调用 GetModel | `this.GetModel<OtherModel>()` ✗ | Model 间不互相引用，通过 Command 统一协调 ✔ |
| Command 持有成员字段 | `private int _count;` ✗ | 通过构造时赋值的公开属性传参 ✔ |
| IUtility 调用框架方法 | `this.GetModel<T>()` ✗ | Utility 是纯粹的基础设施，不依赖框架 ✔ |

### 8.3 层级依赖方向

```
IController  →  ISystem  →  IModel  →  IUtility
    ↑___________↑___________↑
              Event / BindableProperty（反向通知，不反向引用）
```

**核心原则：下层永远不持有上层的引用。**

---

## 9. 结合项目代码的改造示例

> 以下示例基于 T-Armor 现有的 `PlayerController` / `Weapon` / `Bullet` 脚本，演示如何逐步接入 QFramework。

### 9.1 第一步：创建 Architecture

```csharp
// Assets/Script/GamePlay/TArmorArchitecture.cs
using QFramework;

public class TArmorArchitecture : Architecture<TArmorArchitecture>
{
    protected override void Init()
    {
        RegisterModel(new PlayerModel());
        RegisterSystem(new CombatSystem());
    }
}
```

### 9.2 第二步：创建 PlayerModel

```csharp
// Assets/Script/GamePlay/PlayerModel.cs
using QFramework;

public class PlayerModel : AbstractModel
{
    public BindableProperty<int> HP { get; } = new BindableProperty<int>(100);
    public BindableProperty<int> AmmoLeft { get; } = new BindableProperty<int>(30);
    public BindableProperty<int> AmmoRight { get; } = new BindableProperty<int>(30);

    protected override void OnInit() { }
}
```

### 9.3 第三步：定义事件与 Command

```csharp
// Assets/Script/GamePlay/GameEvents.cs
public struct PlayerShootLeftEvent { }
public struct PlayerShootRightEvent { }
public struct PlayerTakeDamageEvent { public int Damage; }
```

```csharp
// Assets/Script/GamePlay/Commands/ShootLeftCommand.cs
using QFramework;

public class ShootLeftCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var model = this.GetModel<PlayerModel>();
        if (model.AmmoLeft.Value <= 0) return;

        model.AmmoLeft.Value--;
        this.SendEvent<PlayerShootLeftEvent>();
    }
}
```

### 9.4 第四步：改造 PlayerController

```csharp
// 改造前（直接调用 Weapon.Shoot()）
if (Input.GetMouseButton(0))
{
    _weapon.WeaponLeft.Shoot();
}

// 改造后（通过 Command 触发，Controller 实现 IController）
public class PlayerController : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

    private void SetInput()
    {
        if (Input.GetMouseButton(0))
            this.SendCommand<ShootLeftCommand>();

        if (Input.GetMouseButton(1))
            this.SendCommand<ShootRightCommand>();
    }
}
```

### 9.5 第五步：CombatSystem 响应射击事件并驱动实际逻辑

```csharp
// Assets/Script/GamePlay/CombatSystem.cs
using QFramework;

public class CombatSystem : AbstractSystem
{
    // 持有对 Weapon 的引用（由场景注入或查找）
    // System 负责跨 Controller 共享的射击判断逻辑

    protected override void OnInit()
    {
        this.RegisterEvent<PlayerShootLeftEvent>(_ => DoShootLeft());
        this.RegisterEvent<PlayerShootRightEvent>(_ => DoShootRight());
    }

    private void DoShootLeft()
    {
        // 实际调用子弹生成逻辑（可引用 ObjectPool 等 Utility）
        this.SendEvent(new BulletFiredEvent { Side = WeaponSide.Left });
    }

    private void DoShootRight()
    {
        this.SendEvent(new BulletFiredEvent { Side = WeaponSide.Right });
    }
}
```

### 9.6 第六步：UI Controller 监听 HP / Ammo 变化

```csharp
public class PlayerHUD : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

    [SerializeField] private Text _hpText;
    [SerializeField] private Text _ammoText;

    private void Start()
    {
        this.GetModel<PlayerModel>().HP
            .RegisterWithInitValue(hp => _hpText.text = $"HP: {hp}")
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        this.GetModel<PlayerModel>().AmmoLeft
            .RegisterWithInitValue(ammo => _ammoText.text = $"弹药: {ammo}")
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }
}
```

---

## 附录：接口与基类速查

| 类型 | 继承/实现 | 必须实现的方法 |
|------|----------|--------------|
| Model | `AbstractModel` | `OnInit()` |
| System | `AbstractSystem` | `OnInit()` |
| Controller | `MonoBehaviour` + `IController` | `GetArchitecture()` |
| Utility | `IUtility`（直接实现接口） | 无框架要求 |
| Command（无返回）| `AbstractCommand` | `OnExecute()` |
| Command（有返回）| `AbstractCommand<TResult>` | `OnExecute()` → 返回 `TResult` |
| Query | `AbstractQuery<TResult>` | `OnDo()` → 返回 `TResult` |
| Architecture | `Architecture<T>`（单例） | `Init()` |

---

*文档生成日期：2026-03-31*
