using System.Collections;
using UnityEngine;

public class VLSTurnBooster : MonoBehaviour
{
	private Missile m;

	public TimedEvents timedEvents;

	public float turnDelay = 0.75f;

	public SolidBooster turnBooster;

	public SolidBooster haltBooster;

	public float turnAngleTarget = 80f;

	public float haltAngDrag = 30f;

	public float haltDragDelay = 0.15f;

	private void Awake()
	{
		m = GetComponentInParent<Missile>();
	}

	public void BeginTurnBooster()
	{
		Vector3 estTargetPos = m.estTargetPos;
		Vector3 vector = base.transform.parent.InverseTransformVector(base.transform.position - estTargetPos);
		base.transform.localRotation = Quaternion.LookRotation(Vector3.forward, -vector);
		StartCoroutine(TurnRoutine());
	}

	private IEnumerator TurnRoutine()
	{
		yield return new WaitForSeconds(turnDelay);
		turnBooster.Fire();
		while (Vector3.Angle(base.transform.forward, Vector3.up) < turnAngleTarget)
		{
			yield return null;
		}
		haltBooster.Fire();
		yield return new WaitForSeconds(haltDragDelay);
		GetComponentInParent<Rigidbody>().angularDrag = haltAngDrag;
	}
}
