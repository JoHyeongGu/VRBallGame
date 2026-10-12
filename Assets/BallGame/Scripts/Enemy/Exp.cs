using UnityEngine;
using UnityEngine.Pool;

public class Exp : MonoBehaviour
{
    [SerializeField] private int expAmount = 10;
    [SerializeField] private float accel = 15f;
    [SerializeField] private float initialSpeed = 2f;

    private Transform target;
    private float currentSpeed = 0f;
    private IObjectPool<Exp> pool;

    public void Init(Vector3 position, IObjectPool<Exp> expPool)
    {
        transform.position = position;
        pool = expPool;
        target = null;
        currentSpeed = 0f;
    }

    public void Magnetize(Transform player)
    {
        if (target == null)
        {
            target = player;
            currentSpeed = initialSpeed;
        }
    }

    private void Update()
    {
        if (target != null)
        {
            currentSpeed += accel * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target.position, currentSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.2f)
            {
                target.GetComponent<Player>().AddExp(expAmount);
                if (pool != null) pool.Release(this);
                else Destroy(gameObject);
            }
        }
    }
}