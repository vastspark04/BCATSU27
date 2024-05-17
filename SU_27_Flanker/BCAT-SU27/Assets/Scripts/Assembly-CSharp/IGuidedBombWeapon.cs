using UnityEngine;

public interface IGuidedBombWeapon
{
	float GetDeployRadius(Vector3 targetPosition);

	Vector3 GetImpactPoint();

	bool HasGuidedBombTarget();

	bool IsDumbMode();
}
