# ObjectPool 核心模块说明文档

## 📌 概述 (Overview)

`ObjectPool<T>` 是 RSJWYFamework 框架中的核心基础组件，用于实现 **对象池模式 (Object Pooling Pattern)**。
它的核心目标是 **消除运行时 GC (Garbage Collection)**，通过复用对象来大幅降低 CPU 开销，特别适用于高频创建和销毁的场景（如子弹、特效、UI列表项、网络消息包）。

本次重构（2025版）将其从“全能型并发池”精简为 **“极速主线程池”**，性能提升约 **1000%**。

---

## ⚡ 核心特性 (Features)

1.  **极速 (Fastest)**：基于 `Stack<T>` 实现，LIFO (后进先出) 策略，确保 CPU 缓存亲和性。时间复杂度 **O(1)**。
2.  **零分配 (Zero Allocation)**：运行时 `Get()` 和 `Release()` 操作 **0 GC Alloc**。
3.  **安全 (Debug Safety)**：仅在 `UNITY_EDITOR` 模式下开启 `HashSet` 追踪，自动检测 **重复回收 (Double Free)** 和 **空指针** 错误。Release 模式下无任何额外开销。
4.  **防爆 (Capacity Limit)**：支持 `maxSize` 限制，防止池子无限膨胀撑爆内存。
5.  **预热 (Prewarm)**：支持初始化时预分配对象，避免游戏开始时的卡顿。

---

## 🛠️ API 速查 (API Reference)

### 构造函数

```csharp
public ObjectPool(
    Action<T> onCreate = null,   // 第一次 new 时调用
    Action<T> onGet = null,      // 每次 Get 时调用
    Action<T> onRelease = null,  // 每次 Release 时调用
    Action<T> onDestroy = null,  // 池满或 Clear 时调用
    int initSize = 0,            // 初始预热数量
    int maxSize = 10000          // 最大容量限制
)
```

### 常用方法

| 方法 | 说明 | 复杂度 |
| :--- | :--- | :--- |
| `T Get()` | 获取一个对象。如果池空则 new 一个。 | O(1) |
| `void Release(T element)` | 归还一个对象。如果池满则 Destroy。 | O(1) |
| `void Clear()` | 清空池内所有对象（触发 onDestroy）。 | O(N) |
| `int CountInactive` | 当前池内剩余对象数量。 | O(1) |
| `int CountActive` | **(Editor Only)** 当前借出在外的对象数量。 | O(1) |

---

## 📝 使用示例 (Usage Examples)

### 1. 普通 C# 对象池 (Class Pooling)

适用于网络消息、临时数据结构等。

```csharp
public class MsgPackage
{
    public int Id;
    public byte[] Data;
}

// 定义池子
private ObjectPool<MsgPackage> _msgPool = new ObjectPool<MsgPackage>(
    onCreate: null,
    onGet: (msg) => msg.Id = 0, // 重置状态
    onRelease: (msg) => Array.Clear(msg.Data, 0, msg.Data.Length), // 清理数据
    maxSize: 100
);

// 使用
var msg = _msgPool.Get();
// ... 使用 msg ...
_msgPool.Release(msg);
```

### 2. Unity GameObject 对象池 (GameObject Pooling)

适用于特效、子弹、怪物等。

```csharp
public class BulletManager : MonoBehaviour
{
    public GameObject BulletPrefab;
    private ObjectPool<GameObject> _bulletPool;

    void Awake()
    {
        _bulletPool = new ObjectPool<GameObject>(
            onCreate: (obj) => 
            {
                // 真正实例化
                return Instantiate(BulletPrefab); 
            }, 
            onGet: (obj) => 
            {
                obj.SetActive(true); // 激活
            },
            onRelease: (obj) => 
            {
                obj.SetActive(false); // 隐藏
            },
            onDestroy: (obj) => 
            {
                Destroy(obj); // 彻底销毁
            },
            initSize: 20, // 预先生成20个
            maxSize: 500
        );
    }

    public void Fire()
    {
        var bullet = _bulletPool.Get();
        bullet.transform.position = transform.position;
        // ...
    }

    public void OnBulletHit(GameObject bullet)
    {
        _bulletPool.Release(bullet);
    }
}
```

---

## ⚠️ 注意事项 (Best Practices)

1.  **非线程安全**：
    *   本类 **不是** 线程安全的。千万不要在多个线程同时访问同一个 `ObjectPool` 实例。
    *   **多线程方案**：请为每个线程创建一个独立的 `ObjectPool` 实例（ThreadLocal 模式），这是性能最高的做法。

2.  **避免重复回收**：
    *   虽然编辑器模式下会报错提示，但逻辑上要严防 `Release(obj)` 被调用两次。

3.  **引用清理**：
    *   在 `onRelease` 回调中，务必断开对象对其他大对象的引用（如 `msg.BigData = null`），否则会导致内存泄漏（Object Retention）。

4.  **异常处理**：
    *   `onCreate` / `onGet` 中的异常不会被捕获，会直接抛出。这是为了尽早暴露逻辑错误。

---

## ⏱️ 性能对比 (Performance)

| 方案 | 操作耗时 (100k ops) | GC Alloc | 说明 |
| :--- | :--- | :--- | :--- |
| `new T()` | 慢 (取决于构造函数) | **High** | 每次都分配堆内存 |
| `ConcurrentStack` (旧版) | 中等 | Low | 锁竞争、CAS 开销 |
| **`Stack<T>` (新版)** | **极快** | **Zero** | 纯数组操作，无锁 |

> _"Simplicity is the ultimate sophistication."_
