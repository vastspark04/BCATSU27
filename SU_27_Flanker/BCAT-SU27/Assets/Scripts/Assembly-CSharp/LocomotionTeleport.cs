using UnityEngine;

public class LocomotionTeleport : MonoBehaviour
{
	public enum AimCollisionTypes
	{
		Point = 0,
		Sphere = 1,
		Capsule = 2,
	}

	public bool EnableMovementDuringReady;
	public bool EnableMovementDuringAim;
	public bool EnableMovementDuringPreTeleport;
	public bool EnableMovementDuringPostTeleport;
	public bool EnableRotationDuringReady;
	public bool EnableRotationDuringAim;
	public bool EnableRotationDuringPreTeleport;
	public bool EnableRotationDuringPostTeleport;
	public TeleportDestination TeleportDestinationPrefab;
	public AimCollisionTypes AimCollisionType;
	public bool UseCharacterCollisionData;
	public float AimCollisionRadius;
	public float AimCollisionHeight;
}
