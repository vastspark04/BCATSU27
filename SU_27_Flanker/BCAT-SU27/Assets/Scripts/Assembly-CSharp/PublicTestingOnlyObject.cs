using UnityEngine;

public class PublicTestingOnlyObject : MonoBehaviour
{
	public bool disableOnly;

	private void Awake()
	{
		if (GameStartup.version.releaseType != GameVersion.ReleaseTypes.Testing)
		{
			if (disableOnly)
			{
				base.gameObject.SetActive(value: false);
			}
			else
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}
