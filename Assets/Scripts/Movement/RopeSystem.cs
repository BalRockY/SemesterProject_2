using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RopeSystem : MonoBehaviour
{
    /*
     * Reference: https://www.raywenderlich.com/348-make-a-2d-grappling-hook-game-in-unity-part-1
     */

    #region Fields
    public GameObject ropeHingeAnchor;
    public DistanceJoint2D ropeJoint;
    public Transform crosshair;
    public SpriteRenderer crosshairSprite;
    public PlayerController playerController;
    private bool ropeAttached;
    private bool distanceSet;
    private Vector2 playerPosition;
    private Rigidbody2D ropeHingeAnchorRb;
    private SpriteRenderer ropeHingeAnchorSprite;
    private Camera _camera;

    // Crosshair distance from player
    public float crosshairDistance = 2f;

    public LineRenderer ropeRenderer;
    public LayerMask ropeLayerMask;

    // Maximum distance the raycast can fire
    private float ropeMaxCastDistance = 20f;

    // Used to track the rope wrapping points
    private List<Vector2> ropePositions = new List<Vector2>();

    #endregion

    #region Methods

    private void Awake()
    {
        _camera = GameObject.Find("CameraRig").GetComponentInChildren<Camera>();
        ropeJoint.enabled = false;
        playerPosition = transform.position;
        ropeHingeAnchorRb = ropeHingeAnchor.GetComponent<Rigidbody2D>();
        ropeHingeAnchorSprite = ropeHingeAnchor.GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        // Capture the world position
        Vector3 worldMousePosition =
            _camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f));

        // Calculate the facing direction
        Vector3 facingDirection = worldMousePosition - transform.position;

        // A representation of the aiming angle
        float aimAngle = Mathf.Atan2(facingDirection.y, facingDirection.x);
        if (aimAngle < 0f)
        {
            aimAngle = Mathf.PI * 2 + aimAngle;
        }

        // Rotation
        Vector3 aimDirection = Quaternion.Euler(0, 0, aimAngle * Mathf.Rad2Deg) * Vector2.right;

        // Temporary variable to store player position
        playerPosition = transform.position;

        // Determine if the rope is attached to an anchor point
        if (!ropeAttached)
        {
            SetCrosshairPosition(aimAngle);
        }
        else
        {
            crosshairSprite.enabled = false;
        }

        HandleInput(aimDirection);

        UpdateRopePositions();
    }

    // This method adds a crosshair at the aimAngle position
    private void SetCrosshairPosition(float aimAngle)
    {
        if (!crosshairSprite.enabled)
        {
            crosshairSprite.enabled = true;
        }

        float x = transform.position.x + crosshairDistance * Mathf.Cos(aimAngle);
        float y = transform.position.y + crosshairDistance * Mathf.Sin(aimAngle);

        Vector3 crossHairPosition = new Vector3(x, y, 0);
        crosshair.transform.position = crossHairPosition;
    }
    private void HandleInput(Vector2 aimDirection)
    {
        
        if (Input.GetMouseButtonDown(0) && playerController.canGrapple)  // Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
        {
            
            if (ropeAttached) return;
            ropeRenderer.enabled = true;

            RaycastHit2D hit = Physics2D.Raycast(playerPosition, aimDirection, ropeMaxCastDistance, ropeLayerMask);


            if (hit.collider != null)
            {
                playerController.grappling = true;
                playerController.canMove = false;
                playerController.doubleJump = false;
                ropeAttached = true;
                
                if (!ropePositions.Contains(hit.point))
                {
                    // Jump slightly to distance the player a little from the ground after grappling to something.
                    transform.GetComponent<Rigidbody2D>().AddForce(new Vector2(0f, 2f), ForceMode2D.Impulse);
                    ropePositions.Add(hit.point);
                    ropeJoint.distance = Vector2.Distance(playerPosition, hit.point);
                    ropeJoint.enabled = true;
                    
                    ropeHingeAnchorSprite.enabled = true;
                }
            }

            else
            {
                ropeRenderer.enabled = false;
                ropeAttached = false;
                ropeJoint.enabled = false;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetRope();
        }
    }

    public void ResetRope() // Jan, (made public) XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    {
        ropeJoint.enabled = false;
        ropeAttached = false;
        playerController.swinging = false;
        ropeRenderer.positionCount = 2;
        ropeRenderer.SetPosition(0, transform.position);
        ropeRenderer.SetPosition(1, transform.position);
        ropePositions.Clear();
        ropeHingeAnchorSprite.enabled = false;

        playerController.DisableMovement(0.1f);
        playerController.doubleJump = true;
        playerController.grappling = false;
    }

    private void UpdateRopePositions()
    {
        
        if (!ropeAttached)
        {
            return;
        }

        ropeRenderer.positionCount = ropePositions.Count + 1;

        for (int i = ropeRenderer.positionCount - 1; i >= 0; i--)
        {
            if (i != ropeRenderer.positionCount - 1)
            {
                ropeRenderer.SetPosition(i, ropePositions[i]);

                if (i == ropePositions.Count - 1 || ropePositions.Count == 1)
                {
                    Vector2 ropePosition = ropePositions[ropePositions.Count - 1];
                    if (ropePositions.Count == 1)
                    {
                        ropeHingeAnchorRb.transform.position = ropePosition;
                        if (!distanceSet)
                        {
                            ropeJoint.distance = Vector2.Distance(transform.position, ropePosition);
                            distanceSet = true;
                        }
                    }
                    else
                    {
                        ropeHingeAnchorRb.transform.position = ropePosition;
                        if (!distanceSet)
                        {
                            ropeJoint.distance = Vector2.Distance(transform.position, ropePosition);
                            distanceSet = true;
                        }
                    }
                }
          
                else if (i - 1 == ropePositions.IndexOf(ropePositions.Last()))
                {
                    Vector2 ropePosition = ropePositions.Last();
                    ropeHingeAnchorRb.transform.position = ropePosition;
                    if (!distanceSet)
                    {
                        ropeJoint.distance = Vector2.Distance(transform.position, ropePosition);
                        distanceSet = true;
                    }
                }
            }
            else
            {
                ropeRenderer.SetPosition(i, transform.position);
            }
        }
    }

    #endregion


}
