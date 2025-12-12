using UnityEngine;

public class fire_script : MonoBehaviour
{
    private PlayerMovement _playerScript;
    public Animator FireAnimator;
    float Beat;
    public float SquashMult;
    bool isGrounded;
    bool FireOn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _playerScript = GetComponentInParent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        FireOn = _playerScript.isFlying;
        isGrounded = _playerScript.isGrounded;
        Beat  = _playerScript.musicBeat;            
        float Sq = Beat * SquashMult;
        if (!isGrounded)
        {
            transform.localScale = new Vector3(1f - Sq * 0.5f, 1f + Sq, 1f);
        }
        else
        {
            transform.localScale = new Vector3(1f - Sq * 0.4f, 1f + Sq * 0.8f, 1f);
        }
        FireAnimator.SetBool("FireOn", FireOn);
    }
}
