using System;
using UnityEngine;

public class HUDNoseIndicator : MonoBehaviour
{
	public FlightInfo flightInfo;

	private void Start()
	{
		CollimatedHUDUI componentInParent = GetComponentInParent<CollimatedHUDUI>();
		float num = VectorUtils.SignedAngle(componentInParent.transform.forward, flightInfo.aoaReferenceTf.forward, componentInParent.transform.up);
		float y = componentInParent.depth * Mathf.Sin(num * ((float)Math.PI / 180f));
		base.transform.localPosition = new Vector3(0f, y, 0f);
	}
}
