using UnityEngine;

public class FollowAndMove : MonoBehaviour
{
    public Transform target; // Target to follow
    public float followSpeed = 5f; // Speed of following
    public float rotationSpeed = 100f; // Speed of rotation

    private float distanceToTarget; // Distance between this object and the target

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target not assigned to FollowAndMove script!");
            enabled = false; // Disable script if target is not assigned
        }

        distanceToTarget = Vector3.Distance(transform.position, target.position);
    }

    void Update()
    {
        // Calculate direction to target
        Vector3 targetDirection = target.position - transform.position;

        // Move towards the target
        transform.position += targetDirection.normalized * followSpeed * Time.deltaTime;

        // Calculate rotation towards the target
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        // Smoothly rotate towards the target
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // User Input for Orbiting
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Orbit around the target based on user input
        if (horizontalInput != 0 || verticalInput != 0)
        {
            transform.RotateAround(target.position, Vector3.up, horizontalInput * rotationSpeed * Time.deltaTime);
            transform.RotateAround(target.position, transform.right, verticalInput * rotationSpeed * Time.deltaTime);
        }
    }
}
