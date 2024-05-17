using System;
using UnityEngine;

public class ChaffCountermeasure : Countermeasure, IQSVehicleComponent
{
	public struct Chaff
	{
		private FixedPoint fp;

		private float timeStart;

		private float effectiveness;

		private float effectDecay;

		private float decayTime;

		public Vector3 position => fp.point;

		public float elapsedTime => Time.time - timeStart;

		public float effectValue => Mathf.Max(0f, effectiveness - (Time.time - timeStart) * effectDecay);

		public bool decayed => Time.time - timeStart > decayTime;

		public Chaff(ChaffCountermeasure cm)
		{
			fp = new FixedPoint(cm.transform.position);
			timeStart = Time.time;
			effectiveness = cm.effectiveness;
			effectDecay = cm.effectDecay;
			decayTime = effectiveness / effectDecay;
		}

		public Chaff(ChaffCountermeasure cm, float timeStart, Vector3D globalPos)
		{
			fp = new FixedPoint(VTMapManager.GlobalToWorldPoint(globalPos));
			this.timeStart = timeStart;
			effectiveness = cm.effectiveness;
			effectDecay = cm.effectDecay;
			decayTime = effectiveness / effectDecay;
		}
	}

	public float effectiveness;

	public float effectDecay;

	public float maxEffectiveness;

	private float magnitude;

	private ChaffLauncher[] launchers;

	public Actor myActor;

	private int altIdx;

	private const int CHAFF_PERSIST_COUNT = 16;

	private Chaff[] chaffs = new Chaff[16];

	private int currChaffIdx;

	private const float CLOSING_SPEED_MULT = 0.45f;

	private const float CLOSING_SPEED_POW = 1.5f;

	private const float ADV_CHAFF_EFFECTIVENESS_MULTIPLIER = 0.75f;

	private string qsNodeName => base.gameObject.name + "_ChaffCountermeasure";

	private void Start()
	{
		if (!myActor)
		{
			Actor[] componentsInParent = GetComponentsInParent<Actor>(includeInactive: true);
			for (int i = 0; i < componentsInParent.Length; i++)
			{
				Actor actor = (myActor = componentsInParent[i]);
			}
		}
		launchers = GetComponentsInChildren<ChaffLauncher>();
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(qsNodeName);
		if (Radar.ADV_RADAR)
		{
			Chaff[] array = chaffs;
			for (int i = 0; i < array.Length; i++)
			{
				Chaff chaff = array[i];
				if (!chaff.decayed)
				{
					ConfigNode configNode2 = new ConfigNode("CHAFF");
					configNode2.SetValue("elapsedTime", chaff.elapsedTime);
					configNode2.SetValue("globalPos", VTMapManager.WorldToGlobalPoint(chaff.position));
					configNode.AddNode(configNode2);
				}
			}
		}
		else
		{
			configNode.SetValue("magnitude", magnitude);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (!qsNode.HasNode(qsNodeName))
		{
			return;
		}
		ConfigNode node = qsNode.GetNode(qsNodeName);
		if (Radar.ADV_RADAR)
		{
			currChaffIdx = 0;
			{
				foreach (ConfigNode node2 in node.GetNodes("CHAFF"))
				{
					float value = node2.GetValue<float>("elapsedTime");
					Vector3D value2 = node2.GetValue<Vector3D>("globalPos");
					Chaff chaff = new Chaff(this, Time.time - value, value2);
					chaffs[currChaffIdx] = chaff;
					currChaffIdx++;
				}
				return;
			}
		}
		magnitude = node.GetValue<float>("magnitude");
	}

	protected override void OnFireCM()
	{
		if ((bool)manager)
		{
			switch (manager.releaseMode)
			{
			case CountermeasureManager.ReleaseModes.Single_Auto:
				TryFire(ref altIdx);
				break;
			case CountermeasureManager.ReleaseModes.Single_L:
			{
				int idx2 = 0;
				TryFire(ref idx2);
				break;
			}
			case CountermeasureManager.ReleaseModes.Single_R:
			{
				int idx = 1;
				TryFire(ref idx);
				break;
			}
			case CountermeasureManager.ReleaseModes.Double:
			{
				for (int i = 0; i < launchers.Length; i++)
				{
					if (ConsumeCM(i))
					{
						launchers[i].FireChaff();
						InternalChaff();
					}
				}
				break;
			}
			}
		}
		else
		{
			TryFire(ref altIdx);
		}
	}

	private void TryFire(ref int idx)
	{
		if (launchers == null)
		{
			return;
		}
		bool flag = false;
		if (ConsumeCM(idx))
		{
			flag = true;
		}
		else
		{
			idx = (idx + 1) % 2;
			if (ConsumeCM(idx))
			{
				flag = true;
			}
		}
		if (flag)
		{
			launchers[idx].FireChaff();
			InternalChaff();
			idx = (idx + 1) % 2;
		}
	}

	private void InternalChaff()
	{
		if (Radar.ADV_RADAR)
		{
			AdvRdrChaff();
			return;
		}
		magnitude += effectiveness;
		magnitude = Mathf.Min(magnitude, maxEffectiveness);
	}

	public float GetMagnitude()
	{
		return magnitude;
	}

	private void AdvRdrChaff()
	{
		for (int i = 0; i < 16; i++)
		{
			int num = (i + currChaffIdx) % 16;
			if (chaffs[num].decayed)
			{
				chaffs[num] = new Chaff(this);
				currChaffIdx = (currChaffIdx + 1) % 16;
				return;
			}
		}
		chaffs[currChaffIdx] = new Chaff(this);
		currChaffIdx = (currChaffIdx + 1) % 16;
	}

	public bool GetAdvChaffAffectedPos(Vector3 radarPos, Vector3 radarLookDir, float radarLookFOV, out Vector3 affectedPos, out Vector3 affectedVel)
	{
		if (!myActor)
		{
			Actor[] componentsInParent = GetComponentsInParent<Actor>(includeInactive: true);
			for (int i = 0; i < componentsInParent.Length; i++)
			{
				Actor actor = (myActor = componentsInParent[i]);
			}
		}
		Vector3 position = myActor.position;
		Vector3 velocity = myActor.velocity;
		Vector3 normalized = (position - radarPos).normalized;
		float num = Mathf.Abs(Vector3.Dot(velocity, normalized));
		float num2 = Mathf.Max(0.001f, Mathf.Pow(num * 0.45f, 1.5f));
		Vector3 zero = Vector3.zero;
		float num3 = 0f;
		float num4 = Mathf.Cos(radarLookFOV * ((float)Math.PI / 180f));
		radarLookDir.Normalize();
		for (int j = 0; j < 16; j++)
		{
			Chaff chaff = chaffs[j];
			if (!chaff.decayed)
			{
				if (Vector3.Dot((chaffs[j].position - radarPos).normalized, radarLookDir) > num4)
				{
					float num5 = chaff.effectValue * 0.75f;
					zero += chaff.position * num5;
					num3 += num5;
					Debug.DrawLine(radarPos, chaff.position, Color.cyan);
				}
				else
				{
					Debug.DrawLine(radarPos, chaff.position, Color.magenta);
				}
			}
		}
		if (Vector3.Dot(radarLookDir, normalized) > num4)
		{
			zero += position * num2;
			num3 += num2;
			Debug.DrawLine(radarPos, position, Color.cyan);
		}
		else
		{
			num2 = 0f;
			Debug.DrawLine(radarPos, position, Color.magenta);
		}
		if (num3 > 0f)
		{
			zero /= num3;
			affectedPos = zero;
			affectedVel = velocity * num2 / num3;
			Debug.DrawLine(radarPos, affectedPos, Color.yellow);
			return true;
		}
		affectedPos = position;
		affectedVel = Vector3.zero;
		return false;
	}
}
