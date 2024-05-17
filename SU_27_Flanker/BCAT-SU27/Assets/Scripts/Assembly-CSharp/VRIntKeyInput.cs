using UnityEngine;

public class VRIntKeyInput : MonoBehaviour
{
	public bool testFeatureOnly;

	public string testFeatureOptionName;

	public KeyCode key;

	public bool useModifier;

	public KeyCode modifier = KeyCode.RightShift;

	private VRInteractable vrint;

	private void Awake()
	{
		if (testFeatureOnly && !Application.isEditor && (!GameSettings.TryGetGameSettingValue<bool>(testFeatureOptionName, out var val) || !val))
		{
			Object.Destroy(this);
		}
		else
		{
			vrint = GetComponent<VRInteractable>();
		}
	}

	private void Update()
	{
		if ((!useModifier || Input.GetKey(modifier)) && Input.GetKeyDown(key))
		{
			vrint.StartInteraction();
			vrint.StopInteraction();
		}
	}
}
