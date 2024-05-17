using UnityEngine;

public class AdaptivePhysicsTimestep : MonoBehaviour
{
	public float minTimestep = 0.011111f;

	public float maxTimestep = 0.04f;

	public float adjustmentRate = 0.01f;

	private float currFrameRate;

	public bool debug;

	private string debugString = string.Empty;

	private void LateUpdate()
	{
		if (Time.deltaTime > 0f)
		{
			currFrameRate = 1f / Time.deltaTime;
			float fixedDeltaTime = Time.fixedDeltaTime;
			fixedDeltaTime = (Time.fixedDeltaTime = ((!(currFrameRate < 80f)) ? Mathf.MoveTowards(fixedDeltaTime, minTimestep, adjustmentRate * Time.deltaTime) : Mathf.MoveTowards(fixedDeltaTime, maxTimestep, adjustmentRate * Time.deltaTime)));
			if (debug)
			{
				debugString = $"FPS: {currFrameRate}, Timestep: {fixedDeltaTime}";
			}
		}
	}

	private void OnGUI()
	{
		GUI.Label(new Rect(10f, 10f, 100f, 100f), debugString);
	}
}
