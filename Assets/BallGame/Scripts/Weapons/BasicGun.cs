using UnityEngine;
using DG.Tweening;

public class BasicGun : Weapon
{
    [Header("<color=yellow>Visual Effects</color>")]
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
        base.Awake(); // Weapon 클래스의 Awake 실행 (조준선 초기화 포함)

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

    protected override void InitializeUpgrades()
    {
        AvailableUpgrades.Add(new UpgradeOption("Max Bullet UP", "최대 탄창 +5", weaponUpgradeColor, () =>
        {
            maxBullet += 5;
            currentBullet += 5;
        }));

        AvailableUpgrades.Add(new UpgradeOption("Damage UP", "공격력 +5", weaponUpgradeColor, () =>
        {
            damage += 5;
        }));

        AvailableUpgrades.Add(new UpgradeOption("Fire Rate UP", "발사 간격 감소", weaponUpgradeColor, () =>
        {
            atkInterval = Mathf.Max(0.05f, atkInterval - 0.05f);
        }));

        AvailableUpgrades.Add(new UpgradeOption("Reload Speed UP", "재장전 시간 감소", weaponUpgradeColor, () =>
        {
            cooltime = Mathf.Max(0.1f, cooltime - 0.3f);
        }));
    }

    protected override void AttackAction()
    {
        Vector3 startPos = firePoint.position;
        Vector3 forwardDir = firePoint.forward;

        if (muzzleFlash != null)
            muzzleFlash.Play();

        // 부모 클래스의 maxRange와 enemyLayer를 그대로 활용
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