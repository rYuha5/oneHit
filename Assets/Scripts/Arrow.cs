using UnityEngine;

public class Arrow : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        if (target != null)
        {
            Vector3 aboveHead = target.position + Vector3.up * 1.5f;
            transform.position = aboveHead;
        }
    }
}