using UnityEngine;
using System;

public enum GraveFaceResult
{
    Front,    // 赤
    Back,     // 青
    Side,     // 黄
    Vertical  // 緑
}

public class GraveController : MonoBehaviour
{
    [Header("Stop Detection")]
    public float velocityThreshold = 0.05f;
    public float stopTime = 0.3f;

    [Header("Fall Detection")]
    public float fallYThreshold = -3f; // ★ 追加

    Rigidbody rb;
    float stillTimer = 0f;
    bool hasStopped = false;
    bool isInvalid = false; // ★ 追加（落下したか）

    public event Action<GraveController> OnStopped;

    public bool hasGraveSupporting = false;

    void OnCollisionStay(Collision collision)
    {
        // Board は無視
        if (!collision.gameObject.CompareTag("Grave"))
            return;

        foreach (var contact in collision.contacts)
        {
            // 接触法線が「下向き」なら支えられている
            // normal が上を向いている = 相手が下にいる
            float dot = Vector3.Dot(contact.normal, Vector3.up);

            if (dot > 0.5f)
            {
                hasGraveSupporting = true;
                return;
            }
        }
    }

    public bool IsOutOfBoard()
    {
        return transform.position.y < -3f;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (hasStopped) return;

        // =====================
        // ★ Board外落下チェック（最優先）
        // =====================
        if (!isInvalid && transform.position.y < fallYThreshold)
        {
            isInvalid = true;
            hasStopped = true;

            // 物理を止める（これ大事）
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            OnStopped?.Invoke(this);
            return;
        }

        // =====================
        // 通常の停止判定
        // =====================
        if (rb.linearVelocity.magnitude < velocityThreshold &&
            rb.angularVelocity.magnitude < velocityThreshold)
        {
            stillTimer += Time.fixedDeltaTime;
            if (stillTimer >= stopTime)
            {
                hasStopped = true;
                OnStopped?.Invoke(this);
            }
        }
        else
        {
            stillTimer = 0f;
        }
    }

    // =====================
    // 姿勢判定
    // =====================
    public GraveFaceResult GetResult()
    {
        Vector3 worldUp = Vector3.up;

        float dotUp = Vector3.Dot(transform.up, worldUp);
        float dotRight = Vector3.Dot(transform.right, worldUp);
        float dotForward = Vector3.Dot(transform.forward, worldUp);

        float absUp = Mathf.Abs(dotUp);
        float absRight = Mathf.Abs(dotRight);
        float absForward = Mathf.Abs(dotForward);

        // 上下面
        if (absUp > absRight && absUp > absForward)
        {
            return dotUp > 0
                ? GraveFaceResult.Front
                : GraveFaceResult.Back;
        }

        // Z面
        if (absForward > absRight)
            return GraveFaceResult.Side;

        // X面
        return GraveFaceResult.Vertical;
    }

    // =====================
    // 外部から確認用（任意）
    // =====================
    public bool IsInvalid()
    {
        return isInvalid;
    }
}
