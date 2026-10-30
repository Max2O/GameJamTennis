using UnityEngine;

public class RoundStart : MonoBehaviour
{
    public Rigidbody Ball;


    void Start()
    {
        Ball = GetComponent<Rigidbody>();
    }
}
