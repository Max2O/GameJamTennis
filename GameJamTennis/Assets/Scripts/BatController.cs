using UnityEngine;

public class BatController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public void Swing()
    {
        animator.SetTrigger("Swing");
    }
}