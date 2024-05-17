using UnityEngine;

public class EngineWarningIndicator : MonoBehaviour
{
	public float blinkRate = 3f;

	public float blinkDutyCycle = 0.75f;

	public UIImageToggle imgToggle;

	public ModuleEngine engine;

	public VRLever engineSwitch;

	public Battery battery;

	public bool useCommonWarning;

	public FlightWarnings.CommonWarnings cw;

	private FlightWarnings fw;

	private void Awake()
	{
		fw = GetComponentInParent<FlightWarnings>();
	}

	private void Update()
	{
		if (useCommonWarning)
		{
			imgToggle.imageEnabled = fw.IsCommonWarningThrown(cw) && battery.Drain(0.001f * Time.deltaTime) && Mathf.Repeat(blinkRate * Time.time, 1f) < blinkDutyCycle;
		}
		else if (battery.Drain(0.001f * Time.deltaTime) && engine.finalThrust < 0.1f && engineSwitch.currentState != 0)
		{
			imgToggle.imageEnabled = Mathf.Repeat(blinkRate * Time.time, 1f) < blinkDutyCycle;
		}
		else
		{
			imgToggle.imageEnabled = false;
		}
	}
}
