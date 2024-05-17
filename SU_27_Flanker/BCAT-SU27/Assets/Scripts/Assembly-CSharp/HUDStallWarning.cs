using System.Collections;
using UnityEngine;

public class HUDStallWarning : MonoBehaviour
{
	public GameObject warningObject;

	public float warnInterval;

	public AudioClip warningClip;

	public AudioSource audioSource;

	public float dotLimit;

	public float minSpeed;

	private float minSpeedSqr;

	private Rigidbody rb;

	private bool started;

	private bool stalling;

	private WaitForSeconds waitInterval;

	private bool clearedWarnings;

	private FlightWarnings flightWarnings;

	private FlightWarnings.FlightWarning flightWarning;

	private bool addedWarning;

	private TiltController tiltController;

	private void Start()
	{
		rb = GetComponentInParent<Rigidbody>();
		tiltController = rb.GetComponentInChildren<TiltController>();
		minSpeedSqr = minSpeed * minSpeed;
		waitInterval = new WaitForSeconds(warnInterval / 2f);
		started = true;
		flightWarnings = rb.GetComponentInChildren<FlightWarnings>();
		flightWarnings.OnClearedWarnings.AddListener(OnClearWarnings);
		flightWarning = new FlightWarnings.FlightWarning("Stall", null);
	}

	private void OnClearWarnings()
	{
		clearedWarnings = true;
		addedWarning = false;
	}

	private void OnEnable()
	{
		stalling = false;
		StartCoroutine(WarningRoutine());
	}

	private IEnumerator WarningRoutine()
	{
		while (!started)
		{
			yield return null;
		}
		while (base.enabled)
		{
			yield return waitInterval;
			stalling = false;
			if (Vector3.Dot(rb.velocity.normalized, rb.transform.forward) < dotLimit && rb.velocity.sqrMagnitude > minSpeedSqr && (!tiltController || tiltController.currentTilt > 45f))
			{
				stalling = true;
				if (!clearedWarnings)
				{
					warningObject.SetActive(value: true);
					if ((bool)audioSource)
					{
						audioSource.PlayOneShot(warningClip);
					}
					if (!addedWarning)
					{
						flightWarnings.AddContinuousWarning(flightWarning);
						addedWarning = true;
					}
				}
			}
			else
			{
				if (addedWarning)
				{
					flightWarnings.RemoveContinuousWarning(flightWarning);
					addedWarning = false;
				}
				clearedWarnings = false;
			}
			yield return waitInterval;
			warningObject.SetActive(value: false);
		}
	}
}
