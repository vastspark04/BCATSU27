using UnityEngine;

public class AIAirTankerSpawn : AIAircraftSpawn, IHasRefuelWaypoint
{
	public Transform refuelWptTransform;

	public RefuelPlane refuelPlane;

	public Transform GetRefuelWaypoint()
	{
		return refuelWptTransform;
	}
}
