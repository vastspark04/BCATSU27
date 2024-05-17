using UnityEngine;
using VTOLVR.Multiplayer;

public class MPOnlyObject : MonoBehaviour
{
	private void Awake()
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			Object.Destroy(base.gameObject);
		}
	}
}
