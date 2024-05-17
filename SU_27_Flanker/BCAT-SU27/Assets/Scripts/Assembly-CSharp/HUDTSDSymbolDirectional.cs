using UnityEngine;

public class HUDTSDSymbolDirectional : HUDTSDSymbol
{
	public bool doHotCold;

	public GameObject dirObj;

	public GameObject hotObj;

	public GameObject coldObj;

	protected override void UpdateRotation(TacticalSituationController.TSActorTargetInfo aInfo)
	{
		Vector3 point = aInfo.point;
		if (doHotCold)
		{
			float num = Vector3.Dot((point - VRHead.position).normalized, aInfo.velocity.normalized);
			if (Mathf.Abs(num) > 0.9f)
			{
				dirObj.SetActive(value: false);
				tf.rotation = Quaternion.LookRotation(aInfo.point - VRHead.position, tsc.transform.up);
				if (num > 0f)
				{
					coldObj.SetActive(value: true);
					hotObj.SetActive(value: false);
				}
				else
				{
					coldObj.SetActive(value: false);
					hotObj.SetActive(value: true);
				}
			}
			else
			{
				hotObj.SetActive(value: false);
				coldObj.SetActive(value: false);
				dirObj.SetActive(value: true);
				point += aInfo.velocity * (Time.time - aInfo.detectionTime);
				Vector3 vector = point - VRHead.position;
				tf.rotation = Quaternion.LookRotation(vector, Vector3.ProjectOnPlane(aInfo.velocity, vector));
			}
		}
		else
		{
			point += aInfo.velocity * (Time.time - aInfo.detectionTime);
			Vector3 vector2 = point - VRHead.position;
			tf.rotation = Quaternion.LookRotation(vector2, Vector3.ProjectOnPlane(aInfo.velocity, vector2));
		}
	}
}
