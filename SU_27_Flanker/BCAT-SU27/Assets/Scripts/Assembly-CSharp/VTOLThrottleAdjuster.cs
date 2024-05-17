using UnityEngine;

public class VTOLThrottleAdjuster : ElectronicComponent, IPersistentDataSaver
{
	public float moveSpeed;

	public Transform heightTransform;

	public float minHeight;

	public float maxHeight;

	public Transform forwardTransform;

	public float minFwd;

	public float maxFwd;

	private void Start()
	{
		if (PilotSaveManager.current != null)
		{
			Vector3 throttlePosition = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).throttlePosition;
			Vector3 localPosition = heightTransform.localPosition;
			localPosition.y = Mathf.Clamp(throttlePosition.y, minHeight, maxHeight);
			heightTransform.localPosition = localPosition;
			Vector3 localPosition2 = forwardTransform.localPosition;
			localPosition2.z = Mathf.Clamp(throttlePosition.z, minFwd, maxFwd);
			forwardTransform.localPosition = localPosition2;
		}
	}

	public void MoveForward()
	{
		if (DrainElectricity(0.1f * Time.deltaTime))
		{
			Vector3 localPosition = forwardTransform.localPosition;
			localPosition.z = Mathf.Min(localPosition.z + moveSpeed * Time.deltaTime, maxFwd);
			forwardTransform.localPosition = localPosition;
		}
	}

	public void MoveBack()
	{
		if (DrainElectricity(0.1f * Time.deltaTime))
		{
			Vector3 localPosition = forwardTransform.localPosition;
			localPosition.z = Mathf.Max(localPosition.z - moveSpeed * Time.deltaTime, minFwd);
			forwardTransform.localPosition = localPosition;
		}
	}

	public void MoveUp()
	{
		if (DrainElectricity(0.1f * Time.deltaTime))
		{
			Vector3 localPosition = heightTransform.localPosition;
			localPosition.y = Mathf.Min(localPosition.y + moveSpeed * Time.deltaTime, maxHeight);
			heightTransform.localPosition = localPosition;
		}
	}

	public void MoveDown()
	{
		if (DrainElectricity(0.1f * Time.deltaTime))
		{
			Vector3 localPosition = heightTransform.localPosition;
			localPosition.y = Mathf.Max(localPosition.y - moveSpeed * Time.deltaTime, minHeight);
			heightTransform.localPosition = localPosition;
		}
	}

	public void SavePersistentData()
	{
		if (PilotSaveManager.current != null)
		{
			Vector3 throttlePosition = new Vector3(0f, heightTransform.localPosition.y, forwardTransform.localPosition.z);
			PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).throttlePosition = throttlePosition;
		}
	}
}
