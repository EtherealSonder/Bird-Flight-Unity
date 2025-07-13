using UnityEngine;

public class BirdAnimatorController : MonoBehaviour
{
    private Animator animator;
    private BirdInput input;
    private Bird bird;

    void Start()
    {
        animator = GetComponent<Animator>();
        input = GetComponentInParent<BirdInput>();
        bird = GetComponentInParent<Bird>();
    }

    void Update()
    {
        bool isActuallyFlapping = input.flapPressed && bird.currentState == Bird.FlightState.Flapping;
        animator.SetBool("isFlapping", isActuallyFlapping);

        if (isActuallyFlapping)
        {
            float angle = input.moveInput.x;

            // Use full range -1 (left) to 1 (right)
            animator.SetFloat("turnDirection", angle);
        }
        else
        {
            // Not flapping – ensure turn is neutral
            animator.SetFloat("turnDirection", 0f);
        }
    }
}
