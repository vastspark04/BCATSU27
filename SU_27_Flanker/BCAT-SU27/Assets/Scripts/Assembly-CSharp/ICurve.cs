using UnityEngine;

public interface ICurve
{
	Vector3D GetPoint(float t);

	Vector3 GetTangent(float t);
}
