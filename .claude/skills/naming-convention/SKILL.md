# Skill: 代码命名规范

## Purpose
强制项目代码命名规范，确保所有新增和修改的代码符合团队约定的命名标准。

## Trigger Conditions
在生成、编辑或审查任何 C# 代码文件时自动激活。

## Constraints
1. 公开成员
- 接口：必须使用 `I` + 大驼峰 → 例如 `IArchitecture`
- 公共方法：必须使用大驼峰，动词优先 → 例如 `GetArchitecture()`
- 公共字段/属性：必须使用大驼峰 → 例如 `DetectionRange`
- 公开组件引用：必须使用大驼峰 → 例如 `Rigidbody2D Rb`
- 公开预制体引用：必须使用 `pf_` + 大驼峰 → 例如 `pf_Bullet`
- 布尔字段：必须使用 `Is/Has/Can` + 大驼峰 → 例如 `IsInit`

2. 私有成员
- 私有字段：必须使用 `_` + 小驼峰 → 例如 `_moveSpeed`
- 静态私有字段：必须使用 `s_` + 小驼峰 → 例如 `s_deathMaterial`
- 私有布尔字段：必须使用 `_` + `has/is/can` + 小驼峰 → 例如 `_hasPatrolRoute`

3. 序列化成员
- 序列化私有字段：必须使用 `[SerializeField]` + `_` + 小驼峰
- 序列化私有预制体：必须使用 `[SerializeField]` + `_pf_` + 小驼峰 → 例如 `_pf_bullet`

4. 其他
- 参数：必须使用小驼峰 → 例如 `target`
- 枚举：必须使用大驼峰，可选 `Enum` 后缀 → 例如 `EnemyTypeEnum`


## Output Format
当生成或修改代码时，自动应用上述命名规范。如需修改现有不符合规范的命名，先提示用户确认。

1. 确认作用域
- 局部（<10行）→ 允许短名（i, cnt, idx）
- 全局/公开 → 必须完整清晰

2. 去重（利用上下文）
- 类内字段 → 省略类名前缀（Car类用_color而非_carColor）
- 方法内局部 → 省略方法名含义

3. 替换为通用缩写
- count → cnt
- index → idx
- identifier → id
- number → num

4. 删除泛词
- getUserData() → getUser()
- CarInfo → Car
- tempResult → result

5. 避免数字/无意义后缀
- value1 / data2 → 改用含义名
- temp / tmp → 改为具体用途

检查：○ 能看懂？○ 无冗余？○ 长度匹配作用域？


## 方法命名规范：
1. 公开方法：
- 动词开头 + 大驼峰 → GetXxx()、SetXxx()、DoXxx()
- 返回bool → Is/Has/Can + 大驼峰 → IsValid()、HasTarget()、CanAttack()
- 事件回调 → On + 名词 → OnDamage()、OnDeath()、OnCollision()
- 生命周期（Unity）→ Awake()、Start()、Update()、OnDestroy()

2. 私有方法：
- 动词开头 + 小驼峰 → updateHealth()、calculateDamage()
- 事件处理 → On + 名词 + Handler → onDamageHandler()
- 工具/辅助方法 → 加Helper/Internal后缀或前缀 → calculateInternal()

3. 扩展方法（C#）：
- 静态类：XxxExtensions
- 方法：动词 + 大驼峰 → ToVector3()、GetComponentSafe()

4. 异步方法：
- 后缀 Async → LoadDataAsync()、FetchPlayerAsync()

5. 常用动词参考：
- 获取 → Get、Fetch、Find、Query
- 设置 → Set、Apply、Assign、Update
- 执行 → Do、Execute、Perform、Run
- 创建 → Create、Build、Generate、Spawn
- 销毁 → Destroy、Dispose、Clean、Clear
- 检查 → Check、Verify、Validate、Is/Has/Can
- 计算 → Calculate、Compute、Evaluate
- 转换 → Convert、Parse、ToXxx、AsXxx

6. 精简要求：
- 去除冗余词 → GetUserData() ❌ → GetUser() ✅
- 上下文复用 → Car类内用 GetSpeed() 而非 GetCarSpeed()
- 长度匹配复杂度 → 简单逻辑用短名（Move()），复杂逻辑用完整名（CalculatePathFinding()）