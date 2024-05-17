using System.Collections;
using UnityEngine;

public class ProxyDetTest : MonoBehaviour
{
	public IRMissileLauncher irML;

	public OpticalMissileLauncher opML;

	public Transform headTf;

	public float dist;

	public float launchSpeed;

	public float tgtLaunchSpeed;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			StartCoroutine(Test());
		}
	}

	private IEnumerator Test()
	{
		irML.LoadAllMissiles();
		irML.GetNextMissile().heatSeeker.debugSeeker = true;
		irML.EnableWeapon();
		opML.LoadAllMissiles();
		Vector3 zero = Vector3.zero;
		zero.y = WaterPhysics.instance.height + 3000f;
		irML.transform.position = zero;
		Vector3 position = new Vector3(0f, 0f, dist);
		position.y = WaterPhysics.instance.height + 3000f;
		opML.transform.position = position;
		irML.transform.rotation = Quaternion.identity;
		opML.transform.rotation = Quaternion.LookRotation(Vector3.back);
		Missile i = opML.GetNextMissile();
		i.SetOpticalTarget(irML.transform);
		opML.FireMissile();
		while (!i.rb)
		{
			yield return null;
		}
		i.rb.velocity += launchSpeed * i.rb.velocity.normalized;
		StartCoroutine(FireIRRoutine(i.transform));
	}

	private IEnumerator FireIRRoutine(Transform tgtTf)
	{
		Missile i = irML.GetNextMissile();
		i.heatSeeker.headTransform = headTf;
		i.heatSeeker.SetSeekerMode(HeatSeeker.SeekerModes.HeadTrack);
		while (!i.hasTarget)
		{
			headTf.LookAt(tgtTf);
			yield return null;
		}
		irML.TryFireMissile();
		while (!i.rb)
		{
			yield return null;
		}
		i.rb.velocity += tgtLaunchSpeed * i.rb.velocity.normalized;
	}
}
