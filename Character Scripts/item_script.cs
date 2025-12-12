using System;
using UnityEngine;

public class item_script : MonoBehaviour, ICollectible
{
    Rigidbody2D rb;

    bool hasTarget;
    Vector3 targetPosition;
    float movespeed = 5f;
    Vector2 TargetDirection;




    public static event Action OnItemCollected;
    public void Collect()
    {
        Debug.Log("item aquired");
       
        Destroy(gameObject);
        OnItemCollected?.Invoke();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); 
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if(hasTarget)
        {
            TargetDirection = (targetPosition - transform.position).normalized;
            rb.linearVelocity = new Vector2(TargetDirection.x, TargetDirection.y) * movespeed;
        }
       
        if (TargetDirection.x +TargetDirection.y <= 0)
        {
            hasTarget = false;
        }
    }
    public void SetTarget( Vector3 position)
    {
        targetPosition = position;
        hasTarget = true;
    }

    
}
