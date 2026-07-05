using UnityEngine;

public class Rotator : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float speed = 15f;

    void Update()
    {
        transform.Rotate(axis.normalized * speed * Time.deltaTime, Space.World);
    }
}
