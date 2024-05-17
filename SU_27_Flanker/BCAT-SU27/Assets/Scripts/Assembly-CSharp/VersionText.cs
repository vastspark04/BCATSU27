using UnityEngine;
using UnityEngine.UI;

public class VersionText : MonoBehaviour
{
	public Text text;

	private void Start()
	{
		text.text = "v" + GameStartup.version.ToString();
	}
}
