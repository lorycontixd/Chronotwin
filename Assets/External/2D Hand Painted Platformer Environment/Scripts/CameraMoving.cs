using UnityEngine;

public class CameraMoving : MonoBehaviour
{
    [SerializeField] private float speed;
    
    private float horizontal;

    void Update()
    {
        horizontal  = Input.GetAxis("Horizontal");

        transform.Translate(new Vector3(horizontal, 0, 0) * speed * Time.deltaTime);
    }
}
