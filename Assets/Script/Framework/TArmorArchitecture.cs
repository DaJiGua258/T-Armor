// Assets/Script/GamePlay/TArmorArchitecture.cs
using QFramework;
using QFramework.Utility;
using QFramework.Model;        
using QFramework.System;
public class TArmorArchitecture : Architecture<TArmorArchitecture>
{
    protected override void Init()
    {
 
        // 模型注册
        RegisterModel<IPlayerModel>(new PlayerModel());
        RegisterModel<IWeaponConfigModel>(new WeaponConfigModel());
        RegisterModel<IEnemeyConfigModel>(new EnemeyConfigModel());
        RegisterModel<IItemConfigModel>(new ItemConfigModel());
        RegisterModel<ILevelTypeModel>(new LevelTypeModel());
        RegisterModel<IMissionConfigModel>(new MissionConfigModel());

        // 系统注册
        RegisterSystem<IEnemyInstanceSystem>(new EnemyInstanceSystem());
        RegisterSystem<IWeaponInstanceSystem>(new WeaponInstanceSystem());
        RegisterSystem<IPickUpItemInstanceSystem>(new PickUpItemInstanceSystem());
        RegisterSystem<IInvenotrySystem>(new InvenotrySystem());
        RegisterSystem<IMissionSystem>(new MissionSystem());

        // 玩家系统（引用武器，改装槽系统）
        RegisterSystem<IPlayerSystem>(new PlayerSystem());
        RegisterSystem<ILevelSystem>(new LevelSystem());

        // 统计数据系统
        RegisterSystem<IStatsSystem>(new StatsSystem());
        
        // 工具注册
        RegisterUtility<IResourceLoad>(new ResouceLoad());
        RegisterUtility<ITimerUtility>(new TimerUtility());
        RegisterUtility<IObjectPoolUtility>(new ObjectPool());
        RegisterUtility<IInputUtility>(new InputUtility());
        RegisterUtility<IDebugUtility>(new DebugUtility());
    }
}