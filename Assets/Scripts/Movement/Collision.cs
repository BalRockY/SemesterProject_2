using UnityEngine;

public class Collision : MonoBehaviour
{
    [Header("Layers")]
    public LayerMask _staticCol;    // Static Collisions reference to ground, wall, ceiling and other areas of non traversal.    

    [Space]
    [Header("Booleans")]
    public bool onStatic;
    public bool onGround;
    public bool onWall;
    public bool onWallRight;
    public bool onWallLeft;

    [Space]
    [Header("Misc")]
    public Vector2 capsuleSize;
    public float smallCollisionRadius;
    public float bigCollisionRadius;
    private float _scale;
    public Vector2 bigBottomOffset, bottomOffset, rightOffset, leftOffset;

    private void Awake()
    {
        // Setting circle colliders at specific locations defined in the inspector,
        //  and scaling the positions and radius according to the Player object's scale

        _scale = this.transform.localScale.x;

        bigCollisionRadius = bigCollisionRadius * _scale;
        bigBottomOffset = new Vector2(bigBottomOffset.x * _scale, bigBottomOffset.y * _scale);

        capsuleSize = capsuleSize * _scale;
        smallCollisionRadius = smallCollisionRadius * _scale;
        bottomOffset = new Vector2(bottomOffset.x * _scale, bottomOffset.y * _scale);
        rightOffset = new Vector2(rightOffset.x * _scale, rightOffset.y * _scale);
        leftOffset = new Vector2(leftOffset.x * _scale, leftOffset.y * _scale);
    }

    private void Update()
    {
        // Checking if plater touches either ground or wall
        onStatic = Physics2D.OverlapCircle((Vector2)transform.position + bigBottomOffset, bigCollisionRadius, _staticCol);

        // Checking if player is on the Ground layer
        onGround = Physics2D.OverlapCircle((Vector2)transform.position + bottomOffset, smallCollisionRadius, _staticCol);
        Physics2D.OverlapCapsule((Vector2)transform.position + bottomOffset, capsuleSize, CapsuleDirection2D.Vertical,0, _staticCol);

        // Checking if player is on a wall on either its left or right side
        onWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, smallCollisionRadius, _staticCol) ||
            Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, smallCollisionRadius, _staticCol);

        onWallRight = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, smallCollisionRadius, _staticCol);
        onWallLeft = Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, smallCollisionRadius, _staticCol);
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        var positions = new Vector2[] { bigBottomOffset, bottomOffset, rightOffset, leftOffset };

        Gizmos.DrawWireSphere((Vector2)transform.position + bigBottomOffset, bigCollisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + bottomOffset, smallCollisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + rightOffset, smallCollisionRadius);
        Gizmos.DrawWireSphere((Vector2)transform.position + leftOffset, smallCollisionRadius);
               

    }
}
