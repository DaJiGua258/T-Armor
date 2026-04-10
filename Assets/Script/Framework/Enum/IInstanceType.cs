using UnityEngine;

namespace QFramework.Enum
{
    public interface IInstanceType
    {
        public TypeEnum TypeEnum { get; set; }
    }

    public class InstanceType : IInstanceType
    {
        public BindableProperty<int> InstanceId = new();

        private TypeEnum _typeId = TypeEnum.None;
        public TypeEnum TypeEnum 
        { 
            get
            {
                if (_typeId == TypeEnum.None)
                    Debug.LogWarning("TypeEnum: " + GetType().Name + " 尚未初始化！");
                return _typeId;
            }
            set => _typeId = value;
        }

        /// <summary>
        /// 依据当前枚举类型，与输入的枚举小类计算类别id
        /// </summary>
        /// <returns></returns>
        public int GetTypeId(int type)
        {
            return (int)TypeEnum * 100 + type;
        }
        
        /// <summary>
        /// 依据计数器获取当前实例的id
        /// </summary>        
        /// <param name="counter"></param>
        /// <returns></returns>
        public int GetInstanceId(int type, int counter)
        {
            return GetTypeId(type) * 100 + counter;
        }
    }
}