using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb; //defined in inspector
    public Animator myAnimator; //defined in inspector
    public Animator vacAnimator; //defined in inspector
    public Material BodyMaterial; //defined in inspector
    public Material HeadMaterial; //defined in inspector
    public VisualEffect DustVFX; //defined in inspector
    public VisualEffect SuckVFX; //defined in inspector
    public GameObject DrillObject; //defined in inspector
    public GameObject FireObject; //defined in inspector
    public GameObject VacObject; //defined in inspector
    public GameObject vacCheck; //defined in inspector
    public GameObject collector; //defined in inspector
    public GameObject itemspawn; //defined in inspector


    [Header("Movement")]
    public float maxMoveSpeed = 4f; //could upgrade  
    public float horizontalDirection; // other scripts need access    float 
    float smoothtime = 0.3f;    
    float xMagnitude;
    float RotationMultiplier = 3f;
    public float verticalDirection; // other scripts need access
    float currentxvelocity = 0;    
    bool Flipping;
    float isFacingRight = 1f;
    public float jumpPower = 3f; //could upgrade  
    int maxJumps = 1;
    int jumpsRemaining;
    float baseGravity = 1f;
    float maxFallSpeed = 9f;
    float fallSpeedMultiplier = 2f;
    public float flyPower = 3f;
    float flyGravity = 0.1f;
    public bool isFlying; // other scripts need access
    float startFly;

    [Header("SquashStretch")]
    float stretchAmount = 1f;
    public float squashMagnitude;    // other scripts need access
    float squashAmount = 0.5f;    
    float impactt;
    float impactStartTime;
    public AnimationCurve impactAnimation; //defined in inspector
    float impactDuration = 1f;
    float curveValue;
    float impactbeat;

    [Header("Music")] // should probably be moved to a seperate script
    public float musicBeat; // other scripts need access
    public float musicBounce; // other scripts may need access
    public float BPM = 130f; // other scripts need access
    float animationDuration = 1f;
    float angularFrequency;
    

    [Header("Tools")]    
    public bool isDrilling = false; // other scripts need access
    float Drilling = 0;
    float DrillposX;
    float DrillposY;
    public Vector2 Drillvector; // other scripts need access
    public float drill; // other scripts need access
    float lerpDrill;
    float initialDrill= 0;
    float DrillstartTime;
    float duration = 1f;
    float drillingT;
    bool isVacuuming;
    public float VacMotion; // other scripts need access  

    [Header("Inventory")]
    int inventory = 0;
    float inventoryAdjustment;
    public float carryStrength= 1f; //could upgrade
    float inventoryAdjustedMaxSpeed;
    public int maxInventory= 4; //could upgrade    

    [Header("Sensors")]
    public Transform groundCheckPos; //defined in inspector
    Vector2 groundCheckSize = new Vector2(0.3f, 0.16f);
    public LayerMask groundLayer; //defined in inspector
    public bool isGrounded; // other scripts need access    
    public Transform wallCheckPos; //defined in inspector
    Vector2 wallCheckSize = new Vector2(0.17f, 0.28f);
    bool isTouchingWall;
    float aiming;
    public Transform toolCheckPos; //defined in inspector
    public Transform drillSensor;
    public Vector2 toolCheckSize = new Vector2(0.25f, 0.25f);

    [Header("Customization")] // should probably be moved to a seperate script
    public int BaseColour = 1; //set default
    public int AccentColour = 1; //set default
    public int HeadColour = 1; //set default

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        
        isFlying = false;        
        impactStartTime = 0;
        DrillObject.SetActive(false);
        VacObject.SetActive(true);
        DrillstartTime = 0;
        FireObject.SetActive(false);
        SuckVFX.Stop();
        vacCheck.SetActive(false);
        collector.SetActive(false);

    }


    // Update is called once per frame
    void Update()
    {
        GroundCheck();        
        WallPushCheck();
        Flip();         
        MusicBeat();
        Flying();
        ProcessDrillingMotion();
        ProcessVacMotion();
        ProcessInventoryAdjustment();


        myAnimator.SetFloat("yVelocity", rb.linearVelocity.y);
        myAnimator.SetFloat("magnitude", Mathf.Abs(rb.linearVelocity.x) / inventoryAdjustedMaxSpeed);        
        myAnimator.SetBool("flip", Flipping);
        vacAnimator.SetBool("suck", isVacuuming);
        vacAnimator.SetFloat("aim", aiming);
        BodyMaterial.SetFloat("_Base_Colour", BaseColour);
        BodyMaterial.SetFloat("_Accent_Colour", AccentColour);
        HeadMaterial.SetFloat("_Head_Colour", HeadColour);

    }

    void FixedUpdate() // AG Moved this stuff to fixed update to try and fix the camera jitter
    {
        ProcessStretch();
        ProcessXMovement();
        ProcessGravity();
    }

    private void OnEnable()
    {
        item_script.OnItemCollected += IncrementItemCount;
    }
    private void OnDisable()
    {
        item_script.OnItemCollected -= IncrementItemCount;
    }
    private void IncrementItemCount()
    {
        inventory++;
        
    }
        private void Flying()
    {
        if (isFlying)
        {
            FireObject.SetActive(true);
            
            DustVFX.Play();
            startFly = Time.time;

        }
        else
        {
            float FlyTime = Time.time - startFly;
            if (FlyTime  > 0.15)
            {
                FireObject.SetActive(false);
            }
        }           

    }
    private void ProcessVacMotion()
    {
        if (isVacuuming)
        {
            VacMotion = musicBeat;
        }
        else
        {
            VacMotion = 0;
        }
    }
    
    private void ProcessDrillingMotion()
    {
        // 1. LOGIC: Move the sensor BEFORE checking for walls
        // This moves the invisible DrillSensor to the target tile 
        // without affecting the visible Drill's position.
        UpdateDrillSensorPosition(); // AG - this is redundant ToolCheck already did this

        float elapsedTime = Time.time - DrillstartTime;
        drillingT = Mathf.Clamp01(elapsedTime / duration);
        
        // ToolCheck() now checks the invisible DrillSensor's location
        if (isDrilling && ToolCheck() == true) 
        {
            Drilling = 1; 
        }
        else
        {
            Drilling = 0;
        }
        
        // 2. VISUALS: Calculate the bounce/squash for the visible drill
        // These calculations set 'Drillvector', which ToolScript reads.
        // We do NOT modify toolCheckPos here, preserving the visual appearance.

        // AG   -it wasnt: the motion only happened inside the child not the parent  -- logic is now broken: drilling motion is always on when button pressed
        lerpDrill = Mathf.Lerp(initialDrill, Drilling, drillingT); // gradual drilling motion start
        if (drillingT >= 1f)
        {
            initialDrill = Drilling;
            DrillstartTime = Time.time;
        }

        drill = (lerpDrill * -musicBounce); 
        DrillposX = Mathf.Abs(horizontalDirection) * lerpDrill * -musicBounce ;

        if (isGrounded && !isFlying)
        {
            if (impactt < 1f) //on impact
            {
                DrillposY = curveValue - musicBounce;                
            }            
            DrillposY = Mathf.Clamp01(-verticalDirection) * lerpDrill * musicBounce ;                       
        }
        else
        {
            DrillposY = Mathf.Clamp01(verticalDirection) * lerpDrill * -musicBounce;
        }
        
        Drillvector = new Vector2(DrillposX, DrillposY);
    }

    private void UpdateDrillSensorPosition() //this process is redundant ToolCheck() did this already
    {
        if (drillSensor == null) return;

        float sensorOffset = 0.85f; 

        if (verticalDirection < -0.1f)
        {
            // Aiming Down
            drillSensor.localPosition = new Vector3(0, -sensorOffset, 0); 
        }
        else if (verticalDirection > 0.1f)
        {
            // Aiming Up
            drillSensor.localPosition = new Vector3(0, sensorOffset, 0);
        }
        else
        {
            // Aiming Horizontal
            // We always use positive X because the Player object itself 
            // is flipped via Transform.localScale.x in the Flip() method.
            drillSensor.localPosition = new Vector3(sensorOffset, 0, 0); 
        }
    }

    private void ProcessInventoryAdjustment()
    {
        if (inventory / carryStrength <= maxInventory)
        {
            inventoryAdjustment = (maxInventory - inventory / carryStrength) / maxInventory;

        }
        else
        {
            inventoryAdjustment = 0;
        }
    }
    private void ProcessXMovement()
    {
        xMagnitude = Mathf.Abs(rb.linearVelocity.x);
       
        inventoryAdjustedMaxSpeed = 0.1f * maxMoveSpeed + maxMoveSpeed * inventoryAdjustment;

       

            float xvelocity = Mathf.SmoothDamp(rb.linearVelocity.x, inventoryAdjustedMaxSpeed * horizontalDirection, ref currentxvelocity, smoothtime);
            rb.linearVelocity = new Vector2(xvelocity,rb.linearVelocity.y);
            

            if (isGrounded & xMagnitude > 0.5 || isGrounded & Drilling > 0) // make dust when grounded
            {
                DustVFX.Play();
            }
            else
            {
                DustVFX.Stop();
            }
          

    }

    private void ProcessGravity()
    {
        if (rb.linearVelocity.y < -0.5)
        {
        rb.gravityScale = baseGravity * fallSpeedMultiplier; //Fall Speed increasingly faster
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -maxFallSpeed));
            if(jumpsRemaining >0)
            {
                jumpsRemaining--;
            }
                
        }
    }
    
    private void ProcessStretch()
    {        
        if (isGrounded) //when grounded 
        {
            
            float elapsedImpactTime = Time.time - impactStartTime; //start timer
            float beatRemainder = impactStartTime % animationDuration; // add time to line up to the beat
            impactDuration = 2f * animationDuration - beatRemainder;
            impactt = Mathf.Clamp01(elapsedImpactTime / impactDuration);
            if (impactt < 1f) //on impact
            {
                curveValue = impactAnimation.Evaluate(impactt);                    
                impactbeat = Mathf.Lerp(0f, 1f, impactt);
                squashMagnitude = 0.2f * (curveValue - (impactbeat * musicBeat)) + inventoryAdjustment * (curveValue - (impactbeat * musicBeat));  
            }
            else
            {
                squashMagnitude = - 0.2f * musicBeat - musicBeat * inventoryAdjustment; // otherwise stretch to beat
            }            
        }

        else //when flying stretch with up velocity
        {
            squashMagnitude = Mathf.Clamp(rb.linearVelocity.y / maxFallSpeed * stretchAmount, 0, maxFallSpeed);
            impactStartTime = Time.time; //reset impact timer
            
        }
        Vector3 ls = transform.localScale;
        ls = new Vector3(isFacingRight, 1, 1); //flip
        transform.localScale = ls;
    }
   
     
    public void Move(InputAction.CallbackContext context) ///buttons horizontal and vertical directions
        {        
        horizontalDirection = context.ReadValue<Vector2>().x;
        verticalDirection = context.ReadValue<Vector2>().y;
        aiming = Mathf.Abs(verticalDirection) + Mathf.Abs(horizontalDirection);
        }
    public void Shoot(InputAction.CallbackContext context) //button for vacuume
    {
        if (context.performed)
        {
            
            if (inventory > 0)
            { 
            inventory--;
            
            Instantiate(itemspawn, transform.position, transform.rotation);
            }

        }
       
    }
    public void Vacuume(InputAction.CallbackContext context) //button for vacuume
    {
        if (context.performed)
        {
            isVacuuming = true;
            SuckVFX.Play();
            vacCheck.SetActive(true);
            collector.SetActive(true);
        }
        if (context.canceled)
        {
            isVacuuming = false;
            SuckVFX.Stop();
            vacCheck.SetActive(false);
            collector.SetActive(false);
        }
      
    }
    public void Drill(InputAction.CallbackContext context) //button to activate  drill
    {
        if (context.performed )
        {
            DrillObject.SetActive(true);
            VacObject.SetActive(false);
            isDrilling = true;
           
        }
        if (context.canceled)
        {
                DrillObject.SetActive(false);
                VacObject.SetActive(true);
                SuckVFX.Stop();
                isDrilling = false;
                vacCheck.SetActive(false);
                collector.SetActive(false);
        }
               
        
    }
    
    public void Jump(InputAction.CallbackContext context) //button for jump and fly
    {
        if (jumpsRemaining > 0)
        {            
            if (context.performed)
            {
                // Hold down jump button = full height
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0.5f * jumpPower + inventoryAdjustment * jumpPower);
                jumpsRemaining--;
            }

            else if (context.canceled)
            {
                //Light tap jump button = half hight
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
                jumpsRemaining--;
            }
        }
        else //can fly on second jump
        {
            if (context.performed)
            { // Hold down jump button = flying
                if (rb.linearVelocity.y < 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0.2f * flyPower + inventoryAdjustment * flyPower); //stop falling
                    rb.gravityScale = -flyGravity * inventoryAdjustment; //inverse gravity for flying 
                    isFlying = true;
                }
                else
                {
                    isFlying = true;
                    
                    rb.gravityScale = -0.02f * flyGravity - flyGravity * inventoryAdjustment; //inverse gravity for flying
                    
                }
                
            }

            else if (context.canceled)
            {
                //stop flying return gravity to normal
                rb.gravityScale = baseGravity;
                isFlying = false;

            }
        }       
    }
    private void GroundCheck()
    {
        if (Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer))
        {
            if (!isFlying)
            {
                jumpsRemaining = maxJumps; //reset Jumps
                rb.gravityScale = baseGravity;//reset gravity
            }
                  
            isGrounded = true;            
        }
        else
        {
            
            isGrounded = false;
        }
    }
    private bool ToolCheck()
    {
        return Physics2D.OverlapBox(toolCheckPos.position, toolCheckSize, 0, groundLayer);
    }
    private bool WallCheck()
    {
        return Physics2D.OverlapBox(wallCheckPos.position, wallCheckSize, 0, groundLayer);
        
    }
    private void WallPushCheck()
    {
        if (horizontalDirection != 0 & WallCheck())
        {
            isTouchingWall = true;
            // do not tilt with x velocity 
            transform.localEulerAngles = Vector3.zero;
        }
        else
        {
            isTouchingWall = false;
            // tilt with x velocity 
            Vector3 newRotation = new Vector3(0f, 0f, rb.linearVelocity.x * -RotationMultiplier);
            transform.localEulerAngles = newRotation;
        }
    }
    private void Flip()
    {
        if (horizontalDirection * isFacingRight < 0)
        {
            Flipping = true;
        }

        else
        {
            Flipping = false;
        }

        if (horizontalDirection > 0)
        {
            isFacingRight = 1;

        }
        else if (horizontalDirection < 0)
        {
            isFacingRight = -1;
        }
    }
   
    private void MusicBeat() // should probably be moved to a seperate script
    { 
    float squashAmplitude = (1f - squashAmount) / 2f;                        
    animationDuration = 60f / BPM;
    angularFrequency = (2f * Mathf.PI) / animationDuration;
    musicBeat = squashAmplitude * Mathf.Sin(Time.time * angularFrequency) * (squashAmount + squashAmplitude);
    musicBounce = Mathf.Abs(squashAmplitude * Mathf.Sin(Time.time * angularFrequency* 0.5f));
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
   
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(wallCheckPos.position, wallCheckSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(toolCheckPos.position, toolCheckSize);

    }

    public void BaseCol(InputAction.CallbackContext context) // should probably be moved to a seperate script
    {
        if (context.performed)
        {
            BaseColour++;
        }
    }

    public void AccentCol(InputAction.CallbackContext context) // should probably be moved to a seperate script
    {
        if (context.performed)
        {
            AccentColour++;
        }
    }
    public void HeadCol(InputAction.CallbackContext context) // should probably be moved to a seperate script
    {
        if (context.performed)
        {
            HeadColour++;
        }
    }
}