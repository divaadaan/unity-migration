using UnityEngine;

public class PlayerStechScript : MonoBehaviour
{
    public Rigidbody2D rb;
    
    [Header("DrillMotion")]
    Vector2 drillmotion;
    Vector2 motion;

    [Header("SquashStretch")]
    private PlayerMovement _playerScript;
    float sqMagnitude;
    bool _isGrounded;
    bool _isFlying;
    float Yposition;

    



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerScript = GetComponentInParent<PlayerMovement>();        
        
    }

    // Update is called once per frame
    void Update()
    {
       ProcessStretch();
       ProcessDrill();

        _isFlying = _playerScript.isFlying;
        _isGrounded = _playerScript.isGrounded;
    }

   
    private void ProcessDrill()
    {
        drillmotion = _playerScript.Drillvector;
        if (_isGrounded && _isFlying)
        {
            motion = drillmotion * 3f + new Vector2(0, -sqMagnitude);
        }
        else
        {
            motion = drillmotion * 3f;
        }        
        transform.localPosition = motion;
    }
    private void ProcessStretch()
    {
        
        sqMagnitude = _playerScript.squashMagnitude;
        
            transform.localScale = new Vector3(1 - sqMagnitude * 0.5f, 1 + sqMagnitude, 1);
        
    }
}
