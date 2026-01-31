using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 数据管理器 (2025 Refactored Edition)
    /// 统一管理 ScriptableData 和 DataBase 类型的运行时数据
    /// </summary>
    [Module]
    [ModuleDependency(typeof(AppAsyncOperationSystem))]
    [ModuleDependency(typeof(EventManager))]
    public class DataManager : ModuleBase
    {
        // 统一存储容器：Type -> List<T> (stored as object)
        private readonly ConcurrentDictionary<Type, object> _repository = new();

        #region Public API - Add

        /// <summary>
        /// 添加单个数据
        /// </summary>
        public void AddData<T>(T data)
        {
            if (data == null)
            {
                AppLogger.Error("[DataManager] 尝试添加空数据");
                return;
            }

            var type = typeof(T);
            
            // 使用 AddOrUpdate 确保线程安全
            _repository.AddOrUpdate(type,
                // Add: 创建新列表
                _ => new List<T> { data },
                // Update: 追加到现有列表
                (_, existingObj) =>
                {
                    var list = (List<T>)existingObj;
                    lock (list) // 列表级锁，防止并发读写列表内部
                    {
                        list.Add(data);
                    }
                    return list;
                });
        }

        /// <summary>
        /// 批量添加数据
        /// </summary>
        public void AddDataList<T>(IList<T> dataList)
        {
            if (dataList == null || dataList.Count == 0)
            {
                AppLogger.Error("[DataManager] 尝试添加空列表");
                return;
            }

            var type = typeof(T);

            _repository.AddOrUpdate(type,
                _ => new List<T>(dataList),
                (_, existingObj) =>
                {
                    var list = (List<T>)existingObj;
                    lock (list)
                    {
                        list.AddRange(dataList);
                    }
                    return list;
                });
        }

        #endregion

        #region Public API - Remove

        /// <summary>
        /// 移除单个数据
        /// </summary>
        public bool RemoveData<T>(T data)
        {
            if (data == null) return false;

            if (_repository.TryGetValue(typeof(T), out var obj))
            {
                var list = (List<T>)obj;
                lock (list)
                {
                    return list.Remove(data);
                }
            }
            return false;
        }

        /// <summary>
        /// 批量移除满足条件的数据
        /// </summary>
        public int RemoveData<T>(Predicate<T> predicate)
        {
            if (predicate == null) return 0;

            if (_repository.TryGetValue(typeof(T), out var obj))
            {
                var list = (List<T>)obj;
                lock (list)
                {
                    return list.RemoveAll(predicate);
                }
            }
            return 0;
        }

        /// <summary>
        /// 清空指定类型的所有数据
        /// </summary>
        public void ClearData<T>()
        {
            if (_repository.TryGetValue(typeof(T), out var obj))
            {
                var list = (List<T>)obj;
                lock (list)
                {
                    list.Clear();
                }
            }
        }

        #endregion

        #region Public API - Get

        /// <summary>
        /// 获取指定类型的所有数据 (返回只读副本或只读包装)
        /// </summary>
        public IReadOnlyList<T> GetAllData<T>()
        {
            if (_repository.TryGetValue(typeof(T), out var obj))
            {
                var list = (List<T>)obj;
                // 返回原始列表的副本以避免外部修改影响内部，或者直接返回 list (如果不介意外部修改)
                // 为了安全，建议返回副本，但为了性能，如果是只读接口，可以直接返回
                // 这里选择返回一个新的 List 副本，防止多线程迭代报错
                lock (list)
                {
                    return new List<T>(list);
                }
            }
            return Array.Empty<T>();
        }

        /// <summary>
        /// 获取第一个数据
        /// </summary>
        public T GetFirstData<T>()
        {
            if (_repository.TryGetValue(typeof(T), out var obj))
            {
                var list = (List<T>)obj;
                lock (list)
                {
                    if (list.Count > 0) return list[0];
                }
            }
            return default;
        }

        /// <summary>
        /// 根据条件查询数据
        /// </summary>
        public List<T> GetDataWhere<T>(Predicate<T> predicate)
        {
            if (predicate == null) return new List<T>();

            if (_repository.TryGetValue(typeof(T), out var obj))
            {
                var list = (List<T>)obj;
                var result = new List<T>();
                lock (list)
                {
                    // 避免 LINQ Where，使用手动循环
                    foreach (var item in list)
                    {
                        if (predicate(item))
                        {
                            result.Add(item);
                        }
                    }
                }
                return result;
            }
            return new List<T>();
        }

        /// <summary>
        /// 随机获取一个数据
        /// </summary>
        public T GetRandomData<T>()
        {
            if (_repository.TryGetValue(typeof(T), out var obj))
            {
                var list = (List<T>)obj;
                lock (list)
                {
                    if (list.Count == 0) return default;
                    int index = UnityEngine.Random.Range(0, list.Count);
                    return list[index];
                }
            }
            return default;
        }

        /// <summary>
        /// 随机获取多个数据 (优化算法)
        /// </summary>
        /// <param name="count">数量</param>
        /// <param name="allowDuplicate">是否允许重复</param>
        public List<T> GetRandomDataList<T>(int count, bool allowDuplicate = false)
        {
            if (count <= 0) return new List<T>();

            if (_repository.TryGetValue(typeof(T), out var obj))
            {
                var list = (List<T>)obj;
                lock (list)
                {
                    int totalCount = list.Count;
                    if (totalCount == 0) return new List<T>();

                    var result = new List<T>(count);

                    if (allowDuplicate)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            result.Add(list[UnityEngine.Random.Range(0, totalCount)]);
                        }
                    }
                    else
                    {
                        // Fisher-Yates Shuffle 变体 (部分洗牌)
                        // 为了不修改原列表，我们需要创建一个索引数组或者临时列表
                        // 如果 count 远小于 totalCount，使用索引选择更优
                        // 如果 count 接近 totalCount，复制列表洗牌更优
                        
                        // 简单策略：复制一份引用列表进行洗牌
                        var temp = new List<T>(list);
                        int n = temp.Count;
                        int m = Mathf.Min(count, n);
                        
                        for (int i = 0; i < m; i++)
                        {
                            int r = UnityEngine.Random.Range(i, n);
                            (temp[i], temp[r]) = (temp[r], temp[i]); // Swap
                            result.Add(temp[i]);
                        }
                    }
                    return result;
                }
            }
            return new List<T>();
        }

        #endregion

        #region Legacy Compatibility (Obsolete)

        // 保留部分旧接口名称并标记为 Obsolete，以便平滑过渡（可选）
        // 鉴于这是一个彻底的重构，我们不再提供旧接口，强迫使用者更新代码以获得最佳性能。

        #endregion

        #region Lifecycle

        public override void Initialize()
        {
            _repository.Clear();
        }

        public override void Shutdown()
        {
            _repository.Clear();
        }

        public override void LifeUpdate()
        {
        }

        #endregion
    }
}
