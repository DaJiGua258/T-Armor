using QFramework.Enum;
using QFramework.Model;

namespace QFramework.System
{
    public interface IPlayerSystem : ISystem
    {
        public void InitPlayerWeapon();
        public PlayerWeapon PlayerWeapon { get; }
    }

    public class PlayerSystem : AbstractSystem, IPlayerSystem
    {   
        // 基础Model和System引用
        private IWeaponConfigModel _weaponModel => this.GetModel<IWeaponConfigModel>();
        private IWeaponInstanceSystem _weaponInstanceSystem => this.GetSystem<IWeaponInstanceSystem>();
        

        // 左右槽位武器数据（改装槽修改武器数据）
        public PlayerWeapon PlayerWeapon { get; private set; }
    
        // 被动武器数据（改装槽修改被动武器数据）


        protected override void OnInit()
        {
            PlayerWeapon = new PlayerWeapon();
        }

        public void InitPlayerWeapon()
        {   
            var weaponDataLeft = new WeaponDataModel(_weaponModel.GetWeaponConfigModel(WeaponTypeEnum.AR));
            var weaponDataRight = new WeaponDataModel(_weaponModel.GetWeaponConfigModel(WeaponTypeEnum.AR));
            PlayerWeapon.WeaponDataLeft.Value = weaponDataLeft;
            PlayerWeapon.WeaponDataRight.Value = weaponDataRight;
        }
        
    }

    public class PlayerWeapon
    {
        public BindableProperty<WeaponDataModel> WeaponDataLeft = new BindableProperty<WeaponDataModel>();
        public BindableProperty<WeaponDataModel> WeaponDataRight = new BindableProperty<WeaponDataModel>();
    
        public PlayerWeapon()
        {
            UnityEngine.Debug.Log("PlayerWeapon Constructor");
        }
    }
}