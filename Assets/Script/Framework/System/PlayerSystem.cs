using QFramework.Enum;
using QFramework.Model;

namespace QFramework.System
{
    public interface IPlayerSystem : ISystem
    {
        public void InitPlayerWeapon();
        public void InitHangerWeapon();
        public PlayerWeapon PlayerWeapon { get; }
    }

    public class PlayerSystem : AbstractSystem, IPlayerSystem
    {   
        // 基础Model和System引用
        private IWeaponConfigModel _weaponModel => this.GetModel<IWeaponConfigModel>();
        private IWeaponInstanceSystem _weaponInstanceSystem => this.GetSystem<IWeaponInstanceSystem>();
        

        // 左右槽位武器数据（改装槽修改武器数据）
        public PlayerWeapon PlayerWeapon { get; private set; }



        protected override void OnInit()
        {
            PlayerWeapon = new PlayerWeapon();
        }

        public void InitPlayerWeapon()
        {
            var weaponDataLeft = new WeaponDataModel(_weaponModel.GetWeaponConfigModel(WeaponTypeEnum.AR));
            var weaponDataRight = new WeaponDataModel(_weaponModel.GetWeaponConfigModel(WeaponTypeEnum.SG));
            PlayerWeapon.Left.Value = weaponDataLeft;
            PlayerWeapon.Right.Value = weaponDataRight;
        }

        public void InitHangerWeapon()
        {
            var hangerLeft = new WeaponDataModel(_weaponModel.GetHangerWeaponConfigModel(WeaponTypeEnum.MTT));
            var hangerRight = new WeaponDataModel(_weaponModel.GetHangerWeaponConfigModel(WeaponTypeEnum.VML));
            PlayerWeapon.HangerLeft.Value = hangerLeft;
            PlayerWeapon.HangerRight.Value = hangerRight;
        }
        
    }

    public class PlayerWeapon
    {
        // 这里使用BindableProperty，后续武器交换时，通过交换引用触发事件
        public BindableProperty<WeaponDataModel> Left = new BindableProperty<WeaponDataModel>();
        public BindableProperty<WeaponDataModel> Right = new BindableProperty<WeaponDataModel>();

        // 吊架武器数据
        public BindableProperty<WeaponDataModel> HangerLeft = new BindableProperty<WeaponDataModel>();
        public BindableProperty<WeaponDataModel> HangerRight = new BindableProperty<WeaponDataModel>();

        public PlayerWeapon()
        {

        }
    }
}