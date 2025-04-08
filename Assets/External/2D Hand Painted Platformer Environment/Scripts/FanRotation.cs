using UnityEngine;

public class FanRotation : MonoBehaviour
{
    
    [SerializeField] private float speed;
    
    private Vector3 angles = new Vector3(0, 0, -1);


    void FixedUpdate()
    {
        transform.Rotate(angles*speed);
    }
}
