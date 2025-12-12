using UnityEngine;

public class DrillBounceScript : MonoBehaviour
{
    private PlayerMovement _playerScript;

    float drillmotion;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerScript = GetComponentInParent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {        
        drillmotion = _playerScript.drill;
        transform.localPosition = new Vector3(0, 3f * drillmotion, 0);       
    }
}
