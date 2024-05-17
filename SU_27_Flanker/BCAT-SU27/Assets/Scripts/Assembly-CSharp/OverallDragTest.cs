using UnityEngine;

public class OverallDragTest : MonoBehaviour
{
	public Vector2 uiPos;

	private ModuleEngine[] engines;

	private Rigidbody rb;

	private FlightInfo fInfo;

	private float dragForce;

	private void OnEnable()
	{
		rb = GetComponent<Rigidbody>();
		engines = GetComponentsInChildren<ModuleEngine>();
		fInfo = GetComponent<FlightInfo>();
	}

	private void Update()
	{
		Vector3 zero = Vector3.zero;
		ModuleEngine[] array = engines;
		foreach (ModuleEngine moduleEngine in array)
		{
			zero += moduleEngine.finalThrust * -moduleEngine.thrustTransform.forward;
		}
		float num = Vector3.Dot(zero, rb.velocity.normalized) / rb.mass;
		float num2 = Vector3.Dot(fInfo.acceleration, rb.velocity.normalized);
		dragForce = (num - num2) * rb.mass;
	}

	private void OnGUI()
	{
		Rect position = new Rect(uiPos.x, uiPos.y, 1000f, 1000f);
		string text = $"{base.gameObject.name}:\nDrag: {dragForce} kN\nSpeed: {fInfo.airspeed}";
		GUI.Label(position, text);
	}
}
