using UnityEngine;

public interface IArtilleryUnit
{
	void FireOnPosition(FixedPoint targetPosition, Vector3 targetVelocity, int shotsPerSalvo, int salvos);

	void FireOnPositionRadius(FixedPoint targetPosition, float radius, int shotsPerSalvo, int salvos);

	void FireOnActor(Actor a, int shotsPerSalvo, int salvos);

	void ClearFireOrders();
}
