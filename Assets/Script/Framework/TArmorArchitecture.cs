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
        RegisterModel<IWeaponModel>(new WeaponConfigModel());
        RegisterModel<IEnemeyConfigModel>(new EnemeyConfigModel());

        // 系统注册
        RegisterSystem<IEnemyInstanceSystem>(new EnemyInstanceSystem());
        RegisterSystem<IWeaponInstanceSystem>(new WeaponInstanceSystem());

        // 玩家系统（引用武器，改装槽系统）
        RegisterSystem<IPlayerSystem>(new PlayerSystem());
        
        // 工具注册
        RegisterUtility<IResourceLoad>(new ResouceLoad());
        RegisterUtility<ITimerUtility>(new TimerUtility());
        RegisterUtility<IObjectPoolUtility>(new ObjectPool());
    }
}