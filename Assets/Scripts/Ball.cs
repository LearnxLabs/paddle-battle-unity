using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class Ball : NetworkBehaviour
{

    public float minBallSpeed;
    public float maxBallSpeed;
    public bool bounced;
    public bool hasCollided;

    [SerializeField] private AudioClip PointSfx;
    [SerializeField] private List<AudioClip> PaddleSfx;
    [SerializeField] private List<AudioClip> TableSfx;

    private Rigidbody _rb;
    private AudioSource _source;

    // Start is called before the first frame update
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _source = GetComponent<AudioSource>();
    }

    public void AddVelocity(int direction)
    {
        _rb.velocity = new Vector3(Random.Range(-0.4f, 0.4f), 0, direction) * (Random.Range(minBallSpeed, maxBallSpeed)) / 1.8f;
    }

    public IEnumerator Despawn()
    {
        yield return new WaitForSeconds(3);
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    public void RpcPlaySound(string sound)
    {
        switch (sound)
        {
            case "PointZone":
                _source.clip = PointSfx;
                _source.Play();
                break;
            case "Paddle":
                _source.clip = PaddleSfx[Random.Range(0, PaddleSfx.Count - 1)];
                _source.Play();
                break;
            case "Table":
                _source.clip = TableSfx[Random.Range(0, TableSfx.Count - 1)];
                _source.Play();
                break;
            default:
                break;
        }
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "PointZone" && !hasCollided)
        {
            hasCollided = true;
            Player player = other.GetComponentInParent<Player>();
            PaddleBattleManager.Instance.AddPoint(player, bounced);
            RpcPlaySound(other.gameObject.name);
            StartCoroutine(Despawn());
            return;
        }

        if (other.gameObject.name == "Paddle")
        {
            float xOffset = Mathf.Abs(transform.position.x);
            float xDirection = transform.position.x > 0 ? -1 : 1;
            float xSpeed = Random.Range(0, xOffset + 5) - Random.Range(0, 2.5f); 
            bounced = false;
            Player player = other.GetComponentInParent<Player>();
            _rb.velocity = new Vector3(xDirection * xSpeed, 10f, -(player.index * 2 - 1) * Random.Range(minBallSpeed, maxBallSpeed));
            RpcPlaySound(other.gameObject.name);
        }
    }

    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Table")
        {
            bounced = true;
            RpcPlaySound(collision.gameObject.name);
        }
    }
}
