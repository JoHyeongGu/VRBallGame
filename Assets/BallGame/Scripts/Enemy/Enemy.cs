using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using DG.Tweening;

public class Enemy : MonoBehaviour
{
    [SerializeField] private GameObject body;
    [SerializeField] private float bodyDieAnimDuration = 0f;
    [SerializeField] private int maxHp = 30;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private UIDocument uiDocument;

    private int currentHp;
    private Collider enemyCollider;
    private Label damageLabel;
    private bool isDead = false;
    private Transform targetPlayer;
    private IObjectPool<Enemy> pool;

    private void Awake()
    {
        if (enemyCollider == null)
        {
            enemyCollider = GetComponent<Collider>();
        }
    }

    public void Init(Transform player, IObjectPool<Enemy> enemyPool)
    {
        targetPlayer = player;
        pool = enemyPool;
        currentHp = maxHp;
        isDead = false;

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
    }

    private void Update()
    {
        if (isDead) return;

        if (targetPlayer != null)
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

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        SpawnManager.Instance.SpawnExpGem(transform.position);

        StartCoroutine(CorpseRoutine());
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