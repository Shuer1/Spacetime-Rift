using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image completedIcon;
    [SerializeField] private Button selfBtn;

    public void SetData(Task task, TaskAsset asset, System.Action onClick)
    {
        if (task == null || asset == null)
            return;

        nameText.text = asset.taskName;
        progressText.text = $"{task.currentAmount}/{asset.requiredAmount}";
        completedIcon.gameObject.SetActive(task.state == TaskState.Completed);

        selfBtn.onClick.RemoveAllListeners();
        selfBtn.onClick.AddListener(() => onClick?.Invoke());
    }
}
