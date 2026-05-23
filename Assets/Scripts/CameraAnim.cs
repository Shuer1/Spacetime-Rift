using UnityEngine;
using Cysharp.Threading.Tasks;

public class CameraAnim : MonoBehaviour
{
    [Header("旋转速度（度/秒）")]
    public float rotationSpeed = 10f;

    private bool isRotating = true;

    private void OnEnable()
    {
        SFXManager.Instance.PlayEventSFX("OverallBGM");
        // 启动旋转协程
        RotateCameraAsync().Forget();
    }

    private void OnDisable()
    {
        isRotating = false;
    }

    private async UniTaskVoid RotateCameraAsync()
    {
        while (isRotating)
        {
            // 绕Y轴旋转，每帧旋转量 = rotationSpeed * deltaTime
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            
            // 等待下一帧
            await UniTask.Yield();
        }
    }
}