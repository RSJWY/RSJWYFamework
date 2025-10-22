using System;
using UnityEngine;
using RSJWYFamework.Runtime;
using Cysharp.Threading.Tasks;

namespace RSJWYFamework.Test
{
    /// <summary>
    /// 定时任务执行器测试类
    /// 展示定时任务系统的各种使用方式
    /// </summary>
    public class TimerExecutorManagerTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool enableVerboseLogging = true;
        [SerializeField] private float testDelayTime = 2f;
        [SerializeField] private float testIntervalTime = 1f;
        [SerializeField] private int testRepeatCount = 5;

        private TimerExecutorManager _timerExecutorManager;
        private string delayTaskId;
        private string repeatTaskId;
        private string conditionalTaskId;
        private int counter = 0;

        private void Start()
        {
            // 获取定时任务执行器
            _timerExecutorManager = ModuleManager.GetModule<TimerExecutorManager>();
            if (_timerExecutorManager != null)
            {
                _timerExecutorManager.EnableVerboseLogging = enableVerboseLogging;
                Debug.Log("定时任务执行器测试开始");
                
                // 延迟开始测试，确保模块完全初始化
                TimerTaskExtensions.Delay(0.5f, StartTests, "StartTestsDelay");
            }
            else
            {
                Debug.LogError("未找到定时任务执行器模块！请确保TimerExecutor已添加到模块管理器中。");
            }
        }

        private void StartTests()
        {
            TestBasicDelayCall();
            TestRepeatCall();
            TestExtensionMethods();
            TestConditionalTasks();
            TestTaskManagement();
        }

        #region 基础功能测试

        /// <summary>
        /// 测试基础延迟调用
        /// </summary>
        private void TestBasicDelayCall()
        {
            Debug.Log("=== 测试基础延迟调用 ===");
            
            // 使用TimerExecutor直接调用
            delayTaskId = _timerExecutorManager.DelayCall(() =>
            {
                Debug.Log($"延迟任务执行！延迟时间: {testDelayTime}秒");
            }, testDelayTime, "BasicDelayTest");

            Debug.Log($"创建延迟任务，ID: {delayTaskId}");
        }

        /// <summary>
        /// 测试重复调用
        /// </summary>
        private void TestRepeatCall()
        {
            Debug.Log("=== 测试重复调用 ===");
            
            counter = 0;
            repeatTaskId = _timerExecutorManager.RepeatCall(() =>
            {
                counter++;
                Debug.Log($"重复任务执行第 {counter} 次");
                
                if (counter >= testRepeatCount)
                {
                    Debug.Log("重复任务完成！");
                }
            }, 1f, testIntervalTime, testRepeatCount, "RepeatTest");

            Debug.Log($"创建重复任务，ID: {repeatTaskId}，将执行 {testRepeatCount} 次");
        }

        #endregion

        #region 扩展方法测试

        /// <summary>
        /// 测试扩展方法
        /// </summary>
        private void TestExtensionMethods()
        {
            Debug.Log("=== 测试扩展方法 ===");

            // 测试静态便捷方法
            string delayTaskId = TimerTaskExtensions.Delay(3f, () => Debug.Log("静态方法延迟执行"), "StaticDelayTest");
            Debug.Log($"静态延迟方法任务ID: {delayTaskId}");

            TimerTaskExtensions.NextFrame(() => Debug.Log("下一帧执行"));
            
            TimerTaskExtensions.EverySecond(() => Debug.Log("每秒执行"), 3, "EverySecondTest");
            
            // 测试不受时间缩放影响的任务
            TimerTaskExtensions.Delay(2f, () => 
            {
                Debug.Log("不受时间缩放影响的任务执行");
            }, "UnscaledTimeTest", true);
        }

        #endregion

        #region 条件任务测试

        /// <summary>
        /// 测试条件任务
        /// </summary>
        private void TestConditionalTasks()
        {
            Debug.Log("=== 测试条件任务 ===");

            // 等待条件满足
            conditionalTaskId = TimerTaskExtensions.WaitUntil(
                () => counter >= 3, // 等待重复任务执行3次
                () => Debug.Log("条件满足！重复任务已执行3次"),
                0.1f, // 每0.1秒检查一次
                10f,  // 10秒超时
                "ConditionalTest"
            );

            Debug.Log($"创建条件任务，ID: {conditionalTaskId}");

            // 测试WaitWhile
            bool testCondition = true;
            TimerTaskExtensions.Delay(5f, () => testCondition = false, "ChangeCondition");
            
            TimerTaskExtensions.WaitWhile(
                () => testCondition,
                () => Debug.Log("WaitWhile条件任务执行：testCondition变为false"),
                0.2f,
                15f,
                "WaitWhileTest"
            );
        }

        #endregion

        #region 任务管理测试

        /// <summary>
        /// 测试任务管理功能
        /// </summary>
        private void TestTaskManagement()
        {
            Debug.Log("=== 测试任务管理功能 ===");

            // 创建一个可取消的任务
            string cancelTaskId = TimerTaskExtensions.Delay(8f, () =>
            {
                Debug.Log("这个任务不应该执行，因为会被取消");
            }, "CancelTest");

            // 3秒后取消任务
            TimerTaskExtensions.Delay(3f, () =>
            {
                bool cancelled = cancelTaskId.CancelTimer();
                Debug.Log($"取消任务结果: {cancelled}");
            }, "CancelAction");

            // 测试任务信息查询
            TimerTaskExtensions.Delay(1f, () =>
            {
                Debug.Log($"当前活跃任务数量: {_timerExecutorManager.ActiveTaskCount}");
                
                var allTasks = _timerExecutorManager.GetAllActiveTasks();
                Debug.Log("所有活跃任务:");
                foreach (var task in allTasks)
                {
                    Debug.Log($"- {task.TaskName} (ID: {task.TaskId}, 执行次数: {task.CurrentExecuteCount})");
                }
            }, "TaskInfoQuery");
        }

        #endregion

        #region 自定义任务测试

        /// <summary>
        /// 测试自定义任务
        /// </summary>
        private void TestCustomTask()
        {
            Debug.Log("=== 测试自定义任务 ===");
            
            var customTask = new CustomTimerTask("自定义任务测试", 2f, 1f, 3);
            string customTaskId = _timerExecutorManager.AddTask(customTask);
            
            Debug.Log($"创建自定义任务，ID: {customTaskId}");
        }

        #endregion

        #region UI按钮测试方法

        [ContextMenu("测试延迟调用")]
        public void TestDelayCallButton()
        {
            TimerTaskExtensions.Delay(1f, () => Debug.Log("按钮触发的延迟调用"), "ButtonDelayTest");
        }

        [ContextMenu("测试重复调用")]
        public void TestRepeatCallButton()
        {
            TimerTaskExtensions.Repeat(0f, 0.5f, () => Debug.Log("按钮触发的重复调用"), 5, "ButtonRepeatTest");
        }

        [ContextMenu("取消所有任务")]
        public void CancelAllTasksButton()
        {
            _timerExecutorManager?.CancelAllTasks();
            Debug.Log("已取消所有任务");
        }

        [ContextMenu("显示任务信息")]
        public void ShowTaskInfoButton()
        {
            if (_timerExecutorManager != null)
            {
                Debug.Log($"当前活跃任务数量: {_timerExecutorManager.ActiveTaskCount}");
                var tasks = _timerExecutorManager.GetAllActiveTasks();
                foreach (var task in tasks)
                {
                    Debug.Log($"任务: {task.TaskName}, 状态: 完成={task.IsCompleted}, 取消={task.IsCancelled}");
                }
            }
        }

        #endregion

        private void OnDestroy()
        {
            // 清理测试任务
            if (_timerExecutorManager != null)
            {
                _timerExecutorManager.CancelAllTasks();
                Debug.Log("TimerExecutorTest销毁，已取消所有测试任务");
            }
        }
    }

    /// <summary>
    /// 自定义定时任务示例
    /// </summary>
    public class CustomTimerTask : TimerTaskBase
    {
        private int executionCount = 0;

        public CustomTimerTask(string taskName, float delayTime, float intervalTime, int maxExecuteCount)
            : base(taskName, delayTime, intervalTime, maxExecuteCount, false)
        {
        }

        protected override async UniTask OnExecuteAsync()
        {
            executionCount++;
            Debug.Log($"自定义任务执行: {TaskName} - 第{executionCount}次执行");
            
            // 模拟一些异步工作
            await UniTask.Delay(100);
            
            Debug.Log($"自定义任务完成: {TaskName} - 第{executionCount}次完成");
        }
    }
}