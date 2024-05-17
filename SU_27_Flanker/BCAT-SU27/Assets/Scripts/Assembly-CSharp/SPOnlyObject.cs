using UnityEngine;
using VTOLVR.Multiplayer;

public class SPOnlyObject : MonoBehaviour
{
	private void Start()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			Object.Destroy(base.gameObject);
		}
	}
}
