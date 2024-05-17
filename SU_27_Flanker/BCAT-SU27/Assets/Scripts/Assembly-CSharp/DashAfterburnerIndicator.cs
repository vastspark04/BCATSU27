using UnityEngine;

public class DashAfterburnerIndicator : MonoBehaviour
{
	public ModuleEngine engine;

	private UIImageToggle imgToggle;

	private void Start()
	{
		imgToggle = GetComponent<UIImageToggle>();
	}

	private void Update()
	{
		if (engine.progressiveAB)
		{
			imgToggle.enabled = false;
			Color color = imgToggle.image.color;
			if (engine.abMult > 0.005f)
			{
				color.a = 0.25f + 3f * engine.abMult / 4f;
			}
			else
			{
				color.a = 0f;
			}
			imgToggle.image.color = color;
		}
		else
		{
			imgToggle.imageEnabled = engine.afterburner;
		}
	}
}
