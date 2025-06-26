using System.Collections;
using UnityEngine;

namespace Assets.RSJWYFamework.Runtiem.Utiltiy
{
    public static partial class Utility
    {
        public static class GameObjectTool
        {
            /// <summary>
            /// 克隆实例
            /// </summary>
            /// <typeparam name="T">实例类型</typeparam>
            /// <param name="original">初始对象</param>
            /// <returns>克隆的新对象</returns>
            public static T Clone<T>(T original) where T : UnityEngine.Object
            {
                return Object.Instantiate(original);
            }
            /// <summary>
            /// 克隆实例
            /// </summary>
            /// <typeparam name="T">实例类型</typeparam>
            /// <param name="original">初始对象</param>
            /// <param name="position">新对象的位置</param>
            /// <param name="rotation">新对象的旋转</param>
            /// <returns>克隆的新对象</returns>
            public static T Clone<T>(T original, Vector3 position, Quaternion rotation) where T : UnityEngine.Object
            {
                return Object.Instantiate(original, position, rotation);
            }
            /// <summary>
            /// 克隆实例
            /// </summary>
            /// <typeparam name="T">实例类型</typeparam>
            /// <param name="original">初始对象</param>
            /// <param name="position">新对象的位置</param>
            /// <param name="rotation">新对象的旋转</param>
            /// <param name="parent">新对象的父物体</param>
            /// <returns>克隆的新对象</returns>
            public static T Clone<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : UnityEngine.Object
            {
                return Object.Instantiate(original, position, rotation, parent);
            }
            /// <summary>
            /// 克隆实例
            /// </summary>
            /// <typeparam name="T">实例类型</typeparam>
            /// <param name="original">初始对象</param>
            /// <param name="parent">新对象的父物体</param>
            /// <returns>克隆的新对象</returns>
            public static T Clone<T>(T original, Transform parent) where T : UnityEngine.Object
            {
                return Object.Instantiate(original, parent);
            }
            /// <summary>
            /// 克隆实例
            /// </summary>
            /// <typeparam name="T">实例类型</typeparam>
            /// <param name="original">初始对象</param>
            /// <param name="parent">新对象的父物体</param>
            /// <param name="worldPositionStays">是否保持世界位置不变</param>
            /// <returns>克隆的新对象</returns>
            public static T Clone<T>(T original, Transform parent, bool worldPositionStays) where T : UnityEngine.Object
            {
                return Object.Instantiate(original, parent, worldPositionStays);
            }
            /// <summary>
            /// 克隆 GameObject 实例
            /// </summary>
            /// <param name="original">初始对象</param>
            /// <param name="isUI">是否是UI对象</param>
            /// <returns>克隆的新对象</returns>
            public static GameObject CloneGameObject(GameObject original, bool isUI = false)
            {


                GameObject obj = Object.Instantiate(original, original.transform.parent, true);
                if (isUI)
                {
                    RectTransform rect = obj.GetComponent<RectTransform>();
                    RectTransform originalRect = original.GetComponent<RectTransform>();
                    rect.anchoredPosition3D = originalRect.anchoredPosition3D;
                    rect.sizeDelta = originalRect.sizeDelta;
                    rect.offsetMin = originalRect.offsetMin;
                    rect.offsetMax = originalRect.offsetMax;
                    rect.anchorMin = originalRect.anchorMin;
                    rect.anchorMax = originalRect.anchorMax;
                    rect.pivot = originalRect.pivot;
                }
                else
                {
                    obj.transform.localPosition = original.transform.localPosition;
                }
                obj.transform.localRotation = original.transform.localRotation;
                obj.transform.localScale = original.transform.localScale;
                obj.SetActive(true);
                return obj;
            }

            /// <summary>
            /// 销毁物体
            /// </summary>
            /// <param name="gameObject"></param>
            public static void Destory(GameObject gameObject)
            {
                Object.Destroy(gameObject);
            }
        }
    }
}