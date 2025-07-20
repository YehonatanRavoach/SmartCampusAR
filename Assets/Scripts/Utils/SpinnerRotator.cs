using UnityEngine;

/// <summary>
/// Rotates the attached GameObject at a constant speed.
/// Used for animated loading indicators and spinners.
/// </summary>
public class SpinnerRotator : MonoBehaviour
{
    public float rotationSpeed = 200f;

    void Update()
    {
        transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
    }
}
