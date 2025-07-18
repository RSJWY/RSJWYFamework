using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    [Module]
    public class DataManager:ModuleBase
    {
         /// <summary>
        /// 数据集，基于ScriptableObject
        /// </summary>
        private ConcurrentDictionary<Type, List<DataBaseSB>> dataSBDic=new();
        /// <summary>
        /// 数据集，基于DataBase
        /// </summary>
        private ConcurrentDictionary<Type, List<DataBase>> dataDic=new();

        /// <summary>
        /// 添加数据
        /// </summary>
        public void AddDataSetSB<TDataBaseSB>(List<TDataBaseSB> dataSet)where TDataBaseSB:DataBaseSB
        {
            if (dataSet == null || dataSet.Count == 0)
            {
                AppLogger.Error("传入的数据为空或无元素");
                return;
            }
            //获取数据类型
            Type type = typeof(TDataBaseSB);
            
            // 线程安全操作
            dataSBDic.AddOrUpdate(
                type,
                // 如果不存在该类型，创建新列表并添加数据
                _ => new List<DataBaseSB>(dataSet.Cast<DataBaseSB>()),
                // 如果已存在，合并数据（需类型检查）
                (_, existingList) =>
                {
                    // 确保现有数据和新数据的类型一致
                    if (existingList.Count > 0 && existingList[0].GetType() != type)
                    {
                        AppLogger.Error($"类型不匹配！已存在的数据类型为 {existingList[0].GetType()}，尝试添加 {type}");
                        return existingList;
                    }
                    existingList.AddRange(dataSet.Cast<DataBaseSB>());
                    return existingList;
                }
            );
        }
        /// <summary>
        /// 添加数据
        /// </summary>
        public void AddDataSet<TDataBase>(List<TDataBase> dataSet)where TDataBase:DataBase
        {
            if (dataSet == null || dataSet.Count == 0)
            {
                AppLogger.Error("传入的数据为空或无元素");
                return;
            }
            //获取数据类型
            Type type = typeof(TDataBase);
            
            // 线程安全操作
            dataDic.AddOrUpdate(
                type,
                // 如果不存在该类型，创建新列表并添加数据
                _ => new List<DataBase>(dataSet.Cast<DataBase>()),
                // 如果已存在，合并数据（需类型检查）
                (_, existingList) =>
                {
                    // 确保现有数据和新数据的类型一致
                    if (existingList.Count > 0 && existingList[0].GetType() != type)
                    {
                        AppLogger.Error($"类型不匹配！已存在的数据类型为 {existingList[0].GetType()}，尝试添加 {type}");
                        return existingList;
                    }
                    existingList.AddRange(dataSet.Cast<DataBase>());
                    return existingList;
                }
            );
        }
        public void AddDataSB<TDataBaseSB>(TDataBaseSB data) where TDataBaseSB : DataBaseSB
        {
            if (data == null)
            {
                AppLogger.Error("传入的数据为空");
                return;
            }

            Type type = typeof(TDataBaseSB);
    
            // 线程安全操作
            dataSBDic.AddOrUpdate(
                type,
                // 如果不存在该类型，创建新列表并添加数据
                _ => new List<DataBaseSB> { data },
                // 如果已存在，追加数据（需类型检查）
                (_, existingList) =>
                {
                    // 确保现有数据的类型和新数据一致
                    if (existingList.Count > 0 && existingList[0].GetType() != type)
                    {
                        AppLogger.Error($"类型不匹配！已存在的数据类型为 {existingList[0].GetType()}，尝试添加 {type}");
                        return existingList;
                    }
                    existingList.Add(data);
                    return existingList;
                }
            );
        }
        public void AddData<TDataBase>(TDataBase data) where TDataBase : DataBase
        {
            if (data == null)
            {
                AppLogger.Error("传入的数据为空");
                return;
            }

            Type type = typeof(TDataBase);
    
            // 线程安全操作
            dataDic.AddOrUpdate(
                type,
                // 如果不存在该类型，创建新列表并添加数据
                _ => new List<DataBase> { data },
                // 如果已存在，追加数据（需类型检查）
                (_, existingList) =>
                {
                    // 确保现有数据的类型和新数据一致
                    if (existingList.Count > 0 && existingList[0].GetType() != type)
                    {
                        AppLogger.Error($"类型不匹配！已存在的数据类型为 {existingList[0].GetType()}，尝试添加 {type}");
                        return existingList;
                    }
                    existingList.Add(data);
                    return existingList;
                }
            );
        }


        /// <summary>
        /// 移除单个数据
        /// </summary>
        /// <param name="data">要移除的数据</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveDataSB<TDataBaseSB>(TDataBaseSB data) where TDataBaseSB : DataBaseSB
        {
            if (data == null)
            {
                AppLogger.Error("要移除的数据为空");
                return false;
            }

            Type type = typeof(TDataBaseSB);
    
            if (!dataSBDic.TryGetValue(type, out var list))
            {
                AppLogger.Error($"没有找到类型 {type} 的数据集");
                return false;
            }

            // 类型检查
            if (list.Count > 0 && list[0].GetType() != type)
            {
                AppLogger.Error($"类型不匹配！数据集中的类型为 {list[0].GetType()}，尝试移除 {type}");
                return false;
            }

            // 线程安全操作
            lock (list)  // 对列表操作加锁保证线程安全
            {
                return list.Remove(data);
            }
        }
        /// <summary>
        /// 移除单个数据
        /// </summary>
        /// <param name="data">要移除的数据</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveData<TDataBase>(TDataBase data) where TDataBase : DataBase
        {
            if (data == null)
            {
                AppLogger.Error("要移除的数据为空");
                return false;
            }

            Type type = typeof(TDataBase);
    
            if (!dataDic.TryGetValue(type, out var list))
            {
                AppLogger.Error($"没有找到类型 {type} 的数据集");
                return false;
            }

            // 类型检查
            if (list.Count > 0 && list[0].GetType() != type)
            {
                AppLogger.Error($"类型不匹配！数据集中的类型为 {list[0].GetType()}，尝试移除 {type}");
                return false;
            }

            // 线程安全操作
            lock (list)  // 对列表操作加锁保证线程安全
            {
                return list.Remove(data);
            }
        }
        /// <summary>
        /// 批量移除数据
        /// </summary>
        /// <param name="predicate">移除条件</param>
        /// <returns>移除的数量</returns>
        public int RemoveDataSB<TDataBaseSB>(Predicate<TDataBaseSB> predicate) where TDataBaseSB : DataBaseSB
        {
            if (predicate == null)
            {
                AppLogger.Error("移除条件不能为空");
                return 0;
            }

            Type type = typeof(TDataBaseSB);
    
            if (!dataSBDic.TryGetValue(type, out var list))
            {
                AppLogger.Error($"没有找到类型 {type} 的数据集");
                return 0;
            }

            // 类型检查
            if (list.Count > 0 && list[0].GetType() != type)
            {
                AppLogger.Error($"类型不匹配！数据集中的类型为 {list[0].GetType()}，尝试移除 {type}");
                return 0;
            }

            // 线程安全操作
            lock (list)
            {
                return list.RemoveAll(item => predicate((TDataBaseSB)item));
            }
        }
        /// <summary>
        /// 批量移除数据
        /// </summary>
        /// <param name="predicate">移除条件</param>
        /// <returns>移除的数量</returns>
        public int RemoveData<TDataBase>(Predicate<TDataBase> predicate) where TDataBase : DataBase
        {
            if (predicate == null)
            {
                AppLogger.Error("移除条件不能为空");
                return 0;
            }

            Type type = typeof(TDataBase);
    
            if (!dataDic.TryGetValue(type, out var list))
            {
                AppLogger.Error($"没有找到类型 {type} 的数据集");
                return 0;
            }

            // 类型检查
            if (list.Count > 0 && list[0].GetType() != type)
            {
                AppLogger.Error($"类型不匹配！数据集中的类型为 {list[0].GetType()}，尝试移除 {type}");
                return 0;
            }

            // 线程安全操作
            lock (list)
            {
                return list.RemoveAll(item => predicate((TDataBase)item));
            }
        }
        /// <summary>
        /// 清空指定类型的所有数据
        /// </summary>
        public void ClearDataSB<TDataBaseSB>() where TDataBaseSB : DataBaseSB
        {
            Type type = typeof(TDataBaseSB);
    
            if (dataSBDic.TryGetValue(type, out var list))
            {
                lock (list)
                {
                    list.Clear();
                }
            }
        }
        /// <summary>
        /// 清空指定类型的所有数据
        /// </summary>
        public void ClearData<TDataBase>() where TDataBase : DataBase
        {
            Type type = typeof(TDataBase);
    
            if (dataDic.TryGetValue(type, out var list))
            {
                lock (list)
                {
                    list.Clear();
                }
            }
        }

        /// <summary>
        /// 获取指定类型的全部数据
        /// </summary>
        /// <returns>只读数据列表</returns>
        public IReadOnlyList<TDataBaseSB> GetAllDataSB<TDataBaseSB>() where TDataBaseSB : DataBaseSB
        {
            Type type = typeof(TDataBaseSB);
    
            //无法获取数据时
            if (!dataSBDic.TryGetValue(type, out var list) || list.Count == 0)
            {
                return new List<TDataBaseSB>().AsReadOnly();
            }

            //获取数据
            // 类型检查-类型获取错误
            if (list[0].GetType() != type)
            {
                AppLogger.Error($"类型不匹配！数据集中的类型为 {list[0].GetType()}，请求类型为 {type}");
                return new List<TDataBaseSB>().AsReadOnly();
            }
            //返回只读数据
            return list.Cast<TDataBaseSB>().ToList().AsReadOnly();
        }
        
        /// <summary>
        /// 获取指定类型的全部数据
        /// </summary>
        /// <returns>只读数据列表</returns>
        public IReadOnlyList<TDataBase> GetAllData<TDataBase>() where TDataBase : DataBase
        {
            Type type = typeof(TDataBase);
    
            //无法获取数据时
            if (!dataDic.TryGetValue(type, out var list) || list.Count == 0)
            {
                return new List<TDataBase>().AsReadOnly();
            }

            //获取数据
            // 类型检查-类型获取错误
            if (list[0].GetType() != type)
            {
                AppLogger.Error($"类型不匹配！数据集中的类型为 {list[0].GetType()}，请求类型为 {type}");
                return new List<TDataBase>().AsReadOnly();
            }
            //返回只读数据
            return list.Cast<TDataBase>().ToList().AsReadOnly();
        }
        /// <summary>
        /// 获取指定类型的第一个数据
        /// </summary>
        /// <returns>第一个数据项，如果没有则返回null</returns>
        public TDataBaseSB GetFirstDataSB<TDataBaseSB>() where TDataBaseSB : DataBaseSB
        {
            Type type = typeof(TDataBaseSB);
    
            if (!dataSBDic.TryGetValue(type, out var list) || list.Count == 0)
            {
                return null;
            }

            // 类型检查
            if (list[0].GetType() != type)
            {
                AppLogger.Error($"类型不匹配！数据集中的类型为 {list[0].GetType()}，请求类型为 {type}");
                return null;
            }

            return (TDataBaseSB)list[0];
        }

        /// <summary>
        /// 获取指定类型的第一个数据
        /// </summary>
        /// <returns>第一个数据项，如果没有则返回null</returns>
        public TDataBase GetFirstData<TDataBase>() where TDataBase : DataBase
        {
            Type type = typeof(TDataBase);
    
            if (!dataDic.TryGetValue(type, out var list) || list.Count == 0)
            {
                return null;
            }

            // 类型检查
            if (list[0].GetType() != type)
            {
                AppLogger.Error($"类型不匹配！数据集中的类型为 {list[0].GetType()}，请求类型为 {type}");
                return null;
            }

            return (TDataBase)list[0];
        }

        /// <summary>
        /// 根据条件获取部分数据
        /// </summary>
        /// <param name="predicate">筛选条件</param>
        /// <returns>符合条件的数据列表</returns>
        public IReadOnlyList<TDataBaseSB> GetDataSBWhere<TDataBaseSB>(Predicate<TDataBaseSB> predicate) 
            where TDataBaseSB : DataBaseSB
        {
            if (predicate == null)
            {
                AppLogger.Error("筛选条件不能为空");
                return new List<TDataBaseSB>().AsReadOnly();
            }

            Type type = typeof(TDataBaseSB);
    
            if (!dataSBDic.TryGetValue(type, out var list) || list.Count == 0)
            {
                return new List<TDataBaseSB>().AsReadOnly();
            }

            // 类型检查
            if (list[0].GetType() != type)
            {
                AppLogger.Error($"类型不匹配！数据集中的类型为 {list[0].GetType()}，请求类型为 {type}");
                return new List<TDataBaseSB>().AsReadOnly();
            }

            var result = new List<TDataBaseSB>();
            foreach (var item in list)
            {
                var typedItem = (TDataBaseSB)item;
                if (predicate(typedItem))
                {
                    result.Add(typedItem);
                }
            }
    
            return result.AsReadOnly();
        }

        /// <summary>
        /// 根据条件获取部分数据
        /// </summary>
        /// <param name="predicate">筛选条件</param>
        /// <returns>符合条件的数据列表</returns>
        public IReadOnlyList<TDataBase> GetDataWhere<TDataBase>(Predicate<TDataBase> predicate)
            where TDataBase : DataBase
        {
            if (predicate == null)
            {
                AppLogger.Error("筛选条件不能为空");
                return new List<TDataBase>().AsReadOnly();
            }

            Type type = typeof(TDataBase);

            if (!dataDic.TryGetValue(type, out var list) || list.Count == 0)
            {
                return new List<TDataBase>().AsReadOnly();
            }

            // 类型检查
            if (list[0].GetType() != type)
            {
                AppLogger.Error($"类型不匹配！数据集中的类型为 {list[0].GetType()}，请求类型为 {type}");
                return new List<TDataBase>().AsReadOnly();
            }

            var result = new List<TDataBase>();
            foreach (var item in list)
            {
                var typedItem = (TDataBase)item;
                if (predicate(typedItem))
                {
                    result.Add(typedItem);
                }
            }

            return result.AsReadOnly();
        }

        
        /// <summary>
        /// 随机获取一个数据
        /// </summary>
        /// <returns>随机数据项，如果没有则返回null</returns>
        public TDataBaseSB GetRandomDataSB<TDataBaseSB>() where TDataBaseSB : DataBaseSB
        {
            var allData = GetAllDataSB<TDataBaseSB>();
            if (allData.Count == 0)
            {
                return null;
            }
    
            int index = UnityEngine.Random.Range(0, allData.Count);
            return allData[index];
        }
        
        
        /// <summary>
        /// 随机获取一个数据
        /// </summary>
        /// <returns>随机数据项，如果没有则返回null</returns>
        public TDataBase GetRandomData<TDataBase>() where TDataBase : DataBase
        {
            var allData = GetAllData<TDataBase>();
            if (allData.Count == 0)
            {
                return null;
            }
    
            int index = UnityEngine.Random.Range(0, allData.Count);
            return allData[index];
        }


        /// <summary>
        /// 随机获取多个数据
        /// </summary>
        /// <param name="count">要获取的数量</param>
        /// <param name="allowDuplicate">是否允许重复</param>
        /// <returns>随机数据列表</returns>
        public IReadOnlyList<TDataBaseSB> GetRandomDataSBs<TDataBaseSB>(int count, bool allowDuplicate = false) 
            where TDataBaseSB : DataBaseSB
        {
            var allData = GetAllDataSB<TDataBaseSB>();
            if (allData.Count == 0)
            {
                return new List<TDataBaseSB>().AsReadOnly();
            }

            var result = new List<TDataBaseSB>();
            if (allowDuplicate || count >= allData.Count)
            {
                // 允许重复或请求数量大于等于总数，直接随机选择
                for (int i = 0; i < count; i++)
                {
                    int index = UnityEngine.Random.Range(0, allData.Count);
                    result.Add(allData[index]);
                }
            }
            else
            {
                // 不允许重复且请求数量小于总数，使用洗牌算法
                var shuffled = allData.OrderBy(x => Guid.NewGuid()).Take(count).ToList();
                result.AddRange(shuffled);
            }
    
            return result.AsReadOnly();
        }
        /// <summary>
        /// 随机获取多个数据
        /// </summary>
        /// <param name="count">要获取的数量</param>
        /// <param name="allowDuplicate">是否允许重复</param>
        /// <returns>随机数据列表</returns>
        public IReadOnlyList<TDataBase> GetRandomDatas<TDataBase>(int count, bool allowDuplicate = false) 
            where TDataBase : DataBase
        {
            var allData = GetAllData<TDataBase>();
            if (allData.Count == 0)
            {
                return new List<TDataBase>().AsReadOnly();
            }

            var result = new List<TDataBase>();
            if (allowDuplicate || count >= allData.Count)
            {
                // 允许重复或请求数量大于等于总数，直接随机选择
                for (int i = 0; i < count; i++)
                {
                    int index = UnityEngine.Random.Range(0, allData.Count);
                    result.Add(allData[index]);
                }
            }
            else
            {
                // 不允许重复且请求数量小于总数，使用洗牌算法
                var shuffled = allData.OrderBy(x => Guid.NewGuid()).Take(count).ToList();
                result.AddRange(shuffled);
            }
    
            return result.AsReadOnly();
        }
        
        /// <summary>
        /// 返回可编辑数据的副本
        /// </summary>
        public List<TDataBaseSB> GetEditableCopySB<TDataBaseSB>() where TDataBaseSB : DataBaseSB
        {
            var readOnlyData = GetAllDataSB<TDataBaseSB>();
            return new List<TDataBaseSB>(readOnlyData);
        }
        /// <summary>
        /// 返回可编辑数据的副本
        /// </summary>
        public List<TDataBase> GetEditableCopy<TDataBase>() where TDataBase : DataBase
        {
            var readOnlyData = GetAllData<TDataBase>();
            return new List<TDataBase>(readOnlyData);
        }
        [Obsolete("注意：数据索引可能因增删而变化，优先使用GetDataSBWhere获取只读副本查询")]
        public bool TryGetDataSBByIndexSB<TDataBaseSB>(int index, out TDataBaseSB result) 
            where TDataBaseSB : DataBaseSB
        {
            result = null;
            if (index < 0) return false;

            var allData = GetAllDataSB<TDataBaseSB>();
            if (index >= allData.Count) return false;

            result = allData[index];
            return true;
        }
        [Obsolete("注意：数据索引可能因增删而变化，优先使用GetDataWhere获取只读副本查询")]
        public bool TryGetDataSBByIndex<TDataBase>(int index, out TDataBase result) 
            where TDataBase : DataBase
        {
            result = null;
            if (index < 0) return false;

            var allData = GetAllData<TDataBase>();
            if (index >= allData.Count) return false;

            result = allData[index];
            return true;
        }

        public override void Initialize()
        {
            dataSBDic.Clear();
            dataDic.Clear();
        }

        public override void Shutdown()
        {
            dataSBDic.Clear();
            dataDic.Clear();
        }

        public override void LifeUpdate()
        {
        }
    }
}