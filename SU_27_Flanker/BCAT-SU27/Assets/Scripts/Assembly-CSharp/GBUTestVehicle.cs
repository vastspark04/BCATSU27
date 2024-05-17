using System.Collections;
using UnityEngine;

public class GBUTestVehicle : MonoBehaviour
{
	public class GBUGlideTestGuidance : MissileGuidanceUnit
	{
		public float angle;

		private Vector3 tgtVec;

		protected override void OnBeginGuidance()
		{
			Vector3 forward = base.transform.forward;
			forward.y = 0f;
			Vector3 vector = Vector3.Cross(Vector3.up, forward);
			tgtVec = (Quaternion.AngleAxis(angle, -vector) * forward).normalized * 1000f;
		}

		public override Vector3 GetGuidedPoint()
		{
			return base.transform.position + tgtVec;
		}
	}

	public MissileLauncher ml;

	private Rigidbody rb;

	public Transform forwardTransform;

	public Transform rearTransform;

	public Transform leftTransform;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		((IParentRBDependent)ml).SetParentRigidbody(rb);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.A))
		{
			StartCoroutine(AltitudeTestRoutine());
		}
		if (Input.GetKeyDown(KeyCode.S))
		{
			StartCoroutine(SpeedTestRoutine());
		}
		if (Input.GetKeyDown(KeyCode.G))
		{
			StartCoroutine(GlideTestRoutine());
		}
	}

	private void TestLaunch(Vector3 initialVelocity, float altitude, Transform target, out Vector3 freeFallImpactPoint, out Missile launchedMissile)
	{
		base.transform.position = new Vector3(0f, WaterPhysics.instance.height + altitude, 0f);
		rb.velocity = initialVelocity;
		base.transform.rotation = Quaternion.LookRotation(initialVelocity, Vector3.up);
		ml.LoadMissile(0);
		Missile nextMissile = ml.GetNextMissile();
		nextMissile.SetGPSTarget(new GPSTarget(target.transform.position, "TGT", 0));
		float time;
		Vector3 vector = (freeFallImpactPoint = HPEquipBombRack.GetBombImpactPoint(out time, ml, 0.2f, 50f, nextMissile.GetComponent<SimpleDrag>().area, nextMissile.mass, hasTargeter: true, target.transform.position, ml.transform.position, rb.velocity));
		ml.FireMissile();
		launchedMissile = nextMissile;
	}

	private IEnumerator GlideSpeedTest(Missile m, DataGraph graph)
	{
		float t = 0f;
		while ((bool)m)
		{
			graph.AddValue(new Vector2(t, m.rb.velocity.magnitude));
			yield return new WaitForSeconds(3f);
			t += 3f;
		}
	}

	private IEnumerator GlideTestRoutine()
	{
		float testSpeed = 200f;
		float altitude = 3000f;
		float[] angles = new float[7] { 10f, 5f, 0f, -5f, -10f, -15f, -20f };
		int speedIdx = 4;
		DataGraph graph = DataGraph.CreateGraph("Glide Test", new Vector3(-600f, 300f));
		for (int i = 0; i < angles.Length; i++)
		{
			ml.LoadMissile(0);
			Missile nextMissile = ml.GetNextMissile();
			nextMissile.SetGPSTarget(new GPSTarget(Vector3.zero, "TGT", 0));
			GBUGlideTestGuidance gBUGlideTestGuidance = nextMissile.gameObject.AddComponent<GBUGlideTestGuidance>();
			gBUGlideTestGuidance.angle = angles[i];
			nextMissile.navMode = Missile.NavModes.Custom;
			nextMissile.guidanceUnit = gBUGlideTestGuidance;
			base.transform.position = new Vector3(0f, WaterPhysics.instance.height + altitude, 0f);
			rb.velocity = testSpeed * Vector3.forward;
			base.transform.rotation = Quaternion.LookRotation(rb.velocity, Vector3.up);
			ml.FireMissile();
			StartCoroutine(GlideTest(angles[i], graph, nextMissile));
			if (i == speedIdx)
			{
				DataGraph graph2 = DataGraph.CreateGraph("Speed over time", new Vector3(-300f, 300f));
				StartCoroutine(GlideSpeedTest(nextMissile, graph2));
			}
			yield return new WaitForSeconds(0.1f);
		}
	}

	private IEnumerator GlideTest(float angle, DataGraph graph, Missile missile)
	{
		Vector3 pos = missile.transform.position;
		while ((bool)missile)
		{
			pos = missile.transform.position;
			yield return null;
		}
		Vector3 vector = pos - base.transform.position;
		vector.y = 0f;
		float magnitude = vector.magnitude;
		graph.AddValue(new Vector2(angle, magnitude));
	}

	private IEnumerator AltitudeTestRoutine()
	{
		float testSpeed = 180f;
		float startAlt = 1f;
		float endAlt = 5000f;
		int num = 10;
		float interval = (endAlt - startAlt) / (float)num;
		DataGraph graphFwd = DataGraph.CreateGraph("Altitude Test (FWD)", new Vector3(-600f, 300f));
		for (float alt3 = startAlt; alt3 < endAlt; alt3 += interval)
		{
			StartCoroutine(RangeTest(testSpeed, alt3, forwardTransform, graphFwd, alt3));
			yield return new WaitForSeconds(0.1f);
		}
		DataGraph graphRear = DataGraph.CreateGraph("Altitude Test (REAR)", new Vector3(-600f, 100f));
		for (float alt3 = startAlt; alt3 < endAlt; alt3 += interval)
		{
			StartCoroutine(RangeTest(testSpeed, alt3, rearTransform, graphRear, alt3));
			yield return new WaitForSeconds(0.1f);
		}
		DataGraph graphLeft = DataGraph.CreateGraph("Altitude Test (LEFT)", new Vector3(-600f, -100f));
		for (float alt3 = startAlt; alt3 < endAlt; alt3 += interval)
		{
			StartCoroutine(RangeTest(testSpeed, alt3, leftTransform, graphLeft, alt3));
			yield return new WaitForSeconds(0.1f);
		}
	}

	private IEnumerator SpeedTestRoutine()
	{
		float testAltitude = 2500f;
		float startSpeed = 0f;
		float endSpeed = 315f;
		int num = 10;
		float interval = (endSpeed - startSpeed) / (float)num;
		DataGraph graphFwd = DataGraph.CreateGraph("Speed Test (FWD)", new Vector3(-100f, 300f));
		for (float speed3 = startSpeed; speed3 <= endSpeed; speed3 += interval)
		{
			StartCoroutine(RangeTest(speed3, testAltitude, forwardTransform, graphFwd, speed3));
			yield return new WaitForSeconds(0.1f);
		}
		DataGraph graphRear = DataGraph.CreateGraph("Speed Test (REAR)", new Vector3(-100f, 100f));
		for (float speed3 = startSpeed; speed3 <= endSpeed; speed3 += interval)
		{
			StartCoroutine(RangeTest(speed3, testAltitude, rearTransform, graphRear, speed3));
			yield return new WaitForSeconds(0.1f);
		}
		DataGraph graphLeft = DataGraph.CreateGraph("Speed Test (LEFT)", new Vector3(-100f, -100f));
		for (float speed3 = startSpeed; speed3 <= endSpeed; speed3 += interval)
		{
			StartCoroutine(RangeTest(speed3, testAltitude, leftTransform, graphLeft, speed3));
			yield return new WaitForSeconds(0.1f);
		}
	}

	private IEnumerator RangeTest(float speed, float altitude, Transform target, DataGraph graph, float xVal)
	{
		TestLaunch(speed * Vector3.forward, altitude, target, out var freeFallImpactPoint, out var launchedMissile);
		FixedPoint basePosition = new FixedPoint(freeFallImpactPoint);
		FixedPoint actualPosition = default(FixedPoint);
		while ((bool)launchedMissile)
		{
			actualPosition.point = launchedMissile.transform.position;
			yield return null;
		}
		float magnitude = (basePosition.point - actualPosition.point).magnitude;
		graph.AddValue(new Vector2(xVal, magnitude));
	}
}
