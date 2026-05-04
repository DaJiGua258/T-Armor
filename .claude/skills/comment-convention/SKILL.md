---
name: comment-convention
description: 代码注释规范技能。当用户编写、生成或审查 Unity/C# 代码时，自动应用团队注释规范。触发场景：创建变量、编写方法、方法内重要步骤、写循环、添加注释等。
disable-model-invocation: false
---

# 注释规范 Skill

变量注释：
- 新创建的变量：在行尾空两格 + // + 注释内容
- 格式：private float _moveSpeed;  // 移动速度（米/秒）
- 公开字段同上：public int enemyId;  // 敌人配置表ID

方法注释：
- 公开方法：必须写三行的 XML 文档注释
  /// <summary>
  /// 方法的简要说明
  /// </summary>

- 逻辑复杂的按功能在XML中提行

方法内部注释：
- 重要步骤：在步骤前单独一行写注释
- 格式：// 初始化寻路组件（独占一行，不跟代码同行）
- 每个独立的逻辑块前加注释说明

循环注释：
- 循环开始前必须写注释，说明循环目的
- 格式：// 遍历所有敌人，查找最近目标
- 复杂循环内可加简短说明注释

注释精简原则：
- 解释"为什么"做，而非"做了什么"（代码自解释）
- 复杂逻辑必须注释
- 魔法数字必须注释
- 临时方案标记 // TODO
- 避免废话：i++  // 自增i ❌

示例：

/// <summary>
/// 敌人实体，负责巡逻、追击和攻击行为
/// </summary>
public class Enemy : MonoBehaviour
{
    // 变量注释（行尾空两格）
    private float _moveSpeed = 3f;  // 移动速度（米/秒）
    private Transform _target;  // 当前追击目标
    public int enemyId;  // 敌人配置表ID

    /// <summary>
    /// 初始化敌人组件
    /// </summary>
    private void Start()
    {
        // 获取必要组件引用
        _target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    /// <summary>
    /// 计算与目标的距离
    /// </summary>
    /// <param name="targetPos">目标世界坐标</param>
    /// <returns>欧氏距离</returns>
    public float GetDistance(Vector3 targetPos)
    {
        // 计算位置差
        Vector3 offset = transform.position - targetPos;
        // 返回距离值
        return offset.magnitude;
    }

    private void Update()
    {
        // 检测玩家是否进入警戒范围
        float distance = GetDistance(_target.position);
        
        // 距离足够近，切换为攻击状态
        if (distance < _attackRange)
        {
            ChangeState(EnemyState.Attack);
        }
    }

    // 遍历所有子弹，检测与敌人的碰撞
    for (int i = 0; i < _bullets.Count; i++)
    {
        _bullets[i].CheckCollision(_enemies);
    }
}

name: region-wrapper
description: 用 #region 框起功能相关的方法
trigger: 用户要求整理/框起/折叠相关方法
steps:
  - 识别选中代码或文件中的方法分组（根据命名/注释/调用关系）
  - 在每组前插入: "#region ----- {区域名} -------------------------"
  - 在每组后插入: "#endregion"
  - 区域名优先从注释提取，否则用功能关键词
format:
  prefix: "#region ----- "
  suffix: " -------------------------"
  close: "#endregion"