using UnityEngine;

public class WeaponController : MonoBehaviour
{
    public Weapon WeaponLeftSlot;
    public Weapon WeaponRightSlot;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private float _bulletSpeed;
    [SerializeField] private float _shootInterval;

    private void Awake()
    {
        Transform body = transform.Find("Body");
        Transform weaponSlotLeft = body.GetChild(0);
        Transform weaponSlotRight = body.GetChild(1);
        
        Transform weaponLeft = weaponSlotLeft.GetChild(0);
        Transform weaponRight = weaponSlotRight.GetChild(0);    

        // 获取武器槽
        WeaponLeftSlot = new Weapon(weaponSlotLeft, weaponLeft.Find("Muzzle"));
        WeaponRightSlot = new Weapon(weaponSlotRight, weaponRight.Find("Muzzle"));

    }

    /// <summary>
    /// 旋转武器
    /// </summary>
    public void RotateWeapon(Vector3 hitPos, Transform body, float aimZOffsetDeg)
    {
       if (!WeaponLeftSlot.WeaponSlot || !WeaponRightSlot.WeaponSlot)
            return;

        Vector3 hit = hitPos;
        if(hit == Vector3.zero || Vector3.Distance(hit, body.position) < 2f)
            return;

        Vector3 dirLeft = hit - WeaponLeftSlot.WeaponSlot.position;
        Vector3 dirRight = hit - WeaponRightSlot.WeaponSlot.position;

        // 在这里实现旋转平滑效果
        // 当前Weapon的朝向（欧拉角z)
        float currentZLeft = WeaponLeftSlot.WeaponSlot.rotation.eulerAngles.z;
        float currentZRight = WeaponRightSlot.WeaponSlot.rotation.eulerAngles.z;

        // 计算目标朝向
        float targetZLeft = Mathf.Atan2(dirLeft.y, dirLeft.x) * Mathf.Rad2Deg + aimZOffsetDeg;
        float targetZRight = Mathf.Atan2(dirRight.y, dirRight.x) * Mathf.Rad2Deg + aimZOffsetDeg;

        // 在360度环绕下插值
        float smoothZLeft = Mathf.LerpAngle(currentZLeft, targetZLeft, 10f * Time.deltaTime);
        float smoothZRight = Mathf.LerpAngle(currentZRight, targetZRight, 10f * Time.deltaTime);

        WeaponLeftSlot.WeaponSlot.rotation = Quaternion.Euler(0f, 0f, smoothZLeft);
        WeaponRightSlot.WeaponSlot.rotation = Quaternion.Euler(0f, 0f, smoothZRight);

        return;
    }

    public void Shoot(Weapon weapon)
    {
        if (_bulletPrefab == null || weapon.Muzzle == null)
        {
            Debug.LogError("BulletPrefab or BulletSpawnPoint or Muzzle is null");
            return;
        }

        // 计算发射方向：与武器朝向一致
        Vector3 shootDir = weapon.Muzzle.right; // local right 是2D武器的默认枪口方向 (一般为右)
        // 枪口世界坐标

        // 创建子弹
        GameObject bullet = Instantiate(_bulletPrefab, weapon.Muzzle.position, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = shootDir.normalized * _bulletSpeed;
        }
    }


}


public class Weapon
{
    public Weapon(Transform weaponSlot, Transform muzzle)
    {
        WeaponSlot = weaponSlot;
        Muzzle = muzzle;
        //BulletSpawnPoint = bulletSpawnPoint;
    }

    public Transform WeaponSlot;
    public Transform Muzzle;
    // public Transform BulletSpawnPoint;
}
