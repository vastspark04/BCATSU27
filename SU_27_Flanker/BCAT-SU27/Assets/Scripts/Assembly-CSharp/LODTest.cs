using UnityEngine;

public class LODTest : MonoBehaviour
{
	public Transform target;

	private Vector3 vectorFromTarget;

	private void Start()
	{
		vectorFromTarget = base.transform.position - target.position;
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.UpArrow))
		{
			vectorFromTarget *= 0.99f;
			base.transform.position = target.position + vectorFromTarget;
		}
		if (Input.GetKey(KeyCode.DownArrow))
		{
			vectorFromTarget *= 1.01f;
			base.transform.position = target.position + vectorFromTarget;
		}
	}

	private void OnGUI()
	{
		string text = "Distance: " + vectorFromTarget.magnitude;
		GUI.Label(new Rect(10f, 10f, 500f, 500f), text);
	}
}
