using UnityEngine;

public class MPOnlyScenarioEditorObj : MonoBehaviour
{
	private void OnEnable()
	{
		if (VTScenario.current != null && !VTScenario.current.multiplayer)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
