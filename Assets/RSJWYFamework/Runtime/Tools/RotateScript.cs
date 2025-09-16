using UnityEngine;
using System.Collections;

namespace RSJWYFamework.Runtiem
{
    
    public class RotateScript : MonoBehaviour
    {
        // 旋转角度（度）
        public float rotationAngle = 30f;
        // 旋转间隔时间（秒）
        public float rotationInterval = 0.5f;
        // 旋转轴
        public Vector3 rotationAxis = Vector3.up;
    
        // 是否使用协程方式
        public bool useCoroutine = true;

        // 累积时间
        private float timeCounter = 0f;

        void Start()
        {
            if (useCoroutine)
            {
                // 使用协程方式
                StartCoroutine(RotateCoroutine());
            }
        }

        void Update()
        {
            if (!useCoroutine)
            {
                // 使用时间累积方式
                timeCounter += Time.deltaTime;
            
                if (timeCounter >= rotationInterval)
                {
                    // 旋转物体
                    transform.Rotate(rotationAxis, rotationAngle);
                    // 重置计时器
                    timeCounter = 0f;
                }
            }
        }

        IEnumerator RotateCoroutine()
        {
            while (true)
            {
                // 等待指定时间
                yield return new WaitForSeconds(rotationInterval);
            
                // 旋转物体
                transform.Rotate(rotationAxis, rotationAngle);
            }
        }
    }
}