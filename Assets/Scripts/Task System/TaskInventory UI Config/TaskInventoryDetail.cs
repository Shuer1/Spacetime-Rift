using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskInventoryDetail : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI desc;
    [SerializeField] private TextMeshProUGUI progress;
    [SerializeField] private TextMeshProUGUI reward;
    [SerializeField] private Image[] itemIcons;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button submitButton;

    public void Render(Task task, TaskAsset asset)
    {
        if (task == null || asset == null)
            return;

        panelRoot.SetActive(true);

        title.text = asset.taskName;
        desc.text  = string.IsNullOrEmpty(asset.description)
            ? "No description."
            : asset.description;

        progress.text = $"Progress: {task.currentAmount}/{asset.requiredAmount}";

        // Reward text
        string rwText = $"EXP: {asset.exp}    Gold: {asset.gold}";
        if (asset.items != null && asset.items.Count > 0)
        {
            rwText += "\nItems: ";
            rwText += string.Join(" & ",
                asset.items.ConvertAll(it => $"{it.item.name} x{it.count}")
            );
        }
        reward.text = rwText;

        for (int i = 0; i < itemIcons.Length; i++)
        {
            if (i < asset.items.Count)
            {
                itemIcons[i].sprite = asset.items[i].item.icon;
                itemIcons[i].gameObject.SetActive(true);
            }
            else
            {
                itemIcons[i].gameObject.SetActive(false);
            }
        }

        if (submitButton != null)
        { 
            // Real submit condition: task must be active and have reached required amount ✅正式版
            /*
            bool canSubmit =
                task.state == TaskState.Active &&
                task.currentAmount >= asset.requiredAmount;
            */

            // For testing purposes, allow completing any active task ✅测试版
            bool canSubmit = task.state == TaskState.Active;

            submitButton.gameObject.SetActive(canSubmit);

            submitButton.onClick.RemoveAllListeners();
            if (canSubmit)
            {
                submitButton.onClick.AddListener(() => TaskManager.Instance.CompleteTask(task.taskId));
            }
        }
    }
}
