using UnityEngine;

public class MicNoFocusIndicator : MonoBehaviour
{
	public GameObject displayObj;

	private bool micEnabled;

	public void SetMicEnabled(int st)
	{
		micEnabled = st > 0;
		if (!micEnabled)
		{
			displayObj.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (micEnabled)
		{
			displayObj.SetActive(!Application.isFocused);
		}
	}
}
