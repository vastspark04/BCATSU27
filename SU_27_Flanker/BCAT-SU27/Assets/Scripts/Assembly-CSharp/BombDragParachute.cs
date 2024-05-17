using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BombDragParachute : MonoBehaviour
{
	public Vector3 targetScale = Vector3.one;

	public Vector3 deploySpeed = Vector3.one;

	public float deployDelay;

	public float deployTime = 0.5f;

	public float deployedDrag;

	public float deployedDragZOffset;

	public Missile missileModule;

	public SimpleDrag dragModule;

	private bool deployed;

	private void Awake()
	{
		Missile.LaunchEvent item = default(Missile.LaunchEvent);
		item.delay = deployDelay;
		item.launchEvent = new UnityEvent();
		item.launchEvent.AddListener(Deploy);
		missileModule.launchEvents.Add(item);
	}

	public void Deploy()
	{
		if (!deployed && base.gameObject.activeSelf && base.enabled)
		{
			deployed = true;
			StartCoroutine(DeployRoutine());
		}
	}

	private IEnumerator DeployRoutine()
	{
		StartCoroutine(DragRoutine());
		float t = 0f;
		Vector3 origScale = base.transform.localScale;
		float deployRate = 1f / deployTime;
		while (t < 1f)
		{
			Vector3 zero = Vector3.zero;
			zero.x = Mathf.Lerp(origScale.x, targetScale.x, deploySpeed.x * t);
			zero.y = Mathf.Lerp(origScale.y, targetScale.y, deploySpeed.y * t);
			zero.z = Mathf.Lerp(origScale.z, targetScale.z, deploySpeed.z * t);
			base.transform.localScale = zero;
			t += Time.deltaTime * deployRate;
			yield return null;
		}
		base.transform.localScale = targetScale;
	}

	private IEnumerator DragRoutine()
	{
		yield return new WaitForSeconds(deployTime / 2f);
		dragModule.SetDragArea(deployedDrag);
		dragModule.SetZOffset(deployedDragZOffset);
	}
}
