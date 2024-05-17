using UnityEngine;

public class HUDTSDSymbol : MonoBehaviour
{
	[HideInInspector]
	public TacticalSituationController tsc;

	[HideInInspector]
	public HUDTacticalSituationSymbols hudController;

	public GameObject radarSymbol;

	private int dataIdx;

	protected Transform tf;

	private float depth;

	public void InitSymbol(TacticalSituationController tsc, HUDTacticalSituationSymbols hc, int dataIdx, float depth)
	{
		this.tsc = tsc;
		hudController = hc;
		this.dataIdx = dataIdx;
		tf = base.transform;
		this.depth = depth;
	}

	public void UpdateSymbol()
	{
		TacticalSituationController.TSTargetInfo tSTargetInfo = tsc.infos[dataIdx];
		TacticalSituationController.TargetStates targetState = tsc.GetTargetState(tSTargetInfo);
		if (targetState != TacticalSituationController.TargetStates.Lost && Vector3.SqrMagnitude(tSTargetInfo.point - VRHead.position) > hudController.minSqrDist)
		{
			if (targetState == TacticalSituationController.TargetStates.Uncertain)
			{
				tf.gameObject.SetActive(Mathf.RoundToInt((Time.time - tSTargetInfo.detectionTime) * 10f) % 2 == 0);
			}
			else
			{
				tf.gameObject.SetActive(value: true);
			}
			TacticalSituationController.TSActorTargetInfo tSActorTargetInfo = (TacticalSituationController.TSActorTargetInfo)tSTargetInfo;
			_ = tSActorTargetInfo.actor;
			Vector3 vector = tSActorTargetInfo.point + tSActorTargetInfo.velocity * (Time.time - tSActorTargetInfo.detectionTime);
			UpdateRotation(tSActorTargetInfo);
			if ((bool)radarSymbol)
			{
				radarSymbol.SetActive(tSActorTargetInfo.radar);
			}
			Vector3 position = VRHead.position + (vector - VRHead.position).normalized * depth;
			tf.position = position;
		}
		else
		{
			tf.gameObject.SetActive(value: false);
		}
	}

	protected virtual void UpdateRotation(TacticalSituationController.TSActorTargetInfo aInfo)
	{
		tf.rotation = Quaternion.LookRotation(aInfo.point - VRHead.position, tsc.transform.up);
	}
}
