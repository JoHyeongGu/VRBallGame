using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
    [Header("<color=yellow>Weapon</color>")]
    [SerializeField] private Weapon leftWeapon;
    [SerializeField] private Weapon rightWeapon;

    [Header("<color=yellow>XR Controller Inputs</color>")]
    [SerializeField] private InputActionProperty leftTrigger;
    [SerializeField] private InputActionProperty rightTrigger;
    [SerializeField] private InputActionProperty rightJoystick;

    [Header("<color=yellow>Dash</color>")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float dashDistance = 3f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("<color=yellow>Experience System</color>")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int maxExp = 100;
    [SerializeField] private float magnetRadius = 4f;
    [SerializeField] private LayerMask expLayer;

    [Header("<color=yellow>UI</color>")]
    [SerializeField] private UIDocument magnetAreaUI;
    [SerializeField] private UIDocument expOverlayUI;
    [SerializeField] private Color playerUpgradeColor = new Color(0.3f, 0.5f, 0.8f, 1f);

    public List<UpgradeOption> AvailableUpgrades { get; private set; } = new List<UpgradeOption>();

    private bool isDashing = false;
    private float lastDashTime = -999f;
    private Collider[] hitGems = new Collider[20];
    private VisualElement expBarFill;
    private Label levelLabel;

    private void Start()
    {
        InitializeUpgrades();
        UpdateMagnetAreaUI();
        InitializeExpUI();
    }

    private void InitializeUpgrades()
    {
        AvailableUpgrades.Add(new UpgradeOption("Dash Cooldown DOWN", "대시 쿨타임 감소", playerUpgradeColor, () =>
        {
            dashCooldown = Mathf.Max(0.1f, dashCooldown - 0.2f);
        }));

        AvailableUpgrades.Add(new UpgradeOption("Dash Distance UP", "대시 거리 증가", playerUpgradeColor, () =>
        {
            dashDistance += 1f;
        }));

        AvailableUpgrades.Add(new UpgradeOption("Magnet Range UP", "경험치 획득 범위 증가", playerUpgradeColor, () =>
        {
            magnetRadius += 1.5f;
            UpdateMagnetAreaUI();
        }));
    }

    public List<UpgradeOption> GetAllWeaponUpgrades()
    {
        List<UpgradeOption> weaponUpgrades = new List<UpgradeOption>(20);

        bool isSameWeaponType = (leftWeapon != null && rightWeapon != null) &&
                                (leftWeapon.GetType() == rightWeapon.GetType());

        if (isSameWeaponType)
        {
            int upgradeCount = Mathf.Min(leftWeapon.AvailableUpgrades.Count, rightWeapon.AvailableUpgrades.Count);

            for (int i = 0; i < upgradeCount; i++)
            {
                UpgradeOption leftOpt = leftWeapon.AvailableUpgrades[i];
                UpgradeOption rightOpt = rightWeapon.AvailableUpgrades[i];

                weaponUpgrades.Add(new UpgradeOption(
                    leftOpt.Title + " (Dual)",
                    leftOpt.Description,
                    leftOpt.ButtonColor,
                    () =>
                    {
                        leftOpt.ApplyAction?.Invoke();
                        rightOpt.ApplyAction?.Invoke();
                    }
                ));
            }
        }
        else
        {
            if (leftWeapon != null) weaponUpgrades.AddRange(leftWeapon.AvailableUpgrades);
            if (rightWeapon != null) weaponUpgrades.AddRange(rightWeapon.AvailableUpgrades);
        }

        return weaponUpgrades;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;

        if (leftTrigger.action.IsPressed() && leftWeapon != null) leftWeapon.TryAttack();
        if (rightTrigger.action.IsPressed() && rightWeapon != null) rightWeapon.TryAttack();

        CheckDashInput();
        CheckMagnet();
    }


    private void CheckDashInput()
    {
        if (isDashing || Time.time - lastDashTime < dashCooldown) return;

        Vector2 joystickValue = rightJoystick.action.ReadValue<Vector2>();
        if (joystickValue.y > 0.7f)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        Vector3 dashDirection = Camera.main.transform.forward;
        dashDirection.y = 0;
        dashDirection.Normalize();

        StartCoroutine(DashRoutine(dashDirection));
    }

    private IEnumerator DashRoutine(Vector3 direction)
    {
        float timer = 0f;
        float dashSpeed = dashDistance / dashDuration;

        while (timer < dashDuration)
        {
            characterController.Move(direction * dashSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
        isDashing = false;
    }

    private void CheckMagnet()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, magnetRadius, hitGems, expLayer);
        for (int i = 0; i < count; i++)
        {
            if (hitGems[i].TryGetComponent<Exp>(out var exp))
            {
                exp.Magnetize(transform);
            }
        }
    }

    public void AddExp(int amount)
    {
        currentExp += amount;
        if (currentExp >= maxExp)
        {
            LevelUp();
        }
        UpdateExpUI();
    }

    private void LevelUp()
    {
        currentExp -= maxExp;
        currentLevel++;
        maxExp = Mathf.FloorToInt(maxExp * 1.5f);

        UpdateMagnetAreaUI();
        UpdateExpUI();

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ShowUpgradeUI();
        }
    }

    private void UpdateMagnetAreaUI()
    {
        if (magnetAreaUI != null)
        {
            magnetAreaUI.transform.localScale = new Vector3(magnetRadius * 2f, magnetRadius * 2f, 1f);
        }
    }

    private void InitializeExpUI()
    {
        if (expOverlayUI != null && expOverlayUI.rootVisualElement != null)
        {
            expBarFill = expOverlayUI.rootVisualElement.Q<VisualElement>("ExpBarFill");
            levelLabel = expOverlayUI.rootVisualElement.Q<Label>("LevelLabel");
        }
        UpdateExpUI();
    }

    private void UpdateExpUI()
    {
        if (expBarFill != null)
        {
            float percent = Mathf.Clamp01((float)currentExp / maxExp) * 100f;
            expBarFill.style.width = new Length(percent, LengthUnit.Percent);
        }
        if (levelLabel != null)
        {
            levelLabel.text = $"Lv.{currentLevel}";
        }
    }

    public void SetActiveWeapon(bool isActive)
    {
        if (leftWeapon != null) leftWeapon.gameObject.SetActive(isActive);
        if (rightWeapon != null) rightWeapon.gameObject.SetActive(isActive);
    }
}