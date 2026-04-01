// Assets/Script/GamePlay/TArmorArchitecture.cs
using QFramework;
using QFramework.Utility;
using QFramework.Model;
// using QFramework.System;
public class TArmorArchitecture : Architecture<TArmorArchitecture>
{
    protected override void Init()
    {

        // 模型注册
        RegisterModel<IPlayerModel>(new PlayerModel());
        RegisterModel<IEnemeyDataModel>(new EnemeyDataModel());

        // 系统注册
        

        // 工具注册
        RegisterUtility<IObjectPoolUtility>(new ObjectPool());
    }
}