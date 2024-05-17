using UnityEngine;

public class PassengerBayLoadingTrigger : MonoBehaviour
{
	public PassengerBay bay;

	private Vector3 lp;

	private Quaternion ftRot;

	private void Start()
	{
		lp = base.transform.localPosition;
		ftRot = base.transform.localRotation;
		base.transform.parent = null;
		base.transform.localScale = Vector3.one;
		base.gameObject.AddComponent<Rigidbody>().isKinematic = true;
	}

	private void LateUpdate()
	{
		base.transform.position = bay.transform.TransformPoint(lp);
		base.transform.rotation = bay.transform.rotation * ftRot;
	}

	private void OnTriggerEnter(Collider col)
	{
		if (bay.rampState == PassengerBay.RampStates.Open)
		{
			Soldier componentInParent = col.GetComponentInParent<Soldier>();
			if ((bool)componentInParent && componentInParent.waitingForPickup && !componentInParent.isLoadedInBay)
			{
				bay.LoadSoldier(componentInParent);
			}
		}
	}
}
