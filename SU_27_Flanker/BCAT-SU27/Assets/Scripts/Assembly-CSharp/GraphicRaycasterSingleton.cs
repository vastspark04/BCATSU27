using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GraphicRaycaster))]
public class GraphicRaycasterSingleton : MonoBehaviour
{
	private static GraphicRaycaster _raycaster;

	public static GraphicRaycaster fetch => _raycaster;

	private void Awake()
	{
		_raycaster = GetComponent<GraphicRaycaster>();
	}
}
