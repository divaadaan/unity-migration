using UnityEngine;

public class GroundCheckScript : MonoBehaviour
{
    private PlayerMovement _playerScript;
    bool isFlying;
    float Yposition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerScript = GetComponentInParent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        isFlying = _playerScript.isFlying;
        if (isFlying)
        {
            Yposition = 0.82f;
        }
        else
        {
            Yposition = -0.05f;
        }
        transform.localPosition = new Vector3 (0,Yposition, 0);
    }
}
