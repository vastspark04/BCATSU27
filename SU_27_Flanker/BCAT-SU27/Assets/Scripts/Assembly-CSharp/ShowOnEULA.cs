using UnityEngine;

public class ShowOnEULA : MonoBehaviour
{
	public GameObject[] objects;

	private void Start()
	{
		if (GameSettings.TryGetGameSettingValue<int>("EULA_AGREED", out var val) && val > 0)
		{
			objects.SetActive(active: true);
		}
		else
		{
			objects.SetActive(active: false);
		}
	}

	public void Agree()
	{
		objects.SetActive(active: true);
	}
}
