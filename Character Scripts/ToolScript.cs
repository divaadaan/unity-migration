using UnityEngine;


public class ToolScript : MonoBehaviour
{
    public Rigidbody2D rb;
    private PlayerMovement _playerScript;
    float Ymovement;
    float Xmovement;
    public Material ToolMaterial;
    
    

    [Header("Lerp")]
    private float startTime;
    public float duration = 2f;

    [Header("Transform")]
    float Displacement;
    public float DisplacementMagnitude = 0.07f;
    public float DisplacementOffset = 0.03f;
    public float StretchMult = 1f;
    private float xs;
    private float ys;
    private Vector3 ls;
    private Vector3 lr;
    private Vector3 lp;
    public float _defaultRotation = 38f;
    float rx = 45f;
    float ry = 90f;
    float positionX;    
    float positionY;    
    private Vector3 initialLocalPosition;
    private Vector3 initialLocalRotation;
    bool Drilling =false;
    float drill;
    
    
    public float sqmult = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerScript = GetComponentInParent<PlayerMovement>();
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localEulerAngles;        
        startTime = 0;
    }

    // Update is called once per frame
    void Update()
    {
        Xmovement = _playerScript.horizontalDirection;
        Ymovement = _playerScript.verticalDirection;
        Drilling = _playerScript.isDrilling;



        InputToolDirection();
        ProcessToolDirection();        
        ToolMaterial.SetFloat("_DisplacementMagnitude", Displacement);

        if (Drilling)
        {
            drill = 1;
        }
        else
        {
            drill = 0;
        }
    }

    

    private void InputToolDirection()
    {        
                  
        if (Ymovement < -0.1f)
        {
            
            positionX = (Mathf.Abs(Xmovement) * 0.2f) + 0.1f;
            positionY = Ymovement * 0.5f + 0.7f - 0.4f * drill ;
        }
        else if (Ymovement > 0.1f)
        {
            positionX = (Mathf.Abs(Xmovement) * 0.2f) ;
            positionY = Ymovement * 0.5f + 0.5f * drill;
        }

        else
        {
            positionX = Mathf.Abs(Xmovement * 0.2f) + 0.15f;
            positionY = Ymovement * 0.5f + 0.3f;
        }
        
    }
    private void ProcessToolDirection()
    {
        
        float elapsedTime = Time.time - startTime;
        float t = Mathf.Clamp01(elapsedTime / duration);
        xs = Xmovement;
        ys = Ymovement;

        float rTotal = (Ymovement * ry) -rx;
        float currentDisplacement = Mathf.Clamp01(Mathf.Abs(xs) + Mathf.Abs(ys));
       
        
        Displacement = DisplacementOffset - (currentDisplacement * DisplacementMagnitude);
        float stretch = StretchMult *(1f + Mathf.Abs(Displacement));
        ls = new Vector3(1, stretch, 1f);
        lp = new Vector3(positionX, positionY, 0);
        lr = new Vector3(0, 0, rTotal - _defaultRotation);
        transform.localScale = ls;
        transform.localEulerAngles = Vector3.Lerp(initialLocalRotation, lr, t);
        transform.localPosition = Vector3.Lerp(initialLocalPosition, lp, t);
        if (t >= 1f)
        {

            
            initialLocalPosition = lp;
            initialLocalRotation = lr;
            startTime = Time.time;
            
        }        
    }
}

