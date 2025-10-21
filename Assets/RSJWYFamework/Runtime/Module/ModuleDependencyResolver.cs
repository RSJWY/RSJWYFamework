using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RSJWYFamework.Runtime
{
    /// <summary>
    /// 模块依赖特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ModuleDependencyAttribute : Attribute
    {
        public Type DependencyType { get; }
        
        public ModuleDependencyAttribute(Type dependencyType)
        {
            DependencyType = dependencyType;
        }
    }
    
    /// <summary>
    /// 模块依赖解析器
    /// </summary>
    public static class ModuleDependencyResolver
    {
        private static readonly Dictionary<Type, List<Type>> _dependencies = new();
        private static readonly Dictionary<Type, int> _initializationOrder = new();
        private static bool _isInitialized = false;
        
        /// <summary>
        /// 初始化依赖关系
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            
            _dependencies.Clear();
            _initializationOrder.Clear();
            
            // 扫描所有模块类型
            var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IModule).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .ToList();
            
            // 构建依赖关系图
            foreach (var moduleType in moduleTypes)
            {
                var dependencies = moduleType.GetCustomAttributes<ModuleDependencyAttribute>()
                    .Select(attr => attr.DependencyType)
                    .ToList();
                
                _dependencies[moduleType] = dependencies;
            }
            
            // 计算初始化顺序
            CalculateInitializationOrder(moduleTypes);
            _isInitialized = true;
        }
        
        /// <summary>
        /// 获取模块的依赖项
        /// </summary>
        public static List<Type> GetDependencies(Type moduleType)
        {
            Initialize();
            return _dependencies.TryGetValue(moduleType, out var deps) ? deps : new List<Type>();
        }
        
        /// <summary>
        /// 获取模块的初始化顺序
        /// </summary>
        public static int GetInitializationOrder(Type moduleType)
        {
            Initialize();
            return _initializationOrder.TryGetValue(moduleType, out var order) ? order : 0;
        }
        
        /// <summary>
        /// 获取按初始化顺序排序的模块类型列表
        /// </summary>
        public static List<Type> GetOrderedModuleTypes()
        {
            Initialize();
            return _initializationOrder.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        }
        
        /// <summary>
        /// 验证依赖关系是否有效（无循环依赖）
        /// </summary>
        public static bool ValidateDependencies()
        {
            Initialize();
            
            foreach (var moduleType in _dependencies.Keys)
            {
                if (HasCircularDependency(moduleType, new HashSet<Type>()))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 计算初始化顺序（拓扑排序）
        /// </summary>
        private static void CalculateInitializationOrder(List<Type> moduleTypes)
        {
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>();
            var order = 0;
            
            foreach (var moduleType in moduleTypes)
            {
                if (!visited.Contains(moduleType))
                {
                    VisitModule(moduleType, visited, visiting, ref order);
                }
            }
        }
        
        /// <summary>
        /// 深度优先遍历模块依赖
        /// </summary>
        private static void VisitModule(Type moduleType, HashSet<Type> visited, HashSet<Type> visiting, ref int order)
        {
            if (visiting.Contains(moduleType))
            {
                throw new InvalidOperationException($"检测到循环依赖: {moduleType.Name}");
            }
            
            if (visited.Contains(moduleType))
            {
                return;
            }
            
            visiting.Add(moduleType);
            
            // 先访问所有依赖项
            if (_dependencies.TryGetValue(moduleType, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    VisitModule(dependency, visited, visiting, ref order);
                }
            }
            
            visiting.Remove(moduleType);
            visited.Add(moduleType);
            _initializationOrder[moduleType] = order++;
        }
        
        /// <summary>
        /// 检查是否存在循环依赖
        /// </summary>
        private static bool HasCircularDependency(Type moduleType, HashSet<Type> visited)
        {
            if (visited.Contains(moduleType))
            {
                return true;
            }
            
            visited.Add(moduleType);
            
            if (_dependencies.TryGetValue(moduleType, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (HasCircularDependency(dependency, visited))
                    {
                        return true;
                    }
                }
            }
            
            visited.Remove(moduleType);
            return false;
        }
    }
}