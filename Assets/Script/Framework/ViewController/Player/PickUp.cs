using UnityEngine;
using QFramework;
using QFramework.Enum;
using static QFramework.Command.PickUpCommand;
using System;




// 如果需要使用 UnityEditor 相关的类，必须在非编辑器环境下屏蔽
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PickUp : MonoBehaviour, IController
{
    public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

    [SerializeField] private int _instanceId;
    
    public TypeEnum _type;
    public WeaponTypeEnum _weaponType;
    public EquipmentTypeEnum _equipmentType;
    public PickUpTypeEnum _pickUpType;

    void Start()
    {
        UpdateInstanceId();
    }

    public int GetInstanceId()
    {
        return _instanceId;
    }


    // 逻辑：更新 InstanceId
    public void UpdateInstanceId()
    {
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
            case TypeEnum.PickUp:
                if (_pickUpType == PickUpTypeEnum.None)
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
                _instanceId = this.SendCommand(new AddPickUpWeaponInstance(_weaponType));
                break;
            // case TypeEnum.Equipment:
            //     _instanceId = this.SendCommand(new AddPickUpEquipmentInstance(_equipmentType));
            //     break;
            // case TypeEnum.PickUp:
            //     _instanceId = this.SendCommand(new AddPickUpPickUpInstance(_pickUpType));
            //     break;
            case TypeEnum.None:
                _instanceId = -1;
                break;
        }
        Debug.Log($"已更新 {gameObject.name} 的 InstanceId: {_instanceId}");
    }
}

// --- 以下是编辑器代码，必须用 #if UNITY_EDITOR 包裹 ---
#if UNITY_EDITOR
[CustomEditor(typeof(PickUp))]
public class PickUpEditor : Editor
{
    SerializedProperty _typeProp;
    SerializedProperty _weaponTypeProp;
    SerializedProperty _equipmentTypeProp;
    SerializedProperty _pickUpTypeProp;
    SerializedProperty _instanceIdProp;

    void OnEnable()
    {
        _typeProp = serializedObject.FindProperty("_type");
        _weaponTypeProp = serializedObject.FindProperty("_weaponType");
        _equipmentTypeProp = serializedObject.FindProperty("_equipmentType");
        _pickUpTypeProp = serializedObject.FindProperty("_pickUpType");
        _instanceIdProp = serializedObject.FindProperty("_instanceId");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制只读 ID
        GUI.enabled = false;
        EditorGUILayout.PropertyField(_instanceIdProp, new GUIContent("实例 ID (只读)"));
        GUI.enabled = true;

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
            case TypeEnum.PickUp:
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