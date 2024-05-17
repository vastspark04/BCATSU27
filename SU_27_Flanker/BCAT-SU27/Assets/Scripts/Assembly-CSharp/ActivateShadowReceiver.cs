using UnityEngine;

public class ActivateShadowReceiver : MonoBehaviour
{
	[ContextMenu("Apply")]
	public void Apply()
	{
		GetComponent<Renderer>().receiveShadows = true;
	}
}
