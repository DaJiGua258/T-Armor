using System.Threading;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class WeaponController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public Weapon WeaponLeft;  // 武器脚本
        public Weapon WeaponRight; 
        

        void Awake()
        {
            Transform weaponSlotLeft = transform.Find("Body/WeaponSlotLeft");
            Transform weaponSlotRight = transform.Find("Body/WeaponSlotRight");
            
            WeaponLeft = weaponSlotLeft.GetChild(0).GetComponent<Weapon>();
            WeaponRight = weaponSlotRight.GetChild(0).GetComponent<Weapon>();

        }

        /// <summary>
        /// 旋转武器
        /// </summary>
        public void RotateWeapon(Vector3 hitPos, Transform body, float aimZOffsetDeg)
        {
        if (!WeaponLeft || !WeaponRight)
                return;

            Vector3 hit = hitPos;
            if(hit == Vector3.zero || Vector3.Distance(hit, body.position) < 2f)
                return;

            Vector3 dirLeft = hit - WeaponLeft.transform.position;
            Vector3 dirRight = hit - WeaponRight.transform.position;

            // 在这里实现旋转平滑效果
            // 当前Weapon的朝向（欧拉角z)
            float currentZLeft = WeaponLeft.transform.rotation.eulerAngles.z;
            float currentZRight = WeaponRight.transform.rotation.eulerAngles.z;

            // 计算目标朝向
            float targetZLeft = Mathf.Atan2(dirLeft.y, dirLeft.x) * Mathf.Rad2Deg + aimZOffsetDeg;
            float targetZRight = Mathf.Atan2(dirRight.y, dirRight.x) * Mathf.Rad2Deg + aimZOffsetDeg;

            // 在360度环绕下插值
            float smoothZLeft = Mathf.LerpAngle(currentZLeft, targetZLeft, 10f * Time.deltaTime);
            float smoothZRight = Mathf.LerpAngle(currentZRight, targetZRight, 10f * Time.deltaTime);

            WeaponLeft.transform.rotation = Quaternion.Euler(0f, 0f, smoothZLeft);
            WeaponRight.transform.rotation = Quaternion.Euler(0f, 0f, smoothZRight);

            return;
        }
    }
}
