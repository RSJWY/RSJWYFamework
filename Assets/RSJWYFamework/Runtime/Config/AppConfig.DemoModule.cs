using Sirenix.OdinInspector;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    // 这是一个使用 partial 扩展配置的示例
    // 你可以将此文件放在任何 Runtime 目录下（只要命名空间一致）
    
    public partial class AppConfig
    {
        // 建议1：使用 FoldoutGroup 将模块配置折叠起来，避免主配置面板太乱
        [FoldoutGroup("DemoModule配置")]
        [LabelText("模块开关")]
        public bool DemoModule_IsEnabled = true;

        [FoldoutGroup("DemoModule配置")]
        [LabelText("最大重试次数")]
        public int DemoModule_MaxRetry = 5;

        // 建议2：字段名加上模块前缀（如 DemoModule_），防止与其他模块冲突
        // 建议3：如果是复杂配置，建议定义内部类，但 ScriptableObject 序列化内部类比较麻烦，
        //       通常直接写字段配合 Group 属性是最简单的。
    }
}
