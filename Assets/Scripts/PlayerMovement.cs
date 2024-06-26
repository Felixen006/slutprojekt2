using JetBrains.Annotations;
using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    
    // Assignables
    public Transform playerCam;
    public Transform orientation;

    // Movement
    public float moveSpeed = 4500;
    public float maxSpeed = 20;
    public bool grounded;
    public LayerMask whatIsGround;
    public float counterMovement = 0.175f;
    public float groundDrag;
    public float maxSlopeAngle = 35f;
    private float threshold = 0.01f;

    // Crouch & Slide
    public float slideForce = 400;
    public float slideCounterMovement = 0.2f;
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;

    // Jumping
    public float jumpForce = 550f;
    public int maxJumps = 2;
    private bool readyToJump = true;
    [SerializeField]private float jumpCooldown = 0.25f;
    private bool doubleJumped = false;
    private int jumpcount = 0;

    // Rotation and Look
    private float xRotation;
    private float sensitivity = 50f;
    [SerializeField] private float sensMultiplier = 1f;

    // Input
    float x, y;
    bool jumping, sprinting, crouching;

    // Other
    public bool activeGrapple;
    public bool freeze;
    private Rigidbody rb;
    public bool swinging;

    // Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    // Movement control flags
    private bool enableMovementOnNextTouch;
    private Vector3 velocityToSet;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void Update()
    {
        if (activeGrapple) return;

        if (grounded && !activeGrapple)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        if (freeze)
        {
            rb.velocity = Vector3.zero;
        }
        MyInput();
        Look();

        if (grounded)
        {
            doubleJumped = false; // Reset double jump state when grounded
        }
    }

    private void MyInput()
    {
        //if (jumping && !doubleJumped)
        //{
        //    if (jumpcount < maxJumps) // Check if the player hasn't reached the maximum jumps
        //    {
        //        jumpcount++;
        //        Jump();
        //        doubleJumped = true; // Set double jump state
        //    }
        //}

        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetKey(KeyCode.Space);
        crouching = Input.GetKey(KeyCode.LeftControl);

        // Crouching
        if (Input.GetKeyDown(KeyCode.LeftControl))
            StartCrouch();
        if (Input.GetKeyUp(KeyCode.LeftControl))
            StopCrouch();
    }

    private void StartCrouch()
    {
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (rb.velocity.magnitude > 0.5f && grounded)
        {
            rb.AddForce(orientation.transform.forward * slideForce);
        }
    }

    private void StopCrouch()
    {
        transform.localScale = playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }



    private void Movement()
    {
        if (activeGrapple) return;

        // Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10);

        // Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        // Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        // If holding jump && ready to jump, then jump
        if (readyToJump && jumping)
        { 
            Jump(); 
        }

        // Set max speed
        float maxSpeed = this.maxSpeed;

        // If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        // If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        // Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        // Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
    }

    private void Jump()
    {
        //if (true)
        //{
        //    readyToJump = false;
        //
        //    // Add jump forces
        //    rb.AddForce(Vector2.up * jumpForce * 1.5f);
        //    rb.AddForce(normalVector * jumpForce * 0.5f);
        //
        //    // If jumping while falling, reset y velocity
        //    Vector3 vel = rb.velocity;
        //    if (rb.velocity.y < 0.5f)
        //        rb.velocity = new Vector3(vel.x, 0, vel.z);
        //    else if (rb.velocity.y > 0)
        //        rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);
        //
        //    // Reset double jump state if double jump is performed
        //    if (!grounded && !doubleJumped)
        //    {
        //        doubleJumped = true;
        //    }
        //
        //    Invoke(nameof(ResetJump), jumpCooldown);
        //}

        if (grounded)
        {
            readyToJump = false;

            // Add jump forces
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            // If jumping while falling, reset y velocity
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0)
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

        // Find current look rotation
        Vector3 rot = playerCam.transform.localRotation.eulerAngles;
        float desiredX = rot.y + mouseX;

        // Rotate, and also make sure we dont over- or under-rotate
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Perform the rotations
        playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;

        // Slow down sliding
        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        // Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        // Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed)
        {
            float fallspeed = rb.velocity.y;
            Vector3 n = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    private void SetVelocity()
    {
        rb.velocity = velocityToSet;
        enableMovementOnNextTouch = true;
    }

    public void ResetVelocity()
    {
        rb.velocity = Vector3.zero;
    }

    public void ResetRestrictions()
    {
        activeGrapple = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (enableMovementOnNextTouch)
        {
            enableMovementOnNextTouch = false;
            ResetRestrictions();
            GetComponent<Grappling>().StopGrapple();
        }
    }

    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrapple = true;
        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f);
        Invoke(nameof(ResetRestrictions), 3f);
    }

    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);
    
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));
    
        return velocityXZ + velocityY;
    }

    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitude = rb.velocity.magnitude;
        float yMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v)
    {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;

    private void OnCollisionStay(Collision other)
    {
        // Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        // Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            // FLOOR
            if (IsFloor(normal))
            {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        // Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void StopGrounded()
    {
        grounded = false;
    }
}