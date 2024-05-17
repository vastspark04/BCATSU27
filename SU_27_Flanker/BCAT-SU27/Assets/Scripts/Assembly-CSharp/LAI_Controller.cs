using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class LAI_Controller : MonoBehaviour
{
	public int testIterations = 2000;

	public float mass;

	public float thrust;

	public float liftFactor;

	public float dragFactor;

	public float maxG = 9f;

	public float maxAoA = 25f;

	public float rollRate;

	private LAI_Pilot pilot_a;

	private LAI_Pilot pilot_b;

	public GameObject modelPrefab;

	private LAI_Pilot.TrainingLibrary tl;

	public bool doLearning;

	private int doneIterations;

	private Transform aModel;

	private Transform bModel;

	public void Start()
	{
		tl = new LAI_Pilot.TrainingLibrary();
		LAI_Pilot.TrainingLibrary trainingLibrary = new LAI_Pilot.TrainingLibrary();
		pilot_a = new LAI_Pilot(tl);
		pilot_b = new LAI_Pilot(trainingLibrary);
		pilot_a.opponent = pilot_b;
		pilot_b.opponent = pilot_a;
		SetSpecs(pilot_a, mass, thrust, liftFactor, dragFactor, maxG, maxAoA, rollRate);
		SetSpecs(pilot_b, mass, thrust, liftFactor, dragFactor, maxG, maxAoA, rollRate);
		aModel = Object.Instantiate(modelPrefab, modelPrefab.transform.parent).transform;
		bModel = Object.Instantiate(modelPrefab, modelPrefab.transform.parent).transform;
		modelPrefab.SetActive(value: false);
		ResetPilots();
		StartCoroutine(TestRoutine());
	}

	private void SetSpecs(LAI_Pilot pilot, float mass, float thrust, float liftFactor, float dragFactor, float maxG, float maxAoA, float rollRate)
	{
		pilot.mass = mass;
		pilot.thrust = thrust;
		pilot.liftFactor = liftFactor;
		pilot.dragFactor = dragFactor;
		pilot.maxG = maxG;
		pilot.maxAoA = maxAoA;
		pilot.rollRate = rollRate;
	}

	private IEnumerator TestRoutine()
	{
		yield return null;
		doneIterations = 0;
		if (doLearning)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			int iterations = testIterations;
			for (int i = 0; i < iterations; i++)
			{
				bool next = false;
				while (!next)
				{
					bool flag = pilot_a.Update(0.02f);
					bool flag2 = pilot_b.Update(0.02f);
					if (flag || flag2 || (pilot_a.position - pilot_b.position).sqrMagnitude > 100000000f)
					{
						if (flag != flag2)
						{
							if (flag)
							{
								pilot_a.RewardKill();
								pilot_b.PunishDeath();
							}
							else
							{
								pilot_b.RewardKill();
								pilot_a.PunishDeath();
							}
						}
						ResetPilotsRandom();
						next = true;
					}
					if (sw.ElapsedMilliseconds > 50)
					{
						sw.Stop();
						sw.Reset();
						sw.Start();
						UpdateModels();
						yield return null;
					}
				}
				doneIterations = i + 1;
			}
		}
		if (doLearning)
		{
			pilot_a.useBestDecision = true;
			pilot_b.useBestDecision = true;
		}
		ResetPilots();
		while (base.enabled)
		{
			bool num = pilot_a.Update(Time.deltaTime);
			bool flag3 = pilot_b.Update(Time.deltaTime);
			UpdateModels();
			if (num || flag3 || (pilot_a.position - pilot_b.position).sqrMagnitude > 100000000f)
			{
				ResetPilotsRandom();
			}
			yield return null;
		}
	}

	private void UpdateModels()
	{
		aModel.localPosition = pilot_a.position;
		aModel.localRotation = pilot_a.rotation;
		bModel.localPosition = pilot_b.position;
		bModel.localRotation = pilot_b.rotation;
	}

	private void ResetPilots()
	{
		pilot_a.position = new Vector3(0f, 100f, 0f);
		pilot_b.position = new Vector3(50f, 100f, 0f);
		pilot_a.velocity = 200f * Vector3.forward;
		pilot_b.velocity = 200f * Vector3.back;
	}

	private void ResetPilotsRandom()
	{
		pilot_a.position = new Vector3(Random.Range(-300, 300), Random.Range(50, 300), Random.Range(-300, 300));
		pilot_b.position = new Vector3(Random.Range(-300, 300), Random.Range(50, 300), Random.Range(-300, 300));
		pilot_a.velocity = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * (200f * Vector3.forward);
		pilot_b.velocity = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * (200f * Vector3.forward);
		pilot_a.rotation = Quaternion.LookRotation(pilot_a.velocity, Vector3.up);
		pilot_b.rotation = Quaternion.LookRotation(pilot_b.velocity, Vector3.up);
		pilot_a.ClearHistory();
		pilot_b.ClearHistory();
	}
}
