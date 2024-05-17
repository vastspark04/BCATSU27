using UnityEngine;

public class DemoOnlyObject : MonoBehaviour
{
	private void Start()
	{
		Object.Destroy(base.gameObject);
	}
}
