using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    [Module] [ModuleDependency(typeof(AppConfigManager))]
    public class ScreenManager:ModuleBase
    {
        // 当前鼠标是否处于捕获锁定状态
        public bool isMouseCaptured = false;
        public override void Initialize()
        {
            var screenConfigJo = ModuleManager.GetModule<AppConfigManager>().GetConfig("Screen");
            var screenJson = JsonConvert.DeserializeObject<ScreenJson>(screenConfigJo.ToString());
            
            AppLogger.Log($"Screen X:{screenJson.ScreenX} Y:{screenJson.ScreenY} Width:{screenJson.ScreenWid} Height:{screenJson.ScreenHei}");
            //Screen.SetResolution(screenJson.ScreenWid, screenJson.ScreenHei, false);
            
            CWinScreen.HandlerInit();
            CWinScreen.SetWindsPos(CWinScreen.windowHandle, screenJson.ScreenX, screenJson.ScreenY, screenJson.ScreenWid, screenJson.ScreenHei);
            CaptureMouse();
        }

        public override void LifeUpdate()
        {
            base.LifeUpdate();
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SetMouseCapture(!isMouseCaptured);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        public override void Shutdown()
        {
        }
        // 在获得/失去应用焦点时自动处理鼠标捕获
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                CaptureMouse();
            }
            else
            {
                ReleaseMouse();
            }
        }

        // 捕获并锁定鼠标使用权（隐藏光标，锁定到窗口中心）
        public void CaptureMouse()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isMouseCaptured = true;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            CWinScreen.BringToFront();
            CWinScreen.CaptureMouseToWindow();
#endif
        }

        // 释放鼠标使用权（显示光标，取消锁定）
        public void ReleaseMouse()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isMouseCaptured = false;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            CWinScreen.ReleaseMouseCapture();
#endif
        }

        // 手动设置是否捕获鼠标（供外部脚本调用）
        public void SetMouseCapture(bool capture)
        {
            if (capture)
            {
                CaptureMouse();
            }
            else
            {
                ReleaseMouse();
            }
        }
    }
    
    public class ScreenJson
    {
        [JsonProperty("X")]
        public int ScreenX { get; private set; }
        [JsonProperty("Y")]
        public int ScreenY { get; private set; }
        [JsonProperty("Width")]
        public int ScreenWid { get; private set; }
        [JsonProperty("Height")]
        public int ScreenHei { get; private set; }
    }
}