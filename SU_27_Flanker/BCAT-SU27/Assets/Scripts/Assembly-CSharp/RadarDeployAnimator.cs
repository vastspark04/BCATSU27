using System.Collections;
using UnityEngine;

public class RadarDeployAnimator : MonoBehaviour, IEngageEnemies
{
	public Radar radar;

	public RotationToggle rotationToggle;

	private bool engageEnemies;

	private Coroutine animRoutine;

	private void Awake()
	{
		radar.allowRotation = false;
	}

	public void SetEngageEnemies(bool engage)
	{
		if (engageEnemies == engage)
		{
			return;
		}
		engageEnemies = engage;
		if (animRoutine != null)
		{
			StopCoroutine(animRoutine);
		}
		if (base.gameObject.activeInHierarchy)
		{
			if (engageEnemies)
			{
				animRoutine = StartCoroutine(DeployRoutine());
			}
			else
			{
				animRoutine = StartCoroutine(RetractRoutine());
			}
		}
	}

	private void OnEnable()
	{
		SetEngageEnemies(!engageEnemies);
		SetEngageEnemies(!engageEnemies);
	}

	private IEnumerator DeployRoutine()
	{
		rotationToggle.SetDeployed();
		while (rotationToggle.transforms[0].currentT < 0.99f)
		{
			yield return null;
		}
		radar.allowRotation = true;
	}

	private IEnumerator RetractRoutine()
	{
		radar.allowRotation = false;
		while (Mathf.Abs(radar.currentAngle) > 0.01f)
		{
			yield return null;
		}
		rotationToggle.SetDefault();
	}
}
