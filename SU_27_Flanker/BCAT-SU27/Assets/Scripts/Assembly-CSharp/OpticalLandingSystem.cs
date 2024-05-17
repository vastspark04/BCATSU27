using System.Collections;
using UnityEngine;

public class OpticalLandingSystem : MonoBehaviour
{
	public struct OLSData
	{
		public int ball;
	}

	public GameObject[] ballFlares;

	public float maxOffset;

	public Transform targetTransform;

	public Transform sensorTransform;

	public float maxDist;

	public float minDist;

	public GameObject displayObject;

	public GameObject datumObject;

	public float minDot;

	private float maxSqrDist;

	private float minSqrDist;

	private int flareCount;

	private bool usePlayer;

	private Tailhook playerHook;

	public int[] waveOffIndices;

	public float waveOffTime;

	public float waveOffIntervalOn;

	public float waveOffIntervalOff;

	private bool wavingOff;

	private Coroutine waveOffRoutine;

	private void Start()
	{
		maxSqrDist = maxDist * maxDist;
		minSqrDist = minDist * minDist;
		flareCount = ballFlares.Length;
	}

	private void OnEnable()
	{
		StartCoroutine(CheckForPlayerRoutine());
	}

	private IEnumerator CheckForPlayerRoutine()
	{
		WaitForSeconds shortWait = new WaitForSeconds(0.5f);
		WaitForSeconds longWait = new WaitForSeconds(5f);
		yield return new WaitForSeconds(Random.Range(0.1f, 5f));
		while (base.enabled)
		{
			if ((bool)FlightSceneManager.instance && (bool)FlightSceneManager.instance.playerActor && !FlightSceneManager.instance.playerActor.flightInfo.isLanded)
			{
				Vector3 vector = FlightSceneManager.instance.playerActor.transform.position - sensorTransform.position;
				float sqrMagnitude = vector.sqrMagnitude;
				if (sqrMagnitude < maxSqrDist && sqrMagnitude > minSqrDist && Vector3.Dot(sensorTransform.forward, vector.normalized) > minDot)
				{
					usePlayer = true;
				}
				else
				{
					usePlayer = false;
				}
				yield return shortWait;
			}
			else
			{
				usePlayer = false;
				yield return longWait;
			}
		}
	}

	private void Update()
	{
		Transform transform = targetTransform;
		if (usePlayer && (bool)FlightSceneManager.instance.playerActor)
		{
			if (!playerHook && (bool)FlightSceneManager.instance && (bool)FlightSceneManager.instance.playerActor)
			{
				playerHook = FlightSceneManager.instance.playerActor.GetComponentInChildren<Tailhook>(includeInactive: true);
			}
			transform = ((!playerHook) ? FlightSceneManager.instance.playerActor.transform : playerHook.hookPointTf);
		}
		if (wavingOff)
		{
			return;
		}
		if ((bool)transform)
		{
			Vector3 vector = transform.position - sensorTransform.position;
			float sqrMagnitude = vector.sqrMagnitude;
			if (sqrMagnitude < maxSqrDist && sqrMagnitude > minSqrDist && Vector3.Dot(sensorTransform.forward, vector.normalized) > minDot)
			{
				displayObject.SetActive(value: true);
				Vector3 toDirection = sensorTransform.InverseTransformPoint(transform.position);
				toDirection.x = 0f;
				int num = Mathf.RoundToInt(Mathf.Clamp(VectorUtils.SignedAngle(Vector3.forward, toDirection, -Vector3.up) / maxOffset, -1f, 1f) * (float)(flareCount / 2));
				int num2 = num + flareCount / 2;
				if (num2 < 0)
				{
					num2 = 0;
				}
				else if (num2 >= flareCount)
				{
					num2 = flareCount - 1;
				}
				for (int i = 0; i < flareCount; i++)
				{
					if (i == num2)
					{
						ballFlares[i].SetActive(value: true);
					}
					else
					{
						ballFlares[i].SetActive(value: false);
					}
				}
				if ((bool)datumObject)
				{
					datumObject.SetActive(num2 != flareCount - 1);
				}
				if (usePlayer && (bool)playerHook && playerHook.isDeployed)
				{
					OLSData oLSData = default(OLSData);
					oLSData.ball = num;
					OLSData data = oLSData;
					playerHook.SendOLSData(data);
				}
			}
			else
			{
				displayObject.SetActive(value: false);
			}
		}
		else
		{
			displayObject.SetActive(value: false);
		}
	}

	private void OnValidate()
	{
		if (maxOffset == 0f)
		{
			maxOffset = 0.01f;
		}
	}

	public void BeginWaveOffSequence()
	{
		if (waveOffRoutine != null)
		{
			StopCoroutine(waveOffRoutine);
		}
		waveOffRoutine = StartCoroutine(WaveOffRoutine());
	}

	private IEnumerator WaveOffRoutine()
	{
		WaitForSeconds intervalWaitOn = new WaitForSeconds(waveOffIntervalOn);
		WaitForSeconds intervalWaitOff = new WaitForSeconds(waveOffIntervalOff);
		wavingOff = true;
		for (int i = 0; i < ballFlares.Length; i++)
		{
			ballFlares[i].SetActive(value: false);
		}
		datumObject.SetActive(value: false);
		displayObject.SetActive(value: true);
		bool on = false;
		float t = Time.time;
		while (Time.time - t < waveOffTime)
		{
			on = !on;
			for (int j = 0; j < waveOffIndices.Length; j++)
			{
				ballFlares[waveOffIndices[j]].SetActive(on);
			}
			if (on)
			{
				yield return intervalWaitOn;
			}
			else
			{
				yield return intervalWaitOff;
			}
		}
		for (int k = 0; k < waveOffIndices.Length; k++)
		{
			ballFlares[waveOffIndices[k]].SetActive(value: false);
		}
		wavingOff = false;
	}
}
