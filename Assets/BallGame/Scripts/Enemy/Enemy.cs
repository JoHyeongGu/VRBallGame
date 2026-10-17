using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using DG.Tweening;

public class Enemy : MonoBehaviour
{
    [Header("<color=yellow>References</color>")]
    [SerializeField] private GameObject body;
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private UIDocument uiDocument;

    [Header("<color=yellow>Stats & Settings</color>")]
    [SerializeField] private float bodyDieAnimDuration = 1.5f;
    [SerializeField] private int maxHp = 30;
    [SerializeField] private float moveSpeed = 2f;

    [Header("<color=yellow>Hit & Knockback</color>")]
    [SerializeField] private float hitAnimDuration = 0.2f;
    [SerializeField] private float knockbackDistance = 0.5f;

    private int currentHp;
    private Collider enemyCollider;
    private Label damageLabel;
    private bool isDead = false;
    private bool isHit = false;
    private Transform targetPlayer;
    private IObjectPool<Enemy> pool;
    private Coroutine hitCoroutine;

    private MaterialPropertyBlock propBlock;
    private AnimationSlot currentAnimSlot = AnimationSlot.None;

    private static readonly int currentAnimIndexHash = Shader.PropertyToID("_CurrentAnimIndex");
    private static readonly int transitionStartTimeHash = Shader.PropertyToID("_TransitionStartTime");
    private static readonly int startTime0Hash = Shader.PropertyToID("_StartTime0");
    private static readonly int startTime1Hash = Shader.PropertyToID("_StartTime1");

    private enum AnimationSlot
    {
        None = -1,
        Move = 0,
        Die = 1
    }

    private void Awake()
    {
        if (enemyCollider == null)
        {
            enemyCollider = GetComponent<Collider>();
        }

        InitializePropertyBlock();
    }

    private void InitializePropertyBlock()
    {
        propBlock = new MaterialPropertyBlock();
        if (bodyRenderer != null)
        {
            bodyRenderer.GetPropertyBlock(propBlock);
        }
    }

    public void Init(Transform player, IObjectPool<Enemy> enemyPool)
    {
        targetPlayer = player;
        pool = enemyPool;
        currentHp = maxHp;
        isDead = false;
        isHit = false;

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
            hitCoroutine = null;
        }

        if (body != null)
        {
            body.SetActive(true);
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }

        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            damageLabel = uiDocument.rootVisualElement.Q<Label>("DamageLabel");
            if (damageLabel != null)
            {
                damageLabel.style.opacity = 0f;
            }
        }

        ChangeAnimationSlot(AnimationSlot.Move);
    }

    private void Update()
    {
        if (isDead) return;

        if (!isHit && targetPlayer != null)
        {
            Vector3 direction = (targetPlayer.position - transform.position).normalized;
            direction.y = 0;

            transform.position += direction * moveSpeed * Time.deltaTime;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        if (uiDocument != null)
        {
            uiDocument.transform.rotation = Quaternion.LookRotation(uiDocument.transform.position - Camera.main.transform.position);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHp -= damage;
        ShowDamageText(damage);

        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            if (hitCoroutine != null)
            {
                StopCoroutine(hitCoroutine);
            }
            hitCoroutine = StartCoroutine(HitRoutine());
        }
    }

    private IEnumerator HitRoutine()
    {
        isHit = true;

        ChangeAnimationSlot(AnimationSlot.Die);

        float timer = 0f;
        float speed = knockbackDistance / hitAnimDuration;

        Vector3 knockbackDir = Vector3.zero;
        if (targetPlayer != null)
        {
            knockbackDir = (transform.position - targetPlayer.position).normalized;
            knockbackDir.y = 0;
        }

        while (timer < hitAnimDuration)
        {
            if (isDead) yield break;

            transform.position += knockbackDir * speed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        if (!isDead)
        {
            isHit = false;
            ChangeAnimationSlot(AnimationSlot.Move);
        }
    }

    private void ShowDamageText(int damage)
    {
        if (damageLabel == null) return;

        damageLabel.text = damage.ToString();
        damageLabel.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(0), 0f));
        damageLabel.style.opacity = 1f;

        float currentY = 0f;
        DOTween.To(() => currentY,
                   y =>
                   {
                       currentY = y;
                       damageLabel.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(y), 0f));
                   },
                   -50f, 0.5f).SetEase(Ease.OutQuart);

        float currentAlpha = 1f;
        DOTween.To(() => currentAlpha,
                   a =>
                   {
                       currentAlpha = a;
                       damageLabel.style.opacity = a;
                   },
                   0f, 0.5f).SetDelay(0.2f);
    }

    private void Die()
    {
        isDead = true;

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
            hitCoroutine = null;
        }

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        ChangeAnimationSlot(AnimationSlot.Die);
        SpawnManager.Instance.SpawnExpGem(transform.position);
        StartCoroutine(CorpseRoutine());
    }

    private void ChangeAnimationSlot(AnimationSlot targetSlot)
    {
        if (bodyRenderer == null || currentAnimSlot == targetSlot) return;

        currentAnimSlot = targetSlot;
        float currentTime = Time.timeSinceLevelLoad;

        bodyRenderer.GetPropertyBlock(propBlock);

        propBlock.SetFloat(currentAnimIndexHash, (float)currentAnimSlot);
        propBlock.SetFloat(transitionStartTimeHash, currentTime);

        if (currentAnimSlot == AnimationSlot.Move)
        {
            propBlock.SetFloat(startTime0Hash, currentTime);
        }
        else if (currentAnimSlot == AnimationSlot.Die)
        {
            propBlock.SetFloat(startTime1Hash, currentTime);
        }

        bodyRenderer.SetPropertyBlock(propBlock);
    }

    private IEnumerator CorpseRoutine()
    {
        yield return new WaitForSeconds(bodyDieAnimDuration);

        if (body != null)
        {
            body.SetActive(false);
        }

        float textAnimTime = 0.8f;
        if (bodyDieAnimDuration < textAnimTime)
        {
            yield return new WaitForSeconds(textAnimTime - bodyDieAnimDuration);
        }

        if (pool != null)
        {
            pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}