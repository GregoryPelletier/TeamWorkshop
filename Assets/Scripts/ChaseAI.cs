using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Put this on an enemy (with Health, MeleeAttack and CharacterLooks).
// It waits near home, notices a target from the other side, walks or flies over, and attacks.
public class ChaseAI : MonoBehaviour
{
    public enum MoveStyle { Walk, Fly, StayPut }

    [Header("Moving")]
    public MoveStyle moveStyle = MoveStyle.Walk;
    public float moveSpeed = 1f;
    [Tooltip("How close (sideways, in units) the target has to be before this notices it.")]
    public float noticeRange = 4f;
    [Tooltip("How far above or below the target can be and still be noticed.")]
    public float noticeHeight = 2.5f;
    [Tooltip("Never goes further than this from where it started.")]
    public float stayNearHome = 4f;

    [Header("Ledge & Wall Detection")]
    [Tooltip("Leave as Nothing to detect any solid Tilemap/collider, or pick your Ground layer.")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float ledgeCheckForwardOffset = 0.2f;
    [SerializeField] private float ledgeCheckDepth = 0.6f;
    [SerializeField] private float wallCheckDistance = 0.15f;

    [Header("Attacking")]
    [Tooltip("Gap between the two bodies (in units) at which it stops and attacks.")]
    public float attackRange = 0.15f;
    [Tooltip("A walker cannot reach a target whose feet are above this share of its own height (on a stump).")]
    public float reachHeight = 0.7f;
    [Tooltip("If the target is this close sideways (right above or below), stand still instead of turning back and forth.")]
    public float standStillGap = 0.3f;

    [Header("Flying only")]
    [Tooltip("How far above the target's feet a flyer likes to be.")]
    public float hoverHeight = 0.9f;
    public float bobAmount = 0.12f;

    public static List<ChaseAI> Active = new List<ChaseAI>();
    public bool IsChasing { get; private set; }
    public Health Target { get; private set; }

    Health myHealth;
    MeleeAttack meleeAttack;
    CharacterLooks myLooks;
    Vector3 homePosition;
    float bobOffset;

    void OnEnable()
    {
        Active.Add(this);
    }

    void OnDisable()
    {
        Active.Remove(this);
        IsChasing = false;
    }

   

    // The nearest living thing on the other side (Neutral things like gates are never chased).
    Health FindTarget()
    {
        Health bestTarget = null;
        float bestDistance = float.MaxValue;
        foreach (Health oneHealth in Health.All)
        {
            if (oneHealth == myHealth || oneHealth.IsDefeated || oneHealth.team == Team.Neutral)
            {
                continue;
            }
            if (myHealth != null && oneHealth.team == myHealth.team)
            {
                continue;
            }
            float thisDistance = Vector2.Distance(oneHealth.transform.position, transform.position);
            if (thisDistance < bestDistance)
            {
                bestDistance = thisDistance;
                bestTarget = oneHealth;
            }
        }
        return bestTarget;
    }

    Bounds MyBox()
    {
        if (myHealth != null)
        {
            return myHealth.BodyBounds();
        }
        return new Bounds(transform.position + Vector3.up * 0.25f, new Vector3(0.4f, 0.5f, 0.1f));
    }

    void Update()
    {
        if (myHealth != null && myHealth.IsDefeated)
        {
            IsChasing = false;
            return;
        }
        if (meleeAttack != null && meleeAttack.IsAttacking)
        {
            return;
        }
        if (Target == null || Target.IsDefeated)
        {
            Target = FindTarget();
        }
        bool isHeld = myHealth != null && myHealth.IsHeld;
        if (Target == null || Target.IsDefeated || isHeld)
        {
            IsChasing = false;
            SetMoving(false);
            if (isHeld && Target != null)
            {
                Face(Target.BodyBounds().center.x > MyBox().center.x);
            }
            return;
        }

        Vector3 targetCenter = Target.BodyBounds().center;
        Vector3 myCenter = MyBox().center;
        float sideDistance = targetCenter.x - myCenter.x;
        float upDistance = targetCenter.y - myCenter.y;
        if (Mathf.Abs(sideDistance) >= noticeRange || Mathf.Abs(upDistance) >= noticeHeight)
        {
            IsChasing = false;
            ReturnHome();
            return;
        }
        IsChasing = true;
        bool rightUnder = Mathf.Abs(sideDistance) < standStillGap;
        if (!rightUnder)
        {
            Face(sideDistance > 0f);
        }

        if (GapToTarget() > attackRange)
        {
            if (rightUnder)
            {
                SetMoving(false);
                return;
            }
            MoveToward(targetCenter);
            return;
        }
        SetMoving(false);
        if (meleeAttack != null)
        {
            meleeAttack.TryAttack();
        }
    }

    // Space between the edge of this body and the edge of the target's body.
    float GapToTarget()
    {
        Bounds myBox = MyBox();
        Bounds targetBox = Target.BodyBounds();
        float sideGap = Mathf.Abs(targetBox.center.x - myBox.center.x) - (targetBox.extents.x + myBox.extents.x);
        if (moveStyle != MoveStyle.Fly)
        {
            // a walker cannot reach a target standing up on a stump above its head
            float reachTop = myBox.min.y + myBox.size.y * reachHeight;
            if (targetBox.min.y > reachTop)
            {
                return 999f;
            }
            return sideGap;
        }
        float upGap = Mathf.Abs(targetBox.center.y - myBox.center.y) - (targetBox.extents.y + myBox.extents.y);
        return Mathf.Max(sideGap, upGap);
    }

    void MoveToward(Vector3 targetPoint)
    {
        if (moveStyle == MoveStyle.StayPut)
        {
            SetMoving(false);
            return;
        }
        Vector3 nextPosition = transform.position;
        float stepSize = moveSpeed * Time.deltaTime;
        float sideDirection = Mathf.Sign(targetPoint.x - MyBox().center.x);
        nextPosition.x += sideDirection * stepSize;

        if (moveStyle == MoveStyle.Fly)
        {
            float wantedHeight = Target.BodyBounds().min.y + hoverHeight + Mathf.Sin(Time.time * 3f + bobOffset) * bobAmount;
            nextPosition.y = Mathf.MoveTowards(transform.position.y, wantedHeight, stepSize);
        }
        else if (WallAhead(sideDirection) || !HasGroundAhead(sideDirection))
        {
            SetMoving(false);
            return;
        }
        if (Mathf.Abs(nextPosition.x - homePosition.x) > stayNearHome)
        {
            SetMoving(false);
            return;
        }
        transform.position = nextPosition;
        SetMoving(true);
    }

    void ReturnHome()
    {
        float homeDistance = homePosition.x - transform.position.x;
        bool awayInHeight = moveStyle == MoveStyle.Fly && Mathf.Abs(homePosition.y - transform.position.y) > 0.05f;
        if (moveStyle == MoveStyle.StayPut || (Mathf.Abs(homeDistance) < 0.05f && !awayInHeight))
        {
            SetMoving(false);
            if (moveStyle == MoveStyle.Fly)
            {
                Vector3 floatPosition = transform.position;
                floatPosition.y = homePosition.y + Mathf.Sin(Time.time * 2f + bobOffset) * bobAmount;
                transform.position = floatPosition;
            }
            return;
        }
        Face(homeDistance > 0f);
        float homeDirection = Mathf.Sign(homeDistance);
        if (moveStyle == MoveStyle.Walk && (WallAhead(homeDirection) || !HasGroundAhead(homeDirection)))
        {
            SetMoving(false);
            return;
        }
        float stepSize = moveSpeed * 0.6f * Time.deltaTime;
        Vector3 nextPosition = transform.position;
        nextPosition.x = Mathf.MoveTowards(nextPosition.x, homePosition.x, stepSize);
        if (moveStyle == MoveStyle.Fly)
        {
            nextPosition.y = Mathf.MoveTowards(transform.position.y, homePosition.y, stepSize);
        }
        transform.position = nextPosition;
        SetMoving(true);
    }

    // Walkers stop at walls: tilemaps, tree trunks, gates, cages, anything solid that is not a character.
    bool WallAhead(float sideDirection)
    {
        Bounds myBox = MyBox();
        float lookDistance = myBox.extents.x + wallCheckDistance;
        Vector2 lowStart = new Vector2(myBox.center.x, myBox.min.y + 0.15f);
        Vector2 midStart = new Vector2(myBox.center.x, myBox.center.y);

        return HitsWall(lowStart, sideDirection, lookDistance) || HitsWall(midStart, sideDirection, lookDistance);
    }

    bool HitsWall(Vector2 rayStart, float sideDirection, float lookDistance)
    {
        RaycastHit2D[] hits = groundLayer.value != 0
            ? Physics2D.RaycastAll(rayStart, new Vector2(sideDirection, 0f), lookDistance, groundLayer)
            : Physics2D.RaycastAll(rayStart, new Vector2(sideDirection, 0f), lookDistance);

        foreach (RaycastHit2D wallHit in hits)
        {
            Collider2D hitCollider = wallHit.collider;
            if (hitCollider == null || hitCollider.isTrigger || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }
            if (Target != null && hitCollider == Target.Body)
            {
                continue;
            }
            if (hitCollider.GetComponentInParent<Tilemap>() != null)
            {
                return true;
            }
            return true;
        }
        return false;
    }

    // Returns true if there is solid ground slightly ahead of the walker's front foot.
    bool HasGroundAhead(float sideDirection)
    {
        if (Mathf.Approximately(sideDirection, 0f)) return true;

        Bounds myBox = MyBox();
        float dir = Mathf.Sign(sideDirection);
        Vector2 rayStart = new Vector2(
    transform.position.x + dir * ledgeCheckForwardOffset,
    myBox.min.y + 0.15f
);

        RaycastHit2D[] hits = groundLayer.value != 0
            ? Physics2D.RaycastAll(rayStart, Vector2.down, ledgeCheckDepth, groundLayer)
            : Physics2D.RaycastAll(rayStart, Vector2.down, ledgeCheckDepth);

        foreach (RaycastHit2D groundHit in hits)
        {
            Collider2D hitCollider = groundHit.collider;
            if (hitCollider == null || hitCollider.isTrigger || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }
            if (Target != null && hitCollider == Target.Body)
            {
                continue;
            }
            return true;
        }
        return false;
    }

    void Face(bool wantRight)
    {
        if (myLooks != null)
        {
            myLooks.Face(wantRight);
        }
    }

    void SetMoving(bool isMoving)
    {
        if (myLooks != null)
        {
            myLooks.SetMoving(isMoving);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (moveStyle != MoveStyle.Walk) return;

        Bounds box = Application.isPlaying
            ? MyBox()
            : new Bounds(transform.position + Vector3.up * 0.25f, new Vector3(0.4f, 0.5f, 0.1f));

        foreach (float dir in new float[] { -1f, 1f })
        {
            // Yellow = ledge check
            Gizmos.color = Color.yellow;
            Vector3 ledgeOrigin = new Vector3(
    transform.position.x + dir * ledgeCheckForwardOffset,
    box.min.y + 0.15f,
    0f
);
            Gizmos.DrawLine(ledgeOrigin, ledgeOrigin + Vector3.down * ledgeCheckDepth);

            // Red = wall check
            Gizmos.color = Color.red;
            float lookDistance = box.extents.x + wallCheckDistance;
            Vector3 wallOrigin = new Vector3(box.center.x, box.center.y, 0f);
            Gizmos.DrawLine(wallOrigin, wallOrigin + Vector3.right * dir * lookDistance);
        }
    }
    void Reset()
    {
        groundLayer = LayerMask.GetMask("Ground");
    }

    void Awake()
    {
        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Ground");
        }

        myHealth = GetComponent<Health>();
        meleeAttack = GetComponent<MeleeAttack>();
        myLooks = GetComponent<CharacterLooks>();
        homePosition = transform.position;
        bobOffset = Random.Range(0f, 6.28f);
    }
}