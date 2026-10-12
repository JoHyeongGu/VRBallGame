using UnityEngine;
using DG.Tweening;

public class BasicGun : Weapon
{
    [Header("<color=yellow>Gun Settings</color>")]
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("<color=yellow>Visual Effects</color>")]
    [SerializeField] private Color lineColor = Color.red;
    [SerializeField] private LineRenderer laserLine;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem hitEffectPrefab;

    [Header("<color=yellow>Reload & Gauge</color>")]
    [SerializeField] private Transform reloadGauge;
    [Range(0, 1)][SerializeField] private float reloadHapticIntensity = 0.05f;
    [SerializeField] private float reloadHapticDuration = 0.05f;

    private ParticleSystem hitEffectInstance;
    private float nextReloadHapticTime;

    protected override void Awake()
    {
        base.Awake();

        if (laserLine == null)
        {
            laserLine = GetComponent<LineRenderer>();
            if (laserLine == null) laserLine = gameObject.AddComponent<LineRenderer>();
        }
        SetupLaserLine();

        if (hitEffectPrefab != null)
        {
            hitEffectInstance = Instantiate(hitEffectPrefab);
            hitEffectInstance.Stop();
        }

        if (reloadGauge != null)
        {
            reloadGauge.localScale = new Vector3(reloadGauge.localScale.x, reloadGauge.localScale.y, 1f);
        }
    }

    private void Update()
    {
        UpdateLaserSight();
    }

    private void SetupLaserLine()
    {
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

    protected override void AttackAction()
    {
        Vector3 startPos = firePoint.position;
        Vector3 forwardDir = firePoint.forward;

        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (Physics.Raycast(startPos, forwardDir, out RaycastHit hit, maxRange, enemyLayer))
        {
            if (hit.collider.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.TakeDamage(damage);
                if (hitEffectInstance != null)
                {
                    hitEffectInstance.transform.position = hit.point;
                    hitEffectInstance.transform.forward = hit.normal;
                    hitEffectInstance.Play();
                }
            }
        }

        if (reloadGauge != null)
        {
            float targetZ = (float)currentBullet / maxBullet;
            reloadGauge.DOScaleZ(targetZ, 0.1f);
        }
    }

    protected override void OnReloadStart()
    {
        if (reloadGauge != null)
        {
            reloadGauge.DOScaleZ(1f, cooltime).SetEase(Ease.Linear);
        }
        nextReloadHapticTime = Time.time;
    }

    protected override void OnReloadUpdate(float progress)
    {
        if (Time.time >= nextReloadHapticTime)
        {
            if (hapticPlayer != null)
            {
                hapticPlayer.SendHapticImpulse(reloadHapticIntensity, reloadHapticDuration);
            }
            nextReloadHapticTime = Time.time + 0.08f;
        }
    }
}