using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RSJWYFamework.Runtime
{
    

    public class GetClickedObjectName : MonoBehaviour
    {
        public Camera camera;
        public float distance = 1000;

        void Update()
        {
            // 检测鼠标左键点击
            if (Input.GetMouseButtonDown(0))
            {
                // 从主摄像机向鼠标点击位置发射射线
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit; // 存储射线碰撞信息

                // 检测射线是否命中物体（最大检测距离可自定义，如1000）
                if (Physics.Raycast(ray, out hit, distance))
                {
                    // 获取被点击物体的名称
                    string clickedObjectName = hit.collider.gameObject.name;
                    Debug.Log("被点击的物体名称：" + clickedObjectName);

                    // 额外：获取物体的标签（可选）
                    string clickedObjectTag = hit.collider.gameObject.tag;
                    Debug.Log("被点击的物体标签：" + clickedObjectTag);
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current);
                eventData.position = Input.mousePosition;

                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                if (results.Count > 0)
                {
                    Debug.Log("射线检测到的第一个元素：" + results[0].gameObject.name);
                }
            }
        }
    }
}