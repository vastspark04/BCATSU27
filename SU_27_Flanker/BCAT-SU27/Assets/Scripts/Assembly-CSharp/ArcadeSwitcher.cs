using UnityEngine;

public class ArcadeSwitcher : MonoBehaviour
{
	public GameObject[] standardObjects;

	public GameObject[] arcadeObjects;

	private void Awake()
	{
		bool flag = false;
		standardObjects.SetActive(!flag);
		arcadeObjects.SetActive(flag);
	}
}
