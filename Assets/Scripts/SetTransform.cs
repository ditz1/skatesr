using UnityEngine;

public class SetTransform : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public Transform tr;
    // Update is called once per frame
    void Update()
    {
        transform.position = tr.position;
    }
}
