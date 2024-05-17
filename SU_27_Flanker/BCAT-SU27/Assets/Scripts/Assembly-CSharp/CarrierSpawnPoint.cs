using System;
using UnityEngine;

[Serializable]
public class CarrierSpawnPoint
{
	public Transform spawnTf;

	public FollowPath catapultPath;

	public CarrierCatapult catapult;

	public FollowPath returnPath;

	public FollowPath stoPath;
}
