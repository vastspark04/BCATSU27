using UnityEngine;

public class VTScenEdBullseyeIcon : MonoBehaviour
{
	public VTScenarioEditor editor;

	public GameObject spriteObj;

	private void Update()
	{
		if (editor.currentScenario != null && editor.currentScenario.waypoints != null && editor.currentScenario.waypoints.bullseye != null)
		{
			spriteObj.SetActive(value: true);
			Vector3 position = editor.currentScenario.waypoints.bullseyeTransform.position;
			position.y = WaterPhysics.instance.height;
			base.transform.position = position;
		}
		else
		{
			spriteObj.SetActive(value: false);
		}
	}
}
