using UnityEngine;

public class Vac_check : MonoBehaviour
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.gameObject.TryGetComponent<item_script>(out item_script item))
        {
            item.SetTarget(transform.parent.position);
        }
    }

}
