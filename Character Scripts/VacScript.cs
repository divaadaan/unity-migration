using UnityEngine;

public class VacScript : MonoBehaviour
{
    private PlayerMovement _playerScript;

    float vacmotion;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerScript = GetComponentInParent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        vacmotion = _playerScript.VacMotion;
        transform.localPosition = new Vector3(0, 0.5f * vacmotion, 0);
        transform.localScale = new Vector3(0.58f + 0.3f * vacmotion, 0.58f - 0.3f * vacmotion, 1);
    }
}
