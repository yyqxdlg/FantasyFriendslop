using UnityEngine;

public class HeroPreviewController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string idleStateName = "Yellow_Idle";

    private void Start()
    {
        if (animator != null)
            animator.Play(idleStateName);
    }
}