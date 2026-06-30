using System.Collections.Generic;
using QFramework.Enum;

namespace QFramework.Model
{
    public class SupportItemConfig
    {
        public SupportTypeEnum SupportType;
        public string name;
        public string iconPath;
        public string description;
        public float cooldownTime;
        public bool canUse;
    }

    public interface ISupportConfigModel : IModel
    {
        SupportItemConfig GetSupportConfig(SupportTypeEnum supportType);
    }

    public class SupportConfigModel : AbstractModel, ISupportConfigModel
    {
        private Dictionary<SupportTypeEnum, SupportItemConfig> _configs = new();

        protected override void OnInit()
        {
            _configs = new Dictionary<SupportTypeEnum, SupportItemConfig>
            {
                // { 
                //     SupportTypeEnum.AirStrikes, new SupportItemConfig
                //     {
                //         SupportType = SupportTypeEnum.AirStrikes,
                //         name = "空袭指令",
                //         iconPath = "",
                //         description = "在指定位置进行空袭打击",
                //         cooldownTime = 30f,
                //         canUse = true,
                //     }
                // },
                // { 
                //     SupportTypeEnum.AirSupport, new SupportItemConfig
                //     {
                //         SupportType = SupportTypeEnum.AirSupport,
                //         name = "空中支援",
                //         iconPath = "",
                //         description = "呼叫空中支援火力",
                //         cooldownTime = 30f,
                //         canUse = true,
                //     }
                // },
                { 
                    SupportTypeEnum.Artillery, new SupportItemConfig
                    {
                        SupportType = SupportTypeEnum.Artillery,
                        name = "炮击指令",
                        iconPath = "Texture/UI/Icon/Support/icon_artillery",
                        description = "在指定位置进行一连串的炮火支援",
                        cooldownTime = 30f,
                        canUse = true,
                    }
                },
                { 
                    SupportTypeEnum.Missile, new SupportItemConfig
                    {
                        SupportType = SupportTypeEnum.Missile,
                        name = "制导导弹",
                        iconPath = "Texture/UI/Icon/Support/icon_missile",
                        description = "在指定位置发射一枚制导导弹",
                        cooldownTime = 45f,
                        canUse = true,
                    }
                },
            };
        }

        public SupportItemConfig GetSupportConfig(SupportTypeEnum supportType)
        {
            _configs.TryGetValue(supportType, out var config);
            return config;
        }
    }
}
