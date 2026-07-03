using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class GoalZone : MonoBehaviour
{
    private bool completed;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed)
        {
            return;
        }

        if (other.GetComponentInParent<WobblePlayerController>() == null)
        {
            return;
        }

        completed = true;
        GameHud.ShowMessage("Prototype complete. You reached the goal!", 12f);
    }
}
