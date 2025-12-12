using UnityEngine;

public class spring_script : MonoBehaviour
{
    private PlayerMovement _playerScript;
    float sqMagnitude;
    float rotation;
    public float rotationOffset = 0;
    public float rotationMult = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerScript = GetComponentInParent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {

        sqMagnitude = _playerScript.squashMagnitude;
        rotation = (-sqMagnitude * rotationMult) - rotationOffset;
     

        transform.localEulerAngles = new Vector3(0, 0, rotation);
    }
}
