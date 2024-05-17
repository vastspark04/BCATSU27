using UnityEngine;

public class SweepDragTest : MonoBehaviour
{
	private void Start()
	{
		float altitude = 3000f;
		for (int i = 0; (float)i <= 90f; i += 15)
		{
			DataGraph dataGraph = DataGraph.CreateGraph($"Sweep: {i}", Vector3.zero);
			dataGraph.AddValue(new Vector2(-0.01f, 3f));
			for (float num = 0f; num < 3f; num += 0.2f)
			{
				float y = AerodynamicsController.fetch.DragMultiplierAtSpeedAndSweep(num * 343f, altitude, i);
				dataGraph.AddValue(new Vector2(num, y));
			}
		}
	}
}
