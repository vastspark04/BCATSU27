using UnityEngine;

public class ClearAIPilotDeathEvents : MonoBehaviour
{
	[ContextMenu("Apply")]
	private void Apply()
	{
		AIPilot[] array = Object.FindObjectsOfType<AIPilot>();
		for (int i = 0; i < array.Length; i++)
		{
			_ = (bool)array[i].GetComponent<Health>();
		}
	}
}
