using System.Collections;
using UnityEngine;

// Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
public delegate void HandleMyJumps();
// Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

public class PlayerController : MonoBehaviour {
    /* References:
         * Better Jumping in 4 lines of code:
         * https://www.youtube.com/watch?v=7KiK0Aqtmzc&ab_channel=BoardToBitsGames
         * 
         * Celeste's Movement | Mix and Jam:
         * https://youtu.be/STyY26a_dPY
         * 
     */
    #region Fields
    
    [Header("Components")]
    [HideInInspector]
    public Rigidbody2D _rb;
    private Collision _col;
    private SpriteRenderer _spr;
    public Sprite _sprOnGround;
    public Sprite _sprInAir;
    private Camera _camera;
    private GameObject _player; // Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    private DistanceJoint2D _distanceJoint;
    private LineRenderer _lineRenderer;
    public Animator _animator;
    public RopeSystem ropeSystem;

    [Space]
    [Header("Variables")]
    public float _speed = 5f;
    public float _jumpForce = 5f;
    public float _wallJumpLerp = 1f;
    public float _climbSpeed = 1f;
    public float _levitationTime = 3f;       

    // Better movement and jumping -
    //  Manupulating gravity when falling, to make a better jump feel
    //  Adding coyote time allowing slightly delayed jumping (inspired coyote and roadrunner)
    public float _gravityMultiplier = 2.5f;
    public float _lowJumpMultiplier = 2f;
    public float _coyoteTime = 0.5f;

    [Space]
    [Header("Booleans")]
    public bool smoothMovement;

    // Allowing mechanics
    public bool canMove;
    public bool canWallGrab;
    public bool canWallJump;
    public bool canDoubleJump;
    public bool canGrapple;

    [Space]
    // Used for methods
    public bool wallGrab;
    public bool wallJump;
    public bool doubleJump;
    public bool grappling;
    public bool swinging;

    [Space]
    [Header("Misc")]
    public int facingDir = 1;
    public Vector2 jumpDir;
    private Vector3 _mousePos;
    private float ct;           // Temporary coyote time
    private float lt;           // Temporary levitation time
    private Vector3 _temPos;    // Temporary mouse position (used for grappling hook)
    #endregion

    // Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public event HandleMyJumps I_Jumped;
    // Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

    private void Awake()
    {
        _camera = GameObject.Find("CameraRig").GetComponentInChildren<Camera>();
        _player = GameObject.FindGameObjectWithTag("Avatar_P0"); // Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collision>();
        _spr = GetComponentInChildren<SpriteRenderer>();
        ropeSystem = GetComponent<RopeSystem>();
                
    }
    void Update()
    {

        #region Input
        float xDir = Input.GetAxis("Horizontal");
        float yDir = Input.GetAxis("Vertical");

        // Returns either -1 or +1 and doesn't smooth the value
        float xDirRaw = Input.GetAxisRaw("Horizontal");
        float yDirRaw = Input.GetAxisRaw("Vertical");
        _animator.SetFloat("HorizontalAnim", xDirRaw);
        _animator.SetFloat("yVerlocity", _rb.velocity.y);

        Vector2 dir;

        if (smoothMovement == true)
        {
            dir = new Vector2(xDir, yDir);
        }
        else
        {
            dir = new Vector2(xDirRaw, yDirRaw);
        }

        #endregion

        #region Movement
        if (canMove == true)
        {
            Move(dir);
        }  
              
       
        if (xDirRaw < 0)
        {
            _spr.flipX = true;
        }
        else if (xDirRaw > 0)
        {
            _spr.flipX = false;
        }
        #endregion

        #region Check collisions

        // If player touches the ground
        if (_col.onGround && !grappling)
        {
            ct = _coyoteTime;
            canMove = true;
            wallJump = false;
            doubleJump = true;
        }

        // If player touches the wall and other parameters
        if (_col.onWall && !grappling)
        {
            canMove = true;
        }

        if (_col.onWall && canMove && canWallGrab)
        {
            wallGrab = true;
        }

        if (!_col.onWall || !canWallGrab)
        {
            wallGrab = false;
        }



        

        // Wall Grabbing & Climbing
        if (wallGrab && Input.GetKey(KeyCode.LeftShift))
        {
            _rb.gravityScale = 0;

            float climbSpeedMod = yDir > 0 ? .5f * _climbSpeed : _climbSpeed;

            _rb.velocity = new Vector2(0, yDir * (_speed * climbSpeedMod));
            _animator.SetBool("WallClimp", true);
        }
        /*
        else if (!wallGrab && Input.GetKey(KeyCode.LeftShift) && !grappling)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, yDir * 0);
        } */
        else
        {
            _rb.gravityScale = 1;
            _animator.SetBool("WallClimp", false);
        }

        if (grappling)
        {
            SwingMove(dir);
        }

        #endregion

        #region Simple animation

        // Simple solution to changing sprites when in air (Need to change for proper animation handling
        if (!_col.onGround && !_col.onWall)
        {
            _animator.SetBool("notOnGround", true);
        }
        else
        {
            _animator.SetBool("notOnGround", false);
        }
        #endregion


        #region Jumping
        if (Input.GetButtonDown("Jump"))
        {
            // Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
            //I_Jumped();
            // Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

            if (_col.onGround)
            {
                Jump(Vector2.up);
            }
            if (_col.onWall && !_col.onGround)
            {
                WallJump(dir);
            }
            if (!_col.onGround && doubleJump && canDoubleJump && !wallJump)
            {
                DoubleJump(Vector2.up);
            }
        }

        // Better Jumping by manipulating gravity at different velocities
        if (_rb.velocity.y < 0)
        {
            _rb.velocity += Vector2.up * Physics2D.gravity.y * (_gravityMultiplier - 1) * Time.deltaTime;
        }
        else if (_rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            _rb.velocity += Vector2.up * Physics2D.gravity.y * (_gravityMultiplier - 1) * Time.deltaTime;
        }
        #endregion
               
    }


    // Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    public void Avatar_GrappleOn() {
        canGrapple = true;
    }
    public void Avatar_GrappleOff() {
        canGrapple = false;
        _player.GetComponent<RopeSystem>().ResetRope();
    }
    // Jan, XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX


    #region Misc Methods
    private void Move(Vector2 dir)
    {
        if (!wallJump || _col.onGround)
        {
            _rb.velocity = new Vector2(dir.x * _speed, _rb.velocity.y);
        }
    }
    private void Jump(Vector2 dir)
    {
        I_Jumped();
        _rb.velocity = new Vector2(_rb.velocity.x, 0);
        _rb.velocity += dir * _jumpForce;
    }
    private void WallJump(Vector2 dir)
    {
        wallJump = true;
        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(.1f));

        Vector2 wallDir = _col.onWallRight ? Vector2.left : Vector2.right;
        if (wallDir == Vector2.left)
        {
            _spr.flipX = true;
        }
        else
        {
            _spr.flipX = false;
        }
        Jump((Vector2.up / 1.5f + wallDir / 1.5f));        
       
    }
    private void DoubleJump(Vector2 dir)
    {
        Jump(dir);
        doubleJump = false;
    }
    
    public IEnumerator DisableMovement(float time)
    {
        canMove = false;
        canWallGrab = false;
        yield return new WaitForSeconds(time);
        canMove = true;
        canWallGrab = true;
    }

    public void SwingMove(Vector2 dir)
    {
        _rb.AddForce(new Vector2(dir.x,0),ForceMode2D.Force);
        GetComponent<DistanceJoint2D>().distance -= dir.y * 0.1f;
    }

    #endregion
}
