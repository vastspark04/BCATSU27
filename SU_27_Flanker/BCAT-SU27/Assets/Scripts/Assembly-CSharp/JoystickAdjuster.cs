using UnityEngine;

public class JoystickAdjuster : ElectronicComponent, IPersistentDataSaver
{
	public Transform heightTransform;

	public Vector3 minHeightPos;

	public Vector3 maxHeightPos;

	public Transform forwardTransform;

	public Vector3 minFwdPos;

	public Vector3 maxFwdPos;

	public Transform rightTransform;

	public Vector3 minRightPos;

	public Vector3 maxRightPos;

	public float moveSpeed;

	public float moveSpeedT = 0.5f;

	private Vector3 jPos;

	public VRInteractable joyInteractable;

	public VRInteractable autoAdjustInteractable;

	private bool autoAdjusting;

	private Transform controllerTransform;

	private int rightDir;

	private int fwdDir;

	private int upDir;

	private Vector3 aHeightOffset;

	private Vector3 aFwdOffset;

	private Vector3 aRightOffset;

	private void Start()
	{
		joyInteractable.enabled = true;
		autoAdjustInteractable.enabled = false;
		autoAdjustInteractable.OnStartInteraction += BeginAutoAdjust;
		autoAdjustInteractable.OnStopInteract.AddListener(EndAutoAdjust);
		if (PilotSaveManager.current != null)
		{
			Vector3 vector = (jPos = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).joystickPosition);
			rightTransform.localPosition = Vector3.Lerp(minRightPos, maxRightPos, vector.x);
			heightTransform.localPosition = Vector3.Lerp(minHeightPos, maxHeightPos, vector.y);
			forwardTransform.localPosition = Vector3.Lerp(minFwdPos, maxFwdPos, vector.z);
		}
		else
		{
			jPos = new Vector3(InverseLerpVector(rightTransform.localPosition, minRightPos, maxRightPos), InverseLerpVector(heightTransform.localPosition, minHeightPos, maxHeightPos), InverseLerpVector(forwardTransform.localPosition, minFwdPos, maxFwdPos));
		}
	}

	private void Update()
	{
		if (!battery || DrainElectricity(0.001f))
		{
			if (autoAdjusting)
			{
				UpdateAutoAdjust();
			}
			if (rightDir != 0)
			{
				jPos.x = Mathf.Clamp01(jPos.x + moveSpeedT * Time.deltaTime * (float)rightDir);
				rightTransform.localPosition = Vector3.Lerp(minRightPos, maxRightPos, jPos.x);
			}
			if (fwdDir != 0)
			{
				jPos.z = Mathf.Clamp01(jPos.z + moveSpeedT * Time.deltaTime * (float)fwdDir);
				forwardTransform.localPosition = Vector3.Lerp(minFwdPos, maxFwdPos, jPos.z);
			}
			if (upDir != 0)
			{
				jPos.y = Mathf.Clamp01(jPos.y + moveSpeedT * Time.deltaTime * (float)upDir);
				heightTransform.localPosition = Vector3.Lerp(minHeightPos, maxHeightPos, jPos.y);
			}
		}
	}

	private void UpdateAutoAdjust()
	{
		Vector3 current = heightTransform.parent.InverseTransformPoint(controllerTransform.position) - aHeightOffset;
		heightTransform.localPosition = Vector3.Lerp(minHeightPos, maxHeightPos, InverseLerpVector(current, minHeightPos, maxHeightPos));
		Vector3 current2 = forwardTransform.parent.InverseTransformPoint(controllerTransform.position) - aFwdOffset;
		forwardTransform.localPosition = Vector3.Lerp(minFwdPos, maxFwdPos, InverseLerpVector(current2, minFwdPos, maxFwdPos));
		Vector3 current3 = rightTransform.parent.InverseTransformPoint(controllerTransform.position) - aRightOffset;
		rightTransform.localPosition = Vector3.Lerp(minRightPos, maxRightPos, InverseLerpVector(current3, minRightPos, maxRightPos));
		jPos = new Vector3(InverseLerpVector(rightTransform.localPosition, minRightPos, maxRightPos), InverseLerpVector(heightTransform.localPosition, minHeightPos, maxHeightPos), InverseLerpVector(forwardTransform.localPosition, minFwdPos, maxFwdPos));
	}

	public void BeginAutoAdjust(VRHandController con)
	{
		autoAdjusting = true;
		controllerTransform = con.transform;
		aHeightOffset = heightTransform.parent.InverseTransformPoint(controllerTransform.position) - heightTransform.localPosition;
		aFwdOffset = forwardTransform.parent.InverseTransformPoint(controllerTransform.position) - forwardTransform.localPosition;
		aRightOffset = rightTransform.parent.InverseTransformPoint(controllerTransform.position) - rightTransform.localPosition;
	}

	public void EndAutoAdjust()
	{
		autoAdjusting = false;
		controllerTransform = null;
	}

	public void ToggleAutoAdjustMode()
	{
		joyInteractable.enabled = !joyInteractable.enabled;
		autoAdjustInteractable.enabled = !autoAdjustInteractable.enabled;
	}

	private float InverseLerpVector(Vector3 current, Vector3 a, Vector3 b)
	{
		Vector3 vector = b - a;
		current = Vector3.Project(current, vector);
		Vector3 rhs = current - a;
		float magnitude = vector.magnitude;
		return Mathf.Clamp01(rhs.magnitude / magnitude * Mathf.Sign(Vector3.Dot(vector, rhs)));
	}

	public void BeginMoveRight(int dir)
	{
		rightDir = dir;
	}

	public void EndMoveRight()
	{
		rightDir = 0;
	}

	public void BeginMoveUp(int dir)
	{
		upDir = dir;
	}

	public void EndMoveUp()
	{
		upDir = 0;
	}

	public void BeginMoveFwd(int dir)
	{
		fwdDir = dir;
	}

	public void EndMoveFwd()
	{
		fwdDir = 0;
	}

	public void SavePersistentData()
	{
		if (PilotSaveManager.current != null)
		{
			PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).joystickPosition = jPos;
		}
	}
}
