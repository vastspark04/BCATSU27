using UnityEngine;

public class LB_TrainingPilot : MonoBehaviour
{
	public float initialSpeed = 300f;

	public float maxG = 9f;

	public float maxAoA = 20f;

	public float rollRate = 180f;

	public float mass = 7f;

	public float thrust = 100f;

	public float liftFactor = 0.0015f;

	public float dragFactor = 0.002f;

	public float minTargetSpeed = 100f;

	public float maxTargetSpeed = 450f;

	public Vector3[] directions;

	private string debugString;

	public void UpdateModel(LB_SimPilot pilot)
	{
		base.transform.position = pilot.position;
		base.transform.rotation = pilot.rotation;
		debugString = pilot.GetDebugString();
	}

	private void OnGUI()
	{
		if ((bool)Camera.current)
		{
			BDGUIUtils.DrawTextAtWorldPoint(debugString, Camera.current, base.transform.position, 12, Color.white);
		}
	}
}
