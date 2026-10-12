using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("<color=yellow>Target References</color>")]
    [SerializeField] private Player targetPlayer;

    [Header("<color=yellow>Visuals</color>")]
    [SerializeField] private GameObject[] interactVisualObjs;

    [Header("<color=yellow>UI Settings</color>")]
    [SerializeField] private UIDocument upgradeUIDocument;

    private VisualElement rootContainer;
    private Button[] upgradeButtons = new Button[3];
    private Label[] titleLabels = new Label[3];
    private Label[] descLabels = new Label[3];

    private UpgradeOption[] currentOptions = new UpgradeOption[3];
    private List<UpgradeOption> cachedWeaponPool = new List<UpgradeOption>(30);
    private List<UpgradeOption> cachedPlayerPool = new List<UpgradeOption>(30);
    private List<UpgradeOption> finalSelectionPool = new List<UpgradeOption>(5);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeUI();
        SetUIRayActive(false);
    }

    private void InitializeUI()
    {
        if (upgradeUIDocument == null || upgradeUIDocument.rootVisualElement == null) return;

        rootContainer = upgradeUIDocument.rootVisualElement.Q<VisualElement>("Root-Upgrade");
        rootContainer.style.display = DisplayStyle.None;

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            upgradeButtons[i] = rootContainer.Q<Button>($"UpgradeButton{i}");
            titleLabels[i] = upgradeButtons[i].Q<Label>("TitleLabel");
            descLabels[i] = upgradeButtons[i].Q<Label>("DescLabel");

            upgradeButtons[i].clicked += () => SelectUpgrade(index);
        }
    }

    public void ShowUpgradeUI()
    {
        Time.timeScale = 0f;
        SetUIRayActive(true);

        GenerateOptionsFromDomain();
        UpdateUIElements();

        rootContainer.style.display = DisplayStyle.Flex;
        rootContainer.style.opacity = 0f;
        DOTween.To(() => rootContainer.style.opacity.value,
                   x => rootContainer.style.opacity = x,
                   1f, 0.3f).SetUpdate(true);
    }

    private void GenerateOptionsFromDomain()
    {
        cachedWeaponPool.Clear();
        cachedPlayerPool.Clear();
        finalSelectionPool.Clear();

        cachedWeaponPool.AddRange(targetPlayer.GetAllWeaponUpgrades());
        cachedPlayerPool.AddRange(targetPlayer.AvailableUpgrades);

        ShuffleList(cachedWeaponPool);
        ShuffleList(cachedPlayerPool);

        if (cachedWeaponPool.Count >= 2)
        {
            finalSelectionPool.Add(cachedWeaponPool[0]);
            finalSelectionPool.Add(cachedWeaponPool[1]);
        }
        if (cachedPlayerPool.Count >= 1)
        {
            finalSelectionPool.Add(cachedPlayerPool[0]);
        }

        ShuffleList(finalSelectionPool);
        finalSelectionPool.CopyTo(currentOptions);
    }

    private void UpdateUIElements()
    {
        for (int i = 0; i < 3; i++)
        {
            if (i >= finalSelectionPool.Count) break;

            titleLabels[i].text = currentOptions[i].Title;
            descLabels[i].text = currentOptions[i].Description;
            upgradeButtons[i].style.backgroundColor = currentOptions[i].ButtonColor;
        }
    }

    private void SelectUpgrade(int index)
    {
        if (index >= finalSelectionPool.Count) return;

        currentOptions[index].ApplyAction?.Invoke();
        CloseUI();
    }

    private void CloseUI()
    {
        SetUIRayActive(false);

        DOTween.To(() => rootContainer.style.opacity.value,
                   x => rootContainer.style.opacity = x,
                   0f, 0.2f).SetUpdate(true).OnComplete(() =>
                   {
                       rootContainer.style.display = DisplayStyle.None;
                       Time.timeScale = 1f;
                   });
    }

    private void SetUIRayActive(bool isActive)
    {
        foreach (var obj in interactVisualObjs)
        {
            if (obj != null) obj.SetActive(isActive);
        }
        // targetPlayer.SetActiveWeapon(!isActive);
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}