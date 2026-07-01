using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class PressurePlate : MonoBehaviour
{
    public Transform controlledObject;
    public Vector3 closedWorldPosition;
    public Vector3 openWorldPosition;
    public float requiredMass = 1.4f;
    public float moveSpeed = 4f;
    public Renderer indicatorRenderer;
    public Color idleColor = new Color(0.95f, 0.78f, 0.24f);
    public Color activeColor = new Color(0.22f, 0.95f, 0.58f);

    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private Material indicatorMaterial;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (controlledObject != null && closedWorldPosition == Vector3.zero && openWorldPosition == Vector3.zero)
        {
            closedWorldPosition = controlledObject.position;
            openWorldPosition = controlledObject.position + Vector3.up * 3f;
        }

        if (indicatorRenderer != null)
        {
            indicatorMaterial = indicatorRenderer.material;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsUsefulOccupant(other))
        {
            occupants.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        occupants.Remove(other);
    }

    private void Update()
    {
        if (controlledObject == null)
        {
            return;
        }

        CleanupOccupants();
        bool active = CurrentMass() >= requiredMass;
        Vector3 target = active ? openWorldPosition : closedWorldPosition;
        controlledObject.position = Vector3.MoveTowards(controlledObject.position, target, moveSpeed * Time.deltaTime);

        if (indicatorMaterial != null)
        {
            indicatorMaterial.color = active ? activeColor : idleColor;
        }

        if (active)
        {
            GameHud.ShowMessage("Gate unlocked. Keep going.", 1.2f);
        }
    }

    private bool IsUsefulOccupant(Collider other)
    {
        if (other == null || other.isTrigger)
        {
            return false;
        }

        if (controlledObject != null && other.transform.IsChildOf(controlledObject))
        {
            return false;
        }

        return other.attachedRigidbody != null || other.GetComponentInParent<WobblePlayerController>() != null;
    }

    private float CurrentMass()
    {
        float mass = 0f;

        foreach (Collider occupant in occupants)
        {
            if (occupant == null)
            {
                continue;
            }

            Rigidbody attached = occupant.attachedRigidbody;
            mass += attached != null ? attached.mass : 1f;
        }

        return mass;
    }

    private void CleanupOccupants()
    {
        occupants.RemoveWhere(item => item == null || !item.gameObject.activeInHierarchy);
    }
}
