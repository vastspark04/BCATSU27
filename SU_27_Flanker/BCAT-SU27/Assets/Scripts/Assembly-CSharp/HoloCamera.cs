using UnityEngine;
using UnityEngine.UI;

public class HoloCamera : MonoBehaviour
{
	public Actor target;

	public Transform renderTexTransform;

	public float distRadius = 150f;

	public float clipRadius = 50f;

	public float scanSpeed = 100f;

	public Text rangeText;

	public Text nameText;

	private Camera cam;

	private float distOffset;

	private void Start()
	{
		cam = GetComponent<Camera>();
	}

	private void Update()
	{
		if ((bool)target)
		{
			renderTexTransform.gameObject.SetActive(value: true);
			Vector3 vector = target.transform.TransformPoint(target.iconOffset);
			float num = Vector3.Distance(vector, base.transform.position);
			float num2 = num + distOffset;
			if (distOffset > distRadius)
			{
				distOffset = 0f - distRadius;
			}
			else
			{
				distOffset += scanSpeed * Time.deltaTime;
			}
			cam.enabled = true;
			base.transform.LookAt(vector);
			cam.nearClipPlane = num2 - clipRadius;
			cam.farClipPlane = num2 + clipRadius;
			cam.fieldOfView = 0.65f;
			rangeText.text = num.ToString("0") + "m";
			nameText.text = target.actorName;
		}
		else
		{
			renderTexTransform.gameObject.SetActive(value: false);
			cam.enabled = false;
		}
	}
}
