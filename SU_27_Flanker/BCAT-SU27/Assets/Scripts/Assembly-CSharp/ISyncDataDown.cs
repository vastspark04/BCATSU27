using UnityEngine;

public interface ISyncDataDown
{
	float Ping { get; }

	float Timestamp { get; }

	Vector3 GetNextVector3();

	float GetNextFloat();

	int GetNextInt();

	ulong GetNextULong();

	Quaternion GetNextQuaternion();
}
