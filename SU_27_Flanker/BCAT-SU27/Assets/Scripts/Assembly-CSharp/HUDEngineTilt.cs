using UnityEngine;
using UnityEngine.UI;

public class HUDEngineTilt : MonoBehaviour
{
	public Text angleText;

	public Transform arrow;

	public EngineEffects engineEffect;

    public TiltController tiltController;

	private void Update()
	{
		float currentTilt = engineEffect.currentTilt;
		angleText.text = Mathf.Round(90f - currentTilt).ToString();
		arrow.localRotation = Quaternion.Euler(0f, 0f, 0f - currentTilt);
	}
}
