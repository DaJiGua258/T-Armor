using UnityEngine;
using QFramework;
using QFramework.Enum;
using QFramework.Command;


// 如果需要使用 UnityEditor 相关的类，必须在非编辑器环境下屏蔽
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PickUpItems : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

    [SerializeField] private int _instanceId;
    
    public TypeEnum _type;
    public WeaponTypeEnum _weaponType;
    public EquipmentTypeEnum _equipmentType;
    public ItemTypeEnum _pickUpType;

    [Header("拾取物品表现参数")]
    [SerializeField] private bool canShowing = false;
    [SerializeField] private float _y;
    // [SerializeField] private float _yOffset = 0.5f;
    // [SerializeField] private float _ySpeed = 2f;
    [SerializeField] private float _rotationSpeed;

    void Start()
    {
        // _y = transform.position.y;
        InitInstanceId();
    }

    void Update()
    {
        if(canShowing)
        {
            // float y = Mathf.Sin(Time.time * _ySpeed) * _yOffset + _yOffset + _y;
            transform.rotation = Quaternion.Euler(0, 0, Time.time * _rotationSpeed);
        }
    }

    public int GetInstanceId()
    {
        return _instanceId;
    }


    // 逻辑：更新 InstanceId
    public void InitInstanceId()
    {
        if(canShowing)
        {
            return;
        }

        switch (_type)
        {
            case TypeEnum.Weapon:
                if (_weaponType == WeaponTypeEnum.None)
                {
                    Debug.LogWarning("WeaponType is None");
                    _instanceId = -1;
                    return;
                }
                break;
            case TypeEnum.Equipment:
                if (_equipmentType == EquipmentTypeEnum.None)
                {
                    Debug.LogWarning("EquipmentType is None");
                    _instanceId = -1;
                    return;
                }
                break;
            case TypeEnum.Item:
                if (_pickUpType == ItemTypeEnum.None)
                {
                    Debug.LogWarning("PickUpType is None");
                    _instanceId = -1;
                    return;
                }
                break;
            case TypeEnum.None:
                Debug.LogWarning("Type is None");
                _instanceId = -1;
                return;
        }
        
        switch (_type)
        {
            case TypeEnum.Weapon:
                _instanceId = this.SendCommand(new PickUpCommand.AddPickUpWeaponInstance(_weaponType));
                break;
            // case TypeEnum.Equipment:
            //     _instanceId = this.SendCommand(new AddPickUpEquipmentInstance(_equipmentType));
            //     break;
            case TypeEnum.Item:
                _instanceId = this.SendCommand(new PickUpCommand.AddPickUpItemInstance(_pickUpType));
                break;
            case TypeEnum.None:
                _instanceId = -1;
                break;
        }
        Debug.Log($"已更新 {gameObject.name} 的 InstanceId: {_instanceId}");
    }

    public void CanShowing()
    {
        canShowing = true;
    }
    
}


#if UNITY_EDITOR
[CustomEditor(typeof(PickUpItems))]
public class PickUpEditor : Editor
{
    SerializedProperty _typeProp;
    SerializedProperty _weaponTypeProp;
    SerializedProperty _equipmentTypeProp;
    SerializedProperty _pickUpTypeProp;
    SerializedProperty _instanceIdProp;

    SerializedProperty _yOffsetProp;
    SerializedProperty _ySpeedProp;
    SerializedProperty _rotationSpeedProp;

    void OnEnable()
    {
        _typeProp = serializedObject.FindProperty("_type");
        _weaponTypeProp = serializedObject.FindProperty("_weaponType");
        _equipmentTypeProp = serializedObject.FindProperty("_equipmentType");
        _pickUpTypeProp = serializedObject.FindProperty("_pickUpType");
        _instanceIdProp = serializedObject.FindProperty("_instanceId");

        _yOffsetProp = serializedObject.FindProperty("_yOffset");
        _ySpeedProp = serializedObject.FindProperty("_ySpeed");
        _rotationSpeedProp = serializedObject.FindProperty("_rotationSpeed");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制只读 ID
        GUI.enabled = false;
        EditorGUILayout.PropertyField(_instanceIdProp, new GUIContent("实例 ID (只读)"));
         GUI.enabled = true;

        EditorGUILayout.PropertyField(_yOffsetProp, new GUIContent("Y Offset"));
        EditorGUILayout.PropertyField(_ySpeedProp, new GUIContent("Y Speed"));
        EditorGUILayout.PropertyField(_rotationSpeedProp, new GUIContent("Rotation Speed"));

        EditorGUILayout.Space();

        // 绘制主枚举
        EditorGUILayout.PropertyField(_typeProp, new GUIContent("Type"));

        // 【关键修改点】：直接获取枚举的真实值，而不是索引 index
        // targetValue 会精确匹配你选中的枚举项
        TypeEnum currentType = (TypeEnum)_typeProp.enumValueIndex; 
        
        // 注意：如果你的枚举设置了特定数字（如 None=100），
        // 则需要用 _typeProp.intValue 强转
        // TypeEnum currentType = (TypeEnum)_typeProp.intValue;

        // 直接用枚举名判断，清晰且不会出错
        switch (currentType)
        {
            case TypeEnum.Weapon: 
                EditorGUILayout.PropertyField(_weaponTypeProp, new GUIContent("Weapon Type"));
                break;
            case TypeEnum.Equipment:
                EditorGUILayout.PropertyField(_equipmentTypeProp, new GUIContent("Equipment Type"));
                break;
            case TypeEnum.Item:
                EditorGUILayout.PropertyField(_pickUpTypeProp, new GUIContent("PickUp Type"));
                break;
            // 如果是 None，就不画任何细分类型
        }

        serializedObject.ApplyModifiedProperties();

        // if (serializedObject.ApplyModifiedProperties())
        // {
        //     ((PickUp)target).UpdateInstanceId();
        // }

        // if (GUILayout.Button("手动刷新 ID"))
        // {
        //     ((PickUp)target).UpdateInstanceId();
        // }
    }
}
#endif