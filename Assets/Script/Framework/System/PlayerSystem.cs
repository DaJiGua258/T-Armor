using System.Collections.Generic;
using QFramework.Enum;
using QFramework.Model;

namespace QFramework.System
{
    public class SupportItemDataModel
    {
        public SupportTypeEnum SupportType;
        public string name;
        public string iconPath;
        public string description;
        public bool canUse;
        public float cooldownTime;
        public BindableProperty<float> CooldownRemaining = new BindableProperty<float>(0f);

        public SupportItemDataModel() { }

        public SupportItemDataModel(SupportItemConfig config)
        {
            SupportType = config.SupportType;
            name = config.name;
            iconPath = config.iconPath;
            description = config.description;
            canUse = config.canUse;
            cooldownTime = config.cooldownTime;
            CooldownRemaining.Value = 0f;
        }
    }

    public interface IPlayerSystem : ISystem
    {
        public void InitPlayerWeapon();
        public void InitHangerWeapon();
        public PlayerWeapon PlayerWeapon { get; }
        public List<ItemDataModel> PlayerMods { get; }
        public List<SupportItemDataModel> SupportItems { get; }
        public SupportItemDataModel GetSupportItemByIndex(int index);
        public void InitSupportItems(List<SupportTypeEnum> itemTypes);
    }

    public class PlayerSystem : AbstractSystem, IPlayerSystem
    {
        // 基础Model和System引用
        private IWeaponConfigModel _weaponModel => this.GetModel<IWeaponConfigModel>();
        private ISupportConfigModel _supportConfigModel => this.GetModel<ISupportConfigModel>();

        // 左右槽位武器数据（改装槽修改武器数据）
        public PlayerWeapon PlayerWeapon { get; private set; }

        // 玩家机体 Mod 槽位（5 个，用于血量/速度等属性强化）
        public List<ItemDataModel> PlayerMods { get; private set; } = new();

        // 支援物品槽位（3 个）
        public List<SupportItemDataModel> SupportItems { get; private set; } = new();

        protected override void OnInit()
        {
            PlayerWeapon = new PlayerWeapon();
            // 初始化 5 个空 Player Mod 槽位
            for (int i = 0; i < 5; i++)
                PlayerMods.Add(new ItemDataModel());

            // 初始化 3 个空支援槽位
            for (int i = 0; i < 3; i++)
                SupportItems.Add(new SupportItemDataModel());

            // 给第一个槽位一个默认的炮击（后续可通过主菜单配置覆盖）
            var defaultConfig = _supportConfigModel.GetSupportConfig(SupportTypeEnum.Artillery);
            if (defaultConfig != null)
                SupportItems[0] = new SupportItemDataModel(defaultConfig);
        }

        public void InitPlayerWeapon()
        {
            var weaponDataLeft = new WeaponDataModel(_weaponModel.GetWeaponConfigModel(WeaponTypeEnum.LMG));
            var weaponDataRight = new WeaponDataModel(_weaponModel.GetWeaponConfigModel(WeaponTypeEnum.HSA));
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

        public SupportItemDataModel GetSupportItemByIndex(int index)
        {
            if (index < 0 || index >= SupportItems.Count) return null;
            return SupportItems[index];
        }

        public void InitSupportItems(List<SupportTypeEnum> itemTypes)
        {
            // 先全部重置为 None
            for (int i = 0; i < SupportItems.Count; i++)
                SupportItems[i] = new SupportItemDataModel();

            for (int i = 0; i < SupportItems.Count && i < itemTypes.Count; i++)
            {
                var config = _supportConfigModel.GetSupportConfig(itemTypes[i]);
                if (config != null)
                    SupportItems[i] = new SupportItemDataModel(config);
            }

            // 如果一个都没选，给一个空袭作为测试
            bool allNone = true;
            for (int i = 0; i < SupportItems.Count; i++)
            {
                if (SupportItems[i].SupportType != SupportTypeEnum.None)
                {
                    allNone = false;
                    break;
                }
            }
            if (allNone)
            {
                var config = _supportConfigModel.GetSupportConfig(SupportTypeEnum.AirStrikes);
                if (config != null)
                    SupportItems[0] = new SupportItemDataModel(config);
            }
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