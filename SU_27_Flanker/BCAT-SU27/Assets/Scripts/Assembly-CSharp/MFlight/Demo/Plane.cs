using UnityEngine;

namespace MFlight.Demo{

[RequireComponent(typeof(Rigidbody))]
public class Plane : MonoBehaviour
{
	[Header("Components")]
	[SerializeField]
	private MouseFlightController controller;

	[Header("Physics")]
	[Tooltip("Force to push plane forwards with")]
	public float thrust = 100f;

	[Tooltip("Pitch, Yaw, Roll")]
	public Vector3 turnTorque = new Vector3(90f, 25f, 45f);

	[Tooltip("Multiplier for all forces")]
	public float forceMult = 1000f;

	[Header("Autopilot")]
	[Tooltip("Sensitivity for autopilot flight.")]
	public float sensitivity = 5f;

	[Tooltip("Angle at which airplane banks fully into target.")]
	public float aggressiveTurnAngle = 10f;

	[Header("Input")]
	[SerializeField]
	[Range(-1f, 1f)]
	private float pitch;

	[SerializeField]
	[Range(-1f, 1f)]
	private float yaw;

	[SerializeField]
	[Range(-1f, 1f)]
	private float roll;

	private Rigidbody rigid;

	private bool rollOverride;

	private bool pitchOverride;

	public float Pitch
	{
		get
		{
			return pitch;
		}
		set
		{
			pitch = Mathf.Clamp(value, -1f, 1f);
		}
	}

	public float Yaw
	{
		get
		{
			return yaw;
		}
		set
		{
			yaw = Mathf.Clamp(value, -1f, 1f);
		}
	}

	public float Roll
	{
		get
		{
			return roll;
		}
		set
		{
			roll = Mathf.Clamp(value, -1f, 1f);
		}
	}

	private void Awake()
	{
		rigid = GetComponent<Rigidbody>();
		if (controller == null)
		{
			Debug.LogError(base.name + ": Plane - Missing reference to MouseFlightController!");
		}
	}

	private void Update()
	{
		rollOverride = false;
		pitchOverride = false;
		float axis = Input.GetAxis("Horizontal");
		if (Mathf.Abs(axis) > 0.25f)
		{
			rollOverride = true;
		}
		float axis2 = Input.GetAxis("Vertical");
		if (Mathf.Abs(axis2) > 0.25f)
		{
			pitchOverride = true;
			rollOverride = true;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		if (controller != null)
		{
			RunAutopilot(controller.MouseAimPos, out num, out num2, out num3);
		}
		yaw = num;
		pitch = (pitchOverride ? axis2 : num2);
		roll = (rollOverride ? axis : num3);
	}

	private void RunAutopilot(Vector3 flyTarget, out float yaw, out float pitch, out float roll)
	{
		if (Input.GetButton("Fire2"))
		{
			yaw = 0f;
			pitch = 0f;
			roll = 0f;
			return;
		}
		Vector3 vector = base.transform.InverseTransformPoint(flyTarget).normalized * sensitivity;
		float value = Vector3.Angle(base.transform.forward, flyTarget - base.transform.position);
		yaw = Mathf.Clamp(vector.x, -1f, 1f);
		pitch = 0f - Mathf.Clamp(vector.y, -1f, 1f);
		float b = Mathf.Clamp(vector.x, -1f, 1f);
		float y = base.transform.right.y;
		float t = Mathf.InverseLerp(0f, aggressiveTurnAngle, value);
		roll = Mathf.Lerp(y, b, t);
	}

	private void FixedUpdate()
	{
		rigid.AddRelativeForce(Vector3.forward * thrust * forceMult, ForceMode.Force);
		rigid.AddRelativeTorque(new Vector3(turnTorque.x * pitch, turnTorque.y * yaw, (0f - turnTorque.z) * roll) * forceMult, ForceMode.Force);
	}
}}
