using System.Collections;
using UnityEngine;

public class HUDCollisionWarning : MonoBehaviour
{
	public GameObject warningObject;

	public AudioClip warningSound;

	public float interval = 0.35f;

	private CollisionDetector detector;

	public AudioSource audioSource;

	private bool started;

	private void Start()
	{
		detector = GetComponentInParent<CollisionDetector>();
		if (!detector)
		{
			base.enabled = false;
		}
		else
		{
			started = true;
		}
	}

	private void OnEnable()
	{
		warningObject.SetActive(value: false);
		StartCoroutine(WarningRoutine());
	}

	private void Update()
	{
	}

	private IEnumerator WarningRoutine()
	{
		while (!started)
		{
			yield return null;
		}
		while (base.enabled)
		{
			if (detector.GetCollisionDetected())
			{
				warningObject.SetActive(value: true);
				audioSource.PlayOneShot(warningSound);
			}
			yield return new WaitForSeconds(interval * 0.85f);
			warningObject.SetActive(value: false);
			yield return new WaitForSeconds(interval * 0.15f);
		}
	}
}
