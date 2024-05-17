using System;
using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class ArticulatingHardpoint : MonoBehaviour, IQSVehicleComponent
{
	[Serializable]
	public class Hardpoint
	{
		public int hpIdx;

		public Transform articulationTf;

		public float maxTilt;

		public float rotationRate;

		public float currentTilt { get; set; }

		public int arrIdx { get; set; }
	}

	public delegate void TiltDelegate(int arrIdx, float tiltAngle);

	public WeaponManager wm;

	public Hardpoint[] hardpoints;

	private bool _auto;

	public bool autoMode
	{
		get
		{
			return _auto;
		}
		set
		{
			if (value != _auto)
			{
				_auto = value;
				this.OnSetAuto?.Invoke(_auto);
			}
		}
	}

	public bool remoteOnly { get; set; }

	public event Action<bool> OnSetAuto;

	public event TiltDelegate OnSetTilt;

	private void Awake()
	{
		for (int i = 0; i < hardpoints.Length; i++)
		{
			hardpoints[i].arrIdx = i;
		}
	}

	private void Update()
	{
		if (remoteOnly || !autoMode)
		{
			return;
		}
		for (int i = 0; i < hardpoints.Length; i++)
		{
			Hardpoint hardpoint = hardpoints[i];
			HPEquippable equip = wm.GetEquip(hardpoint.hpIdx);
			if ((bool)equip && equip.itemActivated)
			{
				if (equip is RocketLauncher)
				{
					UpdateRocketLauncher(hardpoint, (RocketLauncher)equip);
					break;
				}
				if (equip is HPEquipOpticalML)
				{
					UpdateAGM(hardpoint, (HPEquipOpticalML)equip);
				}
			}
		}
	}

	public void SetTilt(Hardpoint hp, float t, bool immediate = false, bool sendEvent = true)
	{
		P_SetTilt(hp, t, immediate, sendEvent);
	}

	public void RemoteSetTilt(int arrIdx, float t)
	{
		P_SetTilt(hardpoints[arrIdx], t, immediate: true, sendEvent: false);
	}

	private void P_SetTilt(Hardpoint hp, float t, bool immediate = false, bool sendEvent = true)
	{
		t = Mathf.Clamp(t, 0f - hp.maxTilt, hp.maxTilt);
		if (immediate)
		{
			hp.currentTilt = t;
		}
		else
		{
			hp.currentTilt = Mathf.MoveTowards(hp.currentTilt, t, hp.rotationRate * Time.deltaTime);
		}
		Quaternion localRotation = Quaternion.AngleAxis(hp.currentTilt, Vector3.right);
		hp.articulationTf.localRotation = localRotation;
		if (sendEvent)
		{
			this.OnSetTilt?.Invoke(hp.arrIdx, hp.currentTilt);
		}
	}

	private void UpdateRocketLauncher(Hardpoint hp, RocketLauncher rl)
	{
		if (!rl.itemActivated)
		{
			return;
		}
		float t;
		if ((bool)wm.opticalTargeter && wm.opticalTargeter.locked)
		{
			_ = hp.articulationTf.parent.forward;
			Vector3 toDirection = Vector3.ProjectOnPlane(rl.GetAimPoint() - hp.articulationTf.position, hp.articulationTf.right);
			Vector3 toDirection2 = Vector3.ProjectOnPlane(wm.opticalTargeter.lockTransform.position - hp.articulationTf.position, hp.articulationTf.right);
			float num = VectorUtils.SignedAngle(hp.articulationTf.forward, toDirection, hp.articulationTf.up);
			float num2 = VectorUtils.SignedAngle(hp.articulationTf.parent.forward, toDirection2, hp.articulationTf.parent.up);
			t = num - num2;
		}
		else
		{
			t = 0f;
		}
		Hardpoint[] array = hardpoints;
		foreach (Hardpoint hardpoint in array)
		{
			HPEquippable equip = wm.GetEquip(hardpoint.hpIdx);
			if ((bool)equip && equip.shortName == rl.shortName)
			{
				SetTilt(hardpoint, t);
			}
		}
	}

	private void UpdateAGM(Hardpoint hp, HPEquipOpticalML mlEq)
	{
		Missile nextMissile = mlEq.ml.GetNextMissile();
		if (!nextMissile)
		{
			return;
		}
		float t;
		if ((bool)wm.opticalTargeter && wm.opticalTargeter.locked)
		{
			float num = nextMissile.maxTorque;
			if (nextMissile.minBallisticCalcSpeed > 0f)
			{
				num = nextMissile.minBallisticCalcSpeed;
			}
			Vector3 missileVelocity = wm.vesselRB.velocity + num * nextMissile.transform.forward;
			Vector3 vector = Missile.BallisticLeadTargetPoint(wm.opticalTargeter.lockTransform.position, wm.opticalTargeter.targetVelocity, nextMissile.transform.position, missileVelocity, missileVelocity.magnitude, nextMissile.leadTimeMultiplier, nextMissile.maxBallisticOffset, nextMissile.maxLeadTime);
			float num2 = (mlEq.autoUncage ? mlEq.autoUncageFraction : mlEq.uncagedFOVFraction) * nextMissile.opticalFOV / 2f;
			num2 *= 0.85f;
			Vector3 target = vector - nextMissile.transform.position;
			target = Vector3.RotateTowards(wm.opticalTargeter.lockTransform.position - nextMissile.transform.position, target, num2 * ((float)Math.PI / 180f), 0f);
			Vector3 toDirection = Vector3.ProjectOnPlane(nextMissile.transform.position + target - hp.articulationTf.position, hp.articulationTf.right);
			t = 0f - VectorUtils.SignedAngle(hp.articulationTf.parent.forward, toDirection, hp.articulationTf.parent.up);
		}
		else
		{
			t = 0f;
		}
		Hardpoint[] array = hardpoints;
		foreach (Hardpoint hardpoint in array)
		{
			HPEquippable equip = wm.GetEquip(hardpoint.hpIdx);
			if ((bool)equip && equip.shortName == mlEq.shortName)
			{
				SetTilt(hardpoint, t);
			}
		}
	}

	public void Tilt(float input, float deltaTime)
	{
		if (remoteOnly)
		{
			return;
		}
		autoMode = false;
		bool flag = false;
		float t = 0f;
		if (!wm.currentEquip)
		{
			return;
		}
		for (int i = 0; i < hardpoints.Length; i++)
		{
			Hardpoint hardpoint = hardpoints[i];
			HPEquippable equip = wm.GetEquip(hardpoint.hpIdx);
			if ((bool)equip && equip.shortName == wm.currentEquip.shortName)
			{
				if (!flag)
				{
					t = hardpoint.currentTilt;
					t = Mathf.Clamp(t + input * hardpoint.rotationRate * deltaTime, 0f - hardpoint.maxTilt, hardpoint.maxTilt);
					flag = true;
				}
				SetTilt(hardpoint, t);
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode("ArticulatingHardpoint").SetValue("autoMode", autoMode);
		for (int i = 0; i < hardpoints.Length; i++)
		{
			qsNode.AddNode($"HP{i}").SetValue("tilt", hardpoints[i].currentTilt);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("ArticulatingHardpoint");
		bool target = false;
		ConfigNodeUtils.TryParseValue(node, "autoMode", ref target);
		autoMode = target;
		for (int i = 0; i < hardpoints.Length; i++)
		{
			string text = $"HP{i}";
			ConfigNode node2 = qsNode.GetNode(text);
			if (node2 != null)
			{
				float target2 = 0f;
				ConfigNodeUtils.TryParseValue(node2, "tilt", ref target2);
				P_SetTilt(hardpoints[i], target2, immediate: true);
			}
		}
	}
}

}