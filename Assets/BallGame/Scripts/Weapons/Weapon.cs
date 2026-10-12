using System.Collections;
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

    [Header("<color=yellow>Haptic Feedback</color>")]
    [SerializeField] protected HapticImpulsePlayer hapticPlayer;
    [Range(0, 1)][SerializeField] protected float hapticIntensity = 0.5f;
    [SerializeField] protected float hapticDuration = 0.1f;

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
    }

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