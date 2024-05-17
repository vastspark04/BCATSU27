using UnityEngine;

public class LGUPlus_ObjectSwitch : MonoBehaviour
{
	public enum EnabledModes
	{
		LGUPlus_Only,
		Standard_Only
	}

	public EnabledModes mode;

	private void Awake()
	{
		base.gameObject.SetActive(mode == EnabledModes.Standard_Only);
	}
}
