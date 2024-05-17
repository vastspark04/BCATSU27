using UnityEngine;

public class OculusOnlyObject : MonoBehaviour
{
	private void Awake()
	{
		if (!GameSettings.VR_SDK_IS_OCULUS)
		{
			Object.DestroyImmediate(base.gameObject);
		}
	}
}
