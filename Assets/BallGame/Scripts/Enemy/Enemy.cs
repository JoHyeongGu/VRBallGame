using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using DG.Tweening;

public class Enemy : MonoBehaviour
{
    [Header("<color=yellow>References</color>")]
    [SerializeField] private GameObject body; //[cite: 1]
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private UIDocument uiDocument; //[cite: 1]

    [Header("<color=yellow>Stats & Settings</color>")]
    [SerializeField] private float bodyDieAnimDuration = 1.5f; //[cite: 1]
    [SerializeField] private int maxHp = 30; //[cite: 1]
    [SerializeField] private float moveSpeed = 2f; //[cite: 1]

    [Header("<color=yellow>Hit & Knockback</color>")]
    [SerializeField] private float hitAnimDuration = 0.2f;
    [SerializeField] private float knockbackDistance = 0.5f;

    private int currentHp; //[cite: 1]
    private Collider enemyCollider; //[cite: 1]
    private Label damageLabel; //[cite: 1]
    private bool isDead = false; //[cite: 1]
    private bool isHit = false;
    private Transform targetPlayer; //[cite: 1]
    private IObjectPool<Enemy> pool; //[cite: 1]
    private Coroutine hitCoroutine;

    private MaterialPropertyBlock propBlock;
    private AnimationSlot currentAnimSlot = AnimationSlot.None;

    private static readonly int currentAnimIndexHash = Shader.PropertyToID("_CurrentAnimIndex");
    private static readonly int transitionStartTimeHash = Shader.PropertyToID("_TransitionStartTime");
    private static readonly int startTime0Hash = Shader.PropertyToID("_StartTime0");
    private static readonly int startTime1Hash = Shader.PropertyToID("_StartTime1");

    // Slot 2를 제거하고 기존 Move(Slot 0)와 Die(Slot 1)만 유지
    private enum AnimationSlot
    {
        None = -1,
        Move = 0,
        Die = 1
    }

    private void Awake() //[cite: 1]
    {
        if (enemyCollider == null)
        {
            enemyCollider = GetComponent<Collider>(); //[cite: 1]
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

    public void Init(Transform player, IObjectPool<Enemy> enemyPool) //[cite: 1]
    {
        targetPlayer = player; //[cite: 1]
        pool = enemyPool; //[cite: 1]
        currentHp = maxHp; //[cite: 1]
        isDead = false; //[cite: 1]
        isHit = false;

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
            hitCoroutine = null;
        }

        if (body != null) //[cite: 1]
        {
            body.SetActive(true); //[cite: 1]
        }

        if (enemyCollider != null) //[cite: 1]
        {
            enemyCollider.enabled = true; //[cite: 1]
        }

        if (uiDocument != null && uiDocument.rootVisualElement != null) //[cite: 1]
        {
            damageLabel = uiDocument.rootVisualElement.Q<Label>("DamageLabel"); //[cite: 1]
            if (damageLabel != null) //[cite: 1]
            {
                damageLabel.style.opacity = 0f; //[cite: 1]
            }
        }

        ChangeAnimationSlot(AnimationSlot.Move);
    }

    private void Update() //[cite: 1]
    {
        if (isDead) return; //[cite: 1]

        if (!isHit && targetPlayer != null)
        {
            Vector3 direction = (targetPlayer.position - transform.position).normalized; //[cite: 1]
            direction.y = 0; //[cite: 1]

            transform.position += direction * moveSpeed * Time.deltaTime; //[cite: 1]

            if (direction != Vector3.zero) //[cite: 1]
            {
                transform.rotation = Quaternion.LookRotation(direction); //[cite: 1]
            }
        }

        if (uiDocument != null) //[cite: 1]
        {
            uiDocument.transform.rotation = Quaternion.LookRotation(uiDocument.transform.position - Camera.main.transform.position); //[cite: 1]
        }
    }

    public void TakeDamage(int damage) //[cite: 1]
    {
        if (isDead) return; //[cite: 1]

        currentHp -= damage; //[cite: 1]
        ShowDamageText(damage); //[cite: 1]

        if (currentHp <= 0) //[cite: 1]
        {
            Die(); //[cite: 1]
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

        // 피격 시 새로운 슬롯 대신 사망 애니메이션(Slot 1)을 재생합니다.
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

        // hitAnimDuration(부분 재생 시간)이 종료된 후 아직 살아있다면 원래 상태(Slot 0)로 복귀합니다.
        if (!isDead)
        {
            isHit = false;
            ChangeAnimationSlot(AnimationSlot.Move);
        }
    }

    private void ShowDamageText(int damage) //[cite: 1]
    {
        if (damageLabel == null) return; //[cite: 1]

        damageLabel.text = damage.ToString(); //[cite: 1]
        damageLabel.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(0), 0f)); //[cite: 1]
        damageLabel.style.opacity = 1f; //[cite: 1]

        float currentY = 0f; //[cite: 1]
        DOTween.To(() => currentY, //[cite: 1]
                   y => //[cite: 1]
                   { //[cite: 1]
                       currentY = y; //[cite: 1]
                       damageLabel.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(y), 0f)); //[cite: 1]
                   }, //[cite: 1]
                   -50f, 0.5f).SetEase(Ease.OutQuart); //[cite: 1]

        float currentAlpha = 1f; //[cite: 1]
        DOTween.To(() => currentAlpha, //[cite: 1]
                   a => //[cite: 1]
                   { //[cite: 1]
                       currentAlpha = a; //[cite: 1]
                       damageLabel.style.opacity = a; //[cite: 1]
                   }, //[cite: 1]
                   0f, 0.5f).SetDelay(0.2f); //[cite: 1]
    }

    private void Die() //[cite: 1]
    {
        isDead = true; //[cite: 1]

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
            hitCoroutine = null;
        }

        if (enemyCollider != null) //[cite: 1]
        {
            enemyCollider.enabled = false; //[cite: 1]
        }

        ChangeAnimationSlot(AnimationSlot.Die);
        SpawnManager.Instance.SpawnExpGem(transform.position); //[cite: 1]
        StartCoroutine(CorpseRoutine()); //[cite: 1]
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