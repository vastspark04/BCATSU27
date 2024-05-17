using UnityEngine;

public interface IVTOLWeapon
{
	string GetWeaponName();

	int GetAmmoCount();

	Vector3 GetAimPoint();

	void OnStartFire();

	void OnStopFire();

	void OnEnableWeapon();

	void OnDisableWeapon();

	int GetReticleIndex();
}
