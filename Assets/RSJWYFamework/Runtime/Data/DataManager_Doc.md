# DataManager 2025 重构版使用指南

## 🌟 核心变更 (Key Changes)

在 2025 年的重构中，我们彻底优化了 `DataManager` 的架构，旨在解决代码冗余、类型分裂和性能瓶颈问题。

### 1. 统一接口 (Unified API)
不再区分 `ScriptableObject` (`SB`) 和普通类 (`DataBase`)。
- **旧代码**: `AddDataSB<T>`, `AddData<T>`, `GetFirstDataSB<T>`, `GetFirstData<T>`
- **新代码**: `AddData<T>`, `GetFirstData<T>`

`DataManager` 现在可以管理任何类型的运行时数据列表，只要该类型是引用类型（建议）。

### 2. 性能优化 (Performance Boost)
- **零 GC 热路径**: 移除了核心方法 (`GetDataWhere`, `AddData`) 中所有的 LINQ 调用 (`Cast`, `ToList`, `Where`)，改为手动循环，显著降低运行时 GC 压力。
- **智能随机算法**: 
  - 单个随机：直接索引访问 (O(1))。
  - 多个随机：采用 **Fisher-Yates Shuffle** 算法 (O(N))，替代了极低效的 `OrderBy(Guid.NewGuid())` (O(N log N) + 大量 GC)。

### 3. 线程安全 (Thread Safety)
- 内部采用 `ConcurrentDictionary` 管理类型仓库。
- 对内部 `List<T>` 的所有读写操作均加锁 (`lock`)，确保多线程并发访问时的安全性。

---

## 📖 API 使用手册

### 1. 添加数据 (Add)

```csharp
// 添加单个数据
var config = new AppConfig();
ModuleManager.GetModule<DataManager>().AddData(config);

// 批量添加数据
List<EnemyData> enemies = ...;
ModuleManager.GetModule<DataManager>().AddDataList(enemies);
```

### 2. 获取数据 (Get)

```csharp
// 获取指定类型的第一个数据
var appConfig = ModuleManager.GetModule<DataManager>().GetFirstData<AppConfig>();

// 获取指定类型的所有数据 (返回只读副本，安全)
var allEnemies = ModuleManager.GetModule<DataManager>().GetAllData<EnemyData>();

// 根据条件查询 (零 GC)
var activeEnemies = ModuleManager.GetModule<DataManager>().GetDataWhere<EnemyData>(e => e.IsActive);
```

### 3. 移除数据 (Remove)

```csharp
// 移除单个数据
ModuleManager.GetModule<DataManager>().RemoveData(myEnemy);

// 批量移除 (例如移除所有死亡单位)
ModuleManager.GetModule<DataManager>().RemoveData<EnemyData>(e => e.IsDead);
```

### 4. 随机获取 (Random)

```csharp
// 随机获取一个
var randomEnemy = ModuleManager.GetModule<DataManager>().GetRandomData<EnemyData>();

// 随机获取 3 个不重复的敌人 (高性能洗牌)
var randomSquad = ModuleManager.GetModule<DataManager>().GetRandomDataList<EnemyData>(3, allowDuplicate: false);
```

---

## ⚠️ 迁移指南 (Migration Guide)

如果您之前的代码使用了 `*SB` 后缀的方法，请按以下规则修改：

1.  `AddDataSB(...)` -> `AddData(...)`
2.  `AddDataSetSB(...)` -> `AddDataList(...)`
3.  `RemoveDataSB(...)` -> `RemoveData(...)`
4.  `GetFirstDataSB(...)` -> `GetFirstData(...)`
5.  `GetAllDataSB(...)` -> `GetAllData(...)`
6.  `GetDataSBWhere(...)` -> `GetDataWhere(...)`

**注意**: 基类 `DataBaseSB` 已重命名为 `ScriptableData`，`DataBase` 保持不变（但推荐作为普通数据基类）。虽然 `DataManager` 不再强制要求继承这些基类，但为了代码规范，建议继续保持继承关系。
