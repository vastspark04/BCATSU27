using UnityEngine;

public class UnloadingZone : MonoBehaviour
{
	public Transform unloadRallyPoint;

	public Waypoint unloadRallyWpt;

	public FollowPath[] unloadPaths;

	public float radius;

	private float sqrRadius;

	public int dropoffObjectiveID;

	private void Start()
	{
		sqrRadius = radius * radius;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawWireSphere(base.transform.position, radius);
	}

	private void Update()
	{
		foreach (PassengerBay passengerBay in PassengerBay.passengerBays)
		{
			if ((bool)passengerBay)
			{
				if ((passengerBay.transform.position - base.transform.position).sqrMagnitude < sqrRadius)
				{
					passengerBay.inUnloadZone = this;
				}
				else if ((bool)passengerBay && passengerBay.inUnloadZone == this)
				{
					passengerBay.inUnloadZone = null;
				}
			}
		}
	}
}
