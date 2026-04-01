using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WanderArea : MonoBehaviour
{
    private BoxCollider boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
    }

    public Vector3 GetRandomPoint()
    {
        Bounds bounds = boxCollider.bounds;

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = bounds.center.y;
        float z = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(x, y, z);
    }
}