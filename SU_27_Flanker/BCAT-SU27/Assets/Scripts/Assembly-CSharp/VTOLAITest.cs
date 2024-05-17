using System.Collections;
using UnityEngine;

public class VTOLAITest : MonoBehaviour
{
	private EngineEffects[] efxs;

	private AIPilot aiPilot;

	public Actor attackTarget;

	public Transform eyeTf;

	private float tilt = 45f;

	private float tiltTgt = 90f;

	private float tiltRate = 15f;

	private IEnumerator Start()
	{
		aiPilot = GetComponent<AIPilot>();
		aiPilot.maxSpeed = 0f;
		aiPilot.navSpeed = 0f;
		efxs = GetComponentsInChildren<EngineEffects>();
		yield return null;
		StartCoroutine(SoundFadeRoutine());
		EngineEffects[] array = efxs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetTilt(tilt);
		}
		float lookTimer = 0f;
		while (lookTimer < 2f)
		{
			lookTimer = ((!(Vector3.Angle(eyeTf.forward, base.transform.position - eyeTf.position) < 30f)) ? 0f : (lookTimer + Time.deltaTime));
			yield return null;
		}
		aiPilot.autoPilot.SetFlaps(1f);
		StartCoroutine(TiltRoutine());
		aiPilot.maxSpeed = 240f;
		aiPilot.navSpeed = 130f;
		yield return new WaitForSeconds(7f);
		GearAnimator[] componentsInChildren = GetComponentsInChildren<GearAnimator>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].Toggle();
		}
		StartCoroutine(CMRoutine());
		aiPilot.autoPilot.SetFlaps(0.5f);
		yield return new WaitForSeconds(7f);
		aiPilot.OrderAttackTarget(attackTarget);
		aiPilot.commandState = AIPilot.CommandStates.Combat;
		aiPilot.autoPilot.SetFlaps(0f);
	}

	private IEnumerator SoundFadeRoutine()
	{
		float o = 0f;
		while (o < 1f)
		{
			yield return null;
			o += 1f * Time.deltaTime;
			AudioController.instance.SetExteriorOpening("vtolIntro", o);
		}
	}

	private IEnumerator TiltRoutine()
	{
		yield return new WaitForSeconds(4f);
		tiltTgt = 25f;
		yield return new WaitForSeconds(4f);
		tiltTgt = 90f;
	}

	private IEnumerator CMRoutine()
	{
		CountermeasureManager cm = GetComponent<CountermeasureManager>();
		yield return new WaitForSeconds(10f);
		while (base.enabled)
		{
			int cmNo = Random.Range(2, 6);
			for (int i = 0; i < cmNo; i++)
			{
				cm.FireCM();
				yield return new WaitForSeconds(Random.Range(0.64f, 2f));
			}
			yield return new WaitForSeconds(15f);
		}
	}

	private void Update()
	{
		tilt = Mathf.MoveTowards(tilt, tiltTgt, tiltRate * Time.deltaTime);
		EngineEffects[] array = efxs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetTilt(tilt);
		}
	}
}
