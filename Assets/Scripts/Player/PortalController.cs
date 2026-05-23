using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 异步传送门控制器（按E触发：消散 -> 传送 -> 重构）
/// 防止重复触发
/// </summary>
[RequireComponent(typeof(Collider))]
public class PortalController : MonoBehaviour
{
    [Header("传送点列表")]
    public List<Transform> teleportPoints = new List<Transform>();

    [Header("提示文本")]
    public string tipText = "按 [E] 进行随机传送";

    [Header("场景消散管理器")]
    public DiffusionManager diffusionManager;

    private bool isTeleporting = false;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameEventBus.Publish(new PortalEnterRangeEvent(this));
            UIManager.Instance.ShowInteractTip(tipText);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameEventBus.Publish(new PortalExitRangeEvent(this));
            UIManager.Instance.HideInteractTip();
        }
    }

    /// <summary>
    /// 异步传送玩家：消散 -> 移动 -> 重构
    /// </summary>
    public async UniTask Teleport(Transform player)
    {
        if (isTeleporting) return;
        if (teleportPoints == null || teleportPoints.Count == 0) return;

        isTeleporting = true;

        Transform target = teleportPoints[Random.Range(0, teleportPoints.Count)];

        // Step 1: 播放消散效果
        if (diffusionManager != null)
        {
            diffusionManager.PlayTeleportEffect(player.position);
            await UniTask.Delay(System.TimeSpan.FromSeconds(diffusionManager.duration * 2));
        }

        // Step 2: 移动玩家
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.position = target.position;
            player.rotation = target.rotation;
            cc.enabled = true;
        }
        else
        {
            player.position = target.position;
            player.rotation = target.rotation;
        }

        Debug.Log($"[PortalController] 玩家已传送到 {target.position}");

        isTeleporting = false;
    }
}