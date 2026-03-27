using Unity.Mathematics;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("躯干引用")]
    public Transform Body;
    public float MoveSpeed;
    public float AimZOffsetDeg;

    [Header("脚部引用")]
    public Transform LegFl; // 前左
    public Transform LegFr; // 前右
    public Transform LegBr; // 后右
    public Transform LegBl; // 后左

    [Header("腿部调整参数")]
    public float FB;
    public float RL;
    public float LegRotation;

    [Header("惯性参数")]
    public float inertiaSpeed = 8f;         // 惯性插值速度
    public float inertiaDistance = 1f;    // 停止后惯性最大距离
    public float minMoveThreshold = 0.01f;  // 认为已经停止的阈值
    public float inertiaDelay = 0.7f;  // 滞后感倍数可调
    public Vector3 lastBodyDir;  // 上一次身体移动方向
    public float lerpFactor = 30;  // 躯干移动方向插值因子
    private Vector3 legInertiaOffset;  // 腿部惯性偏移
    private float legInertiaFactor;  // 腿部惯性因子
    private Vector3 lastBodyPosition;  // 上一次身体位置
    private Vector3 inertiaOffset;  // 惯性偏移
    

    // 武器槽
    [SerializeField] private Transform _weaponSlotLeft;
    [SerializeField] private Transform _weaponSlotRight;

    private Camera _cam;
    private Plane _plane;  // 平面

    void Awake()
    {
        _cam = Camera.main;
        if (!Body) Body = transform.Find("Body");

        // 获取武器槽
        if (!_weaponSlotLeft) _weaponSlotLeft = transform.Find("Body").GetChild(0);
        if (!_weaponSlotRight) _weaponSlotRight = transform.Find("Body").GetChild(1);

        // 获取脚部
        Transform legs = transform.Find("Legs");
        if (!LegFl) LegFl = legs.Find("FL");
        if (!LegFr) LegFr = legs.Find("FR");
        if (!LegBr) LegBr = legs.Find("BR");
        if (!LegBl) LegBl = legs.Find("BL");
    }

    private void Start()
    {
        ParamsInit();
        InitLegPostion();
    }

    private void Update()
    {
        Move(); // 移动
        RotateBody(); // 旋转躯干
        UpdateLegPostion(); // 更新腿部位置

        //RotateWeapon();
    }

    /// <summary>
    /// 初始化参数
    /// </summary>
    private void ParamsInit()
    {
        // MoveSpeed = 3f;
        AimZOffsetDeg = 180f;
    }


    /// <summary>
    /// 移动
    /// </summary>
    private void Move()
    {
        // 获取键盘输入的移动方向
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        // 如果输入方向的平方大于1，则归一化
        if (input.sqrMagnitude > 1f) input.Normalize();
        transform.position += new Vector3(input.x, input.y, 0f) * (MoveSpeed * Time.deltaTime);
    }

    

    /// <summary>
    /// 旋转躯干
    /// </summary>
    private void RotateBody()
    {
        
        if (!Body || !_cam)
            return;

        Vector3 hit = GetHitPosition();
        if(hit == Vector3.zero)
            return;

        // 计算点击位置与角色位置的差值
        Vector3 dir = hit - Body.position;
        dir.z = 0f;

        // 如果差值小于1e-6f，则不进行旋转
        if (Vector3.Distance(hit, Body.position) < 1e-6f) 
        {
            return;
        }

        // 在这里实现旋转平滑效果
        // 当前Body的朝向（欧拉角z)
        float currentZ = Body.rotation.eulerAngles.z;
        // 计算目标朝向
        float targetZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + AimZOffsetDeg;

        // 在360度环绕下插值
        float smoothZ = Mathf.LerpAngle(currentZ, targetZ, 10f * Time.deltaTime);

        Body.rotation = Quaternion.Euler(0f, 0f, smoothZ);

        // 当Body旋转到目标朝向时，小于10度，则允许Weapon旋转
        if(Mathf.Abs(currentZ - targetZ) < 10f)
        {
            RotateWeapon();
        }

        return;
    }

    /// <summary>
    /// 旋转武器
    /// </summary>
    private void RotateWeapon()
    {
       if (!_weaponSlotLeft || !_weaponSlotRight)
            return;

        Vector3 hit = GetHitPosition();
        if(hit == Vector3.zero || Vector3.Distance(hit, Body.position) < 2f)
            return;

        Vector3 dirLeft = hit - _weaponSlotLeft.position;
        Vector3 dirRight = hit - _weaponSlotRight.position;

        // 在这里实现旋转平滑效果
        // 当前Weapon的朝向（欧拉角z)
        float currentZLeft = _weaponSlotLeft.rotation.eulerAngles.z;
        float currentZRight = _weaponSlotRight.rotation.eulerAngles.z;

        // 计算目标朝向
        float targetZLeft = Mathf.Atan2(dirLeft.y, dirLeft.x) * Mathf.Rad2Deg + AimZOffsetDeg;
        float targetZRight = Mathf.Atan2(dirRight.y, dirRight.x) * Mathf.Rad2Deg + AimZOffsetDeg;

        // 在360度环绕下插值
        float smoothZLeft = Mathf.LerpAngle(currentZLeft, targetZLeft, 10f * Time.deltaTime);
        float smoothZRight = Mathf.LerpAngle(currentZRight, targetZRight, 10f * Time.deltaTime);

        _weaponSlotLeft.rotation = Quaternion.Euler(0f, 0f, smoothZLeft);
        _weaponSlotRight.rotation = Quaternion.Euler(0f, 0f, smoothZRight);
        return;
    }

    /// <summary>
    /// 获取点击位置的世界坐标
    /// </summary>
    /// <returns></returns>
    private Vector3 GetHitPosition()   
    {
        // 创建一个平面，用于计算点击位置与角色位置的差值
        // 数学上的无限平面，这里使用Vector3.forward作为法线
        _plane = new Plane(Vector3.forward, transform.position);

        // 从屏幕点击位置发射射线，获取点击位置的世界坐标
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (!_plane.Raycast(ray, out float d)) 
        {
            return Vector3.zero;
        }

        // 获取点击位置的世界坐标
        Vector3 hit = ray.GetPoint(d);  // 即point = ray.origin + ray.direction * d
        return hit;
    }

    /// <summary>
    /// 更新腿部位置
    /// </summary>
    void InitLegPostion()
    {
        if (!LegFl || !LegFr || !LegBr || !LegBl) return;

        // FL (前左): y增加(前), x减少(左)
        LegFl.localPosition = new Vector3(-RL, FB, LegFl.localPosition.z);
        LegFl.localRotation = Quaternion.Euler(0, 0, LegRotation);

        // FR (前右): y增加(前), x增加(右)
        LegFr.localPosition = new Vector3(RL, FB, LegFr.localPosition.z);
        LegFr.localRotation = Quaternion.Euler(0, 0, -LegRotation);

        // BR (后右): y减少(后), x增加(右)
        LegBr.localPosition = new Vector3(RL, -FB, LegBr.localPosition.z);
        LegBr.localRotation = Quaternion.Euler(0, 0, LegRotation);

        // BL (后左): y减少(后), x减少(左)
        LegBl.localPosition = new Vector3(-RL, -FB, LegBl.localPosition.z);
        LegBl.localRotation = Quaternion.Euler(0, 0, -LegRotation);
    }

    /// <summary>
    /// 更新腿部位置
    /// </summary>
    void UpdateLegPostion()
    {
        

        // 初始化
        if (lastBodyPosition == Vector3.zero) lastBodyPosition = transform.position;
        if (inertiaOffset == Vector3.zero) inertiaOffset = Vector3.zero;

        // 计算Body的速度
        Vector3 bodyDelta = (transform.position - lastBodyPosition);
        float bodySpeed = bodyDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        // 判断是否在移动（可调参数：速度阈值）
        bool isMoving = bodySpeed > minMoveThreshold;

        if (isMoving)
        {
            // 角色移动时，腿部惯性方向和Body移动方向相反，制造滞后感
            Vector3 targetOffset = -bodyDelta * inertiaDelay;   // 滞后感倍数可调
            inertiaOffset = Vector3.Lerp(inertiaOffset, targetOffset, inertiaSpeed * Time.deltaTime);
        }
        else
        {
            // 停止后，腿部朝上一次移动的方向“超前”一小段，然后逐渐回归
            Vector3 stopDir = bodyDelta.normalized;
            Vector3 targetOffset = stopDir * inertiaDistance;
            inertiaOffset = Vector3.Lerp(inertiaOffset, targetOffset, (inertiaSpeed * 0.33f) * Time.deltaTime);

            // 当惯性offset足够小时，归零
            if (inertiaOffset.magnitude < 0.001f)
                inertiaOffset = Vector3.zero;
        }

        // 对腿部偏移进行限制，避免偏移过大
        if (inertiaOffset.magnitude > inertiaDistance) inertiaOffset = inertiaOffset.normalized * inertiaDistance;

        // 记录本帧位置
        lastBodyPosition = transform.position;

        // 每条腿添加基于与body移动方向关系的独立偏移
        Vector3 bodyMoveDir = (GetHitPosition() - Body.position);
        lastBodyDir = bodyMoveDir;
        bodyMoveDir = Vector3.Lerp(lastBodyDir, bodyMoveDir, lerpFactor * Time.deltaTime).normalized;

        // 定义每条腿的相对初始朝向（单位向量）
        Vector3 legFlDir = new Vector3(-1, 1, 0).normalized;
        Vector3 legFrDir = new Vector3(1, 1, 0).normalized;
        Vector3 legBrDir = new Vector3(1, -1, 0).normalized;
        Vector3 legBlDir = new Vector3(-1, -1, 0).normalized;

        // 计算相关系数，越接近1代表方向越一致，应该减少惯性偏移
        float flWeight = Mathf.Max(Vector3.Dot(bodyMoveDir, legFlDir) + 0.5f, 0);
        float frWeight = Mathf.Max(Vector3.Dot(bodyMoveDir, legFrDir) + 0.5f, 0);
        float brWeight = Mathf.Max(Vector3.Dot(bodyMoveDir, legBrDir) + 0.5f, 0);
        float blWeight = Mathf.Max(Vector3.Dot(bodyMoveDir, legBlDir) + 0.5f, 0);

        Debug.Log("flWeight: " + flWeight + " frWeight: " + frWeight + " brWeight: " + brWeight + " blWeight: " + blWeight);
        Debug.Log("bodyMoveDir: " + bodyMoveDir);

        // 保存每只腿的惯性offset
        Vector3 legFlOffset = inertiaOffset * flWeight;
        Vector3 legFrOffset = inertiaOffset * frWeight;
        Vector3 legBrOffset = inertiaOffset * brWeight;
        Vector3 legBlOffset = inertiaOffset * blWeight;
        
        // 将惯性效果应用到四条腿的通用位移
        LegFl.localPosition = new Vector3(-RL + legFlOffset.x, FB + legFlOffset.y, LegFl.localPosition.z);
        LegFr.localPosition = new Vector3(RL + legFrOffset.x, FB + legFrOffset.y, LegFr.localPosition.z);
        LegBr.localPosition = new Vector3(RL + legBrOffset.x, -FB + legBrOffset.y, LegBr.localPosition.z);
        LegBl.localPosition = new Vector3(-RL + legBlOffset.x, -FB + legBlOffset.y, LegBl.localPosition.z);
    }
}
