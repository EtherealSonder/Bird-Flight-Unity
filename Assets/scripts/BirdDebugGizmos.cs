using UnityEngine;

public class BirdDebugGizmos : MonoBehaviour
{
    public Bird bird;

    void OnDrawGizmos()
    {
        if (bird == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(bird.body.position, bird.body.position + bird.body.linearVelocity);
        UnityEditor.Handles.Label(bird.body.position + Vector3.up * 2f, bird.currentState.ToString());
    }
}
