using UnityEngine;

/// <summary>
/// Moves a product along the factory's straight main conveyor and loops it to the line start.
/// </summary>
[DisallowMultipleComponent]
public sealed class ConveyorProductMover : MonoBehaviour
{
    [SerializeField] private float speed = 0.65f;
    [SerializeField] private float startZ = -12.75f;
    [SerializeField] private float endZ = 12.75f;

    public void ConfigureMotion(float newSpeed, float newStartZ, float newEndZ)
    {
        speed = Mathf.Max(0f, newSpeed);
        startZ = newStartZ;
        endZ = Mathf.Max(newStartZ + 0.01f, newEndZ);
    }

    private void Update()
    {
        Vector3 position = transform.position;
        position.z += speed * Time.deltaTime;

        if (position.z > endZ)
        {
            position.z = startZ + (position.z - endZ);
        }

        transform.position = position;
    }
}
