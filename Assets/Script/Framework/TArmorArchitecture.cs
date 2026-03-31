// Assets/Script/GamePlay/TArmorArchitecture.cs
using QFramework;
using QFramework.Utility;
public class TArmorArchitecture : Architecture<TArmorArchitecture>
{
    protected override void Init()
    {
        RegisterUtility<IObjectPoolUtility>(new ObjectPool());
    }
}