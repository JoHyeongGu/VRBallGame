using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public abstract class Weapon : MonoBehaviour
{
    [Header("<color=yellow>Base Stats</color>")]
    [SerializeField] protected float cooltime = 3f;
    [SerializeField] protected float atkInterval = 0.2f;
    [SerializeField] protected int maxBullet = 10;
    [SerializeField] protected int damage = 10;
    [SerializeField] protected Transform firePoint;

    [Header("<color=yellow>Laser Sight & Raycast</color>")]
    [SerializeField] protected float maxRange = 100f;
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] private bool useLaserSight = true;
    [SerializeField] private Color lineColor = Color.red;
    [SerializeField] private LineRenderer laserLine;

    [Header("<color=yellow>Haptic Feedback</color>")]
    [SerializeField] protected HapticImpulsePlayer hapticPlayer;
    [Range(0, 1)][SerializeField] protected float hapticIntensity = 0.5f;
    [SerializeField] protected float hapticDuration = 0.1f;

    [Header("<color=yellow>Upgrade Settings</color>")]
    [SerializeField] protected Color weaponUpgradeColor = new Color(0.8f, 0.3f, 0.3f, 1f);

    public List<UpgradeOption> AvailableUpgrades { get; private set; } = new List<UpgradeOption>();

    protected int currentBullet;
    protected bool isReloading = false;
    protected float lastFireTime = -9999f;

    protected virtual void Awake()
    {
        if (hapticPlayer == null)
        {
            hapticPlayer = GetComponentInParent<HapticImpulsePlayer>();
        }

        currentBullet = maxBullet;
        InitializeUpgrades();

        if (useLaserSight)
        {
            InitializeLaserSight();
        }
    }

    protected virtual void Update()
    {
        if (useLaserSight)
        {
            UpdateLaserSight();
        }
    }

    private void InitializeLaserSight()
    {
        if (laserLine == null)
        {
            laserLine = GetComponent<LineRenderer>();
            if (laserLine == null) laserLine = gameObject.AddComponent<LineRenderer>();
        }

        laserLine.useWorldSpace = true;
        laserLine.positionCount = 2;
        laserLine.startWidth = 0.005f;
        laserLine.endWidth = 0.005f;
        laserLine.startColor = lineColor;
        laserLine.endColor = lineColor;
    }

    private void UpdateLaserSight()
    {
        if (firePoint == null || laserLine == null) return;

        Vector3 startPos = firePoint.position;
        Vector3 endPos = startPos + (firePoint.forward * maxRange);

        if (Physics.Raycast(startPos, firePoint.forward, out RaycastHit hit, maxRange, enemyLayer))
        {
            endPos = hit.point;
        }

        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, endPos);
    }

    protected abstract void InitializeUpgrades();

    public bool TryAttack()
    {
        if (isReloading || currentBullet <= 0) return false;
        if (Time.time - lastFireTime < atkInterval) return false;

        lastFireTime = Time.time;
        currentBullet--;
        AttackAction();
        TriggerHapticFeedback();

        if (currentBullet <= 0)
        {
            StartCoroutine(ReloadRoutine());
        }
        return true;
    }

    protected abstract void AttackAction();

    protected void TriggerHapticFeedback()
    {
        if (hapticPlayer == null) return;
        hapticPlayer.SendHapticImpulse(hapticIntensity, hapticDuration);
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(0.15f);

        OnReloadStart();
        float timer = 0f;
        while (timer < cooltime)
        {
            timer += Time.deltaTime;
            OnReloadUpdate(timer / cooltime);
            yield return null;
        }

        currentBullet = maxBullet;
        isReloading = false;
        OnReloadComplete();
    }

    protected virtual void OnReloadStart() { }
    protected virtual void OnReloadUpdate(float progress) { }
    protected virtual void OnReloadComplete() { }
}