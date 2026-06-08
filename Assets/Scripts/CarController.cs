using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Wheel Visuals")]
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;

    [Header("Car Settings")]
    public float maxMotorTorque = 1500f;  // N.m
    public float maxBrakeTorque = 3000f;  // N.m
    public float maxSteerAngle = 35f;     // Degrees
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);
    public Vector3 wheelRotationOffset = Vector3.zero;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private float brakeInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.centerOfMass += centerOfMassOffset;
        }
    }

    private void Update()
    {
        moveInput = Input.GetAxis("Vertical");
        turnInput = Input.GetAxis("Horizontal");
        brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void FixedUpdate()
    {
        ApplySteering();
        ApplyMotor();
        ApplyBraking();
        UpdateAllWheels();
    }

    private void ApplySteering()
    {
        float currentSteerAngle = turnInput * maxSteerAngle;
        if (frontLeftCollider != null) frontLeftCollider.steerAngle = currentSteerAngle;
        if (frontRightCollider != null) frontRightCollider.steerAngle = currentSteerAngle;
    }

    private void ApplyMotor()
    {
        float torque = moveInput * maxMotorTorque;
        // Rear wheel drive (RWD)
        if (rearLeftCollider != null) rearLeftCollider.motorTorque = torque;
        if (rearRightCollider != null) rearRightCollider.motorTorque = torque;
    }

    private void ApplyBraking()
    {
        float currentBrake = brakeInput * maxBrakeTorque;
        if (frontLeftCollider != null) frontLeftCollider.brakeTorque = currentBrake;
        if (frontRightCollider != null) frontRightCollider.brakeTorque = currentBrake;
        if (rearLeftCollider != null) rearLeftCollider.brakeTorque = currentBrake;
        if (rearRightCollider != null) rearRightCollider.brakeTorque = currentBrake;
    }

    private void UpdateAllWheels()
    {
        UpdateWheel(frontLeftCollider, frontLeftTransform);
        UpdateWheel(frontRightCollider, frontRightTransform);
        UpdateWheel(rearLeftCollider, rearLeftTransform);
        UpdateWheel(rearRightCollider, rearRightTransform);
    }

    private void UpdateWheel(WheelCollider col, Transform trans)
    {
        if (col == null || trans == null) return;

        Vector3 position;
        Quaternion rotation;
        col.GetWorldPose(out position, out rotation);

        trans.position = position;
        trans.rotation = rotation * Quaternion.Euler(wheelRotationOffset);
    }
}