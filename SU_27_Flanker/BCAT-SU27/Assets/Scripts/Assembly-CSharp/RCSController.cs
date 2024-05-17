using UnityEngine;

public class RCSController : FlightControlComponent
{
	public enum RCSModes
	{
		On,
		Auto,
		Off
	}

	public Battery battery;

	public float powerDrain;

	public float maxTorque;

	public bool separateAxisTorque;

	public float maxPitchTorque;

	public float maxRollTorque;

	public float maxYawTorque;

	public float fuelDrain;

	public FuelTank fuelTank;

	public ParticleSystem[] pSystems;

	public float multiplier = 1f;

	public float pSpeed;

	public float pSize;

	public float pEmission;

	public float minFac = 0.1f;

	public AudioSource audioSource;

	public AudioClip popThrustAudio;

	public float minPopAudioInterval = 0.25f;

	private float[] lastPopAudioTime;

	public FlightInfo flightInfo;

	public RCSModes rcsMode = RCSModes.Auto;

	public AnimationCurve autoRcsCurve;

	[Header("Torque Per Tilt")]
	public bool useTiltCurve;

	public TiltController tc;

	public AnimationCurve tiltCurve;

	private int pCount;

	private Vector3 input;

	private bool alive = true;

	private Rigidbody rb;

	private bool hasDisabledPsystems;

	private bool isPowered;

	public void SetRcsMode(int m)
	{
		rcsMode = (RCSModes)m;
	}

	private void Awake()
	{
		rb = GetComponentInParent<Rigidbody>();
		pCount = pSystems.Length;
		for (int i = 0; i < pCount; i++)
		{
			pSystems[i].SetEmissionRate(0f);
		}
		lastPopAudioTime = new float[pSystems.Length];
	}

	public override void SetPitchYawRoll(Vector3 pitchYawRoll)
	{
		if (alive)
		{
			float num = ((rcsMode == RCSModes.On) ? 1f : autoRcsCurve.Evaluate(flightInfo.airspeed));
			if (useTiltCurve)
			{
				num *= tiltCurve.Evaluate(tc.currentTilt);
			}
			if (flightInfo.isLanded)
			{
				num = 0f;
			}
			input = num * pitchYawRoll;
		}
	}

	public void Kill()
	{
		input = Vector3.zero;
		alive = false;
	}

	private void FixedUpdate()
	{
		isPowered = alive && rcsMode != RCSModes.Off && (!battery || battery.Drain(powerDrain * Time.fixedDeltaTime)) && (fuelDrain <= 0f || fuelTank.fuel > 0f);
		if (isPowered)
		{
			Vector3 vector = ((!separateAxisTorque) ? (input * maxTorque) : new Vector3(maxPitchTorque * input.x, maxYawTorque * input.y, maxRollTorque * input.z));
			float num = fuelTank.RequestFuel((Mathf.Abs(vector.x) + Mathf.Abs(vector.y) + Mathf.Abs(vector.z)) * fuelDrain * Time.fixedDeltaTime);
			rb.AddRelativeTorque(num * vector);
		}
	}

	private void Update()
	{
		if (isPowered)
		{
			hasDisabledPsystems = false;
			Vector3 rhs = input.z * rb.transform.up;
			Vector3 rhs2 = input.x * rb.transform.up;
			Vector3 rhs3 = input.y * rb.transform.right;
			for (int i = 0; i < pCount; i++)
			{
				Vector3 forward = pSystems[i].transform.forward;
				Vector3 vector = pSystems[i].transform.position - rb.worldCenterOfMass;
				float num = Mathf.Sign(Vector3.Dot(vector, rb.transform.forward));
				Mathf.Sign(Vector3.Dot(vector, rb.transform.up));
				float num2 = Mathf.Clamp01(Vector3.Dot(forward, rhs2) * num);
				float num3 = Mathf.Clamp01((0f - Vector3.Dot(forward, rhs3)) * num);
				float num4 = Mathf.Clamp01(Vector3.Dot(forward, rhs) * Mathf.Sign(Vector3.Dot(-vector, rb.transform.right)));
				ParticleSystem.MainModule main = pSystems[i].main;
				float num5 = Mathf.Clamp01(num2 + num3 + num4) * multiplier;
				if (num5 < minFac)
				{
					num5 = 0f;
				}
				else if (main.startSpeed.constant < 1f && (bool)audioSource && Time.time - lastPopAudioTime[i] > minPopAudioInterval)
				{
					audioSource.PlayOneShot(popThrustAudio);
					lastPopAudioTime[i] = Time.time;
				}
				pSystems[i].SetEmissionRate(num5 * pEmission);
				main.startSpeed = pSpeed * num5;
				main.startSize = pSize * num5;
			}
		}
		else if (!hasDisabledPsystems)
		{
			for (int j = 0; j < pCount; j++)
			{
				pSystems[j].SetEmissionRate(0f);
			}
			hasDisabledPsystems = true;
		}
	}
}
