using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class WheelForceDebugText : MonoBehaviour
{
	public RaySpringDamper susp;

	private Text txt;

	private void Start()
	{
		txt = GetComponent<Text>();
	}

	private void Update()
	{
		txt.text = susp.currentWheelForce.magnitude.ToString("0.0");
	}
}
