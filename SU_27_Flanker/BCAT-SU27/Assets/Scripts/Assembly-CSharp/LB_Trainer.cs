using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class LB_Trainer : MonoBehaviour
{
	public LB_TrainingPilot pilotA;

	public LB_TrainingPilot pilotB;

	private int currentIteration;

	public int trainingIterations;

	public bool visualizeTraining = true;

	public bool previewCertaintyMap;

	public int cmapStartSize = 128;

	public bool loadExistingData = true;

	public bool saveData = true;

	public bool pauseAfterTraining = true;

	public bool randomResetPosition = true;

	private float estTime = -1f;

	private int winsA;

	private int winsB;

	private LB_SimPilot sp_a;

	private LB_SimPilot sp_b;

	private Texture2D cmap_a;

	private Texture2D cmap_b;

	private void Start()
	{
		StartCoroutine(TrainingRoutine());
	}

	private IEnumerator TrainingRoutine()
	{
		string sp_a_path = Path.Combine(Path.GetFullPath("."), "Assets\\BDLearningBot\\Databases\\sp_a.ldb");
		Vector3 initialPosA = pilotA.transform.position;
		Quaternion initialRotA = pilotA.transform.rotation;
		Vector3 initialPosB = pilotB.transform.position;
		Quaternion initialRotB = pilotB.transform.rotation;
		sp_a = new LB_SimPilot();
		SetupSim(sp_a, pilotA);
		sp_a.OnWin += delegate
		{
			winsA++;
		};
		if (loadExistingData)
		{
			sp_a.CreateBot();
			sp_a.learningBot.LoadDatabase(sp_a_path);
		}
		sp_b = new LB_SimPilot();
		SetupSim(sp_b, pilotB);
		sp_b.OnWin += delegate
		{
			winsB++;
		};
		sp_b.learn = false;
		Stopwatch sw = new Stopwatch();
		sw.Start();
		sp_a.opponent = sp_b;
		sp_b.opponent = sp_a;
		float timeStart = Time.realtimeSinceStartup;
		int cmap_interval = 30;
		int cmapIdx = 0;
		cmap_a = new Texture2D(cmapStartSize, cmapStartSize);
		cmap_a.filterMode = FilterMode.Point;
		for (currentIteration = 0; currentIteration < trainingIterations; currentIteration++)
		{
			if (randomResetPosition)
			{
				sp_a.position = RandomStartPos();
				sp_a.rotation = RandomStartRot();
				sp_b.position = RandomStartPos();
				sp_b.rotation = RandomStartRot();
			}
			sp_a.velocity = pilotA.initialSpeed * sp_a.forward;
			sp_b.velocity = pilotB.initialSpeed * sp_b.forward;
			sp_a.BeginSim();
			sp_b.BeginSim();
			while (!sp_a.isStandby && !sp_b.isStandby)
			{
				bool flag = false;
				try
				{
					sp_a.Update(0.04f);
					sp_b.Update(0.04f);
				}
				catch (NullReferenceException message)
				{
					UnityEngine.Debug.LogError(message);
					pilotA.UpdateModel(sp_a);
					pilotB.UpdateModel(sp_b);
					flag = true;
				}
				if (flag)
				{
					yield break;
				}
				if (visualizeTraining)
				{
					pilotA.UpdateModel(sp_a);
					pilotB.UpdateModel(sp_b);
					yield return null;
				}
				else
				{
					if (sw.ElapsedMilliseconds <= 30)
					{
						continue;
					}
					sw.Stop();
					pilotA.UpdateModel(sp_a);
					pilotB.UpdateModel(sp_b);
					yield return null;
					if (previewCertaintyMap)
					{
						cmapIdx = (cmapIdx + 1) % cmap_interval;
						if (cmapIdx == 0)
						{
							cmap_a = sp_a.GetCertaintyMap(cmap_a);
						}
					}
					sw.Reset();
					sw.Start();
				}
			}
			float num = Time.realtimeSinceStartup - timeStart;
			float num2 = (float)currentIteration / (float)trainingIterations;
			estTime = num / num2 - num;
			if (!randomResetPosition)
			{
				sp_a.position = initialPosA;
				sp_a.rotation = initialRotA;
				sp_b.position = initialPosB;
				sp_b.rotation = initialRotB;
			}
		}
		if (saveData)
		{
			sp_a.learningBot.SaveDatabase(sp_a_path);
		}
		StartCoroutine(ShowcaseRoutine());
	}

	private Vector3 RandomStartPos()
	{
		return new Vector3(UnityEngine.Random.Range(-2500, 2500), UnityEngine.Random.Range(100, 2500), UnityEngine.Random.Range(-2500, 2500));
	}

	private Quaternion RandomStartRot()
	{
		return Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up);
	}

	private void SetupSim(LB_SimPilot sp, LB_TrainingPilot p)
	{
		sp.position = p.transform.position;
		sp.rotation = p.transform.rotation;
		sp.velocity = p.initialSpeed * p.transform.forward;
		sp.SetTargetSpeed(p.initialSpeed);
		sp.maxG = p.maxG;
		sp.thrust = p.thrust;
		sp.mass = p.mass;
		sp.liftFactor = p.liftFactor;
		sp.dragFactor = p.dragFactor;
		sp.rollRate = p.rollRate;
		sp.maxAoA = p.maxAoA;
		sp.minTargetSpeed = p.minTargetSpeed;
		sp.maxTargetSpeed = p.maxTargetSpeed;
		sp.directions = p.directions;
	}

	private IEnumerator ShowcaseRoutine()
	{
		cmap_a = sp_a.GetCertaintyMap(cmap_a);
		pilotA.UpdateModel(sp_a);
		pilotB.UpdateModel(sp_b);
		Vector3 initialPosA = pilotA.transform.position;
		Quaternion initialRotA = pilotA.transform.rotation;
		Vector3 initialPosB = pilotB.transform.position;
		Quaternion initialRotB = pilotB.transform.rotation;
		while (base.enabled)
		{
			if (randomResetPosition)
			{
				sp_a.position = RandomStartPos();
				sp_a.rotation = RandomStartRot();
				sp_b.position = RandomStartPos();
				sp_b.rotation = RandomStartRot();
			}
			else
			{
				sp_a.position = initialPosA;
				sp_a.rotation = initialRotA;
				sp_b.position = initialPosB;
				sp_b.rotation = initialRotB;
			}
			sp_a.velocity = pilotA.initialSpeed * sp_a.forward;
			sp_b.velocity = pilotB.initialSpeed * sp_b.forward;
			sp_a.BeginSim();
			sp_b.BeginSim();
			while (!sp_a.isStandby && !sp_b.isStandby)
			{
				sp_a.Update(4f * Time.deltaTime);
				sp_b.Update(4f * Time.deltaTime);
				pilotA.UpdateModel(sp_a);
				pilotB.UpdateModel(sp_b);
				yield return null;
			}
			yield return null;
		}
	}

	private void OnGUI()
	{
		GUI.Label(new Rect(10f, 10f, 900f, 200f), $"Training: {currentIteration}/{trainingIterations} ({Mathf.Round((float)currentIteration / (float)trainingIterations * 100f)}% remain: {FormattedTime(estTime)})\n" + $"Wins: A({winsA})  B({winsB})\n" + "   " + ((float)winsA / (float)(winsA + winsB) * 100f).ToString("0.0") + "% wins");
		if (currentIteration < trainingIterations && GUI.Button(new Rect(900f, 10f, 100f, 20f), "Stop Training"))
		{
			trainingIterations = currentIteration;
		}
		if ((bool)cmap_a)
		{
			GUI.DrawTexture(new Rect(10f, 60f, 256f, 256f), cmap_a);
		}
		if ((bool)cmap_b)
		{
			GUI.DrawTexture(new Rect(10f, 336f, 256f, 256f), cmap_b);
		}
	}

	private string FormattedTime(float s)
	{
		int num = Mathf.FloorToInt(s);
		int num2 = num % 60;
		int num3 = (num - num2) / 60;
		int num4 = num3 % 60;
		int num5 = (num3 - num4) / 60;
		return string.Format("[{0}:{1}:{2}]", num5, num4.ToString("00"), num2.ToString("00"));
	}
}
