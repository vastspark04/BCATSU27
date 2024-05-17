using UnityEngine;
using UnityEngine.UI;

public class HUD360Horizon : MonoBehaviour
{
	public Image imageTemplate;

	public int vertices = 32;

	public Text headingTemplate;

	public float headingHeight;

	private void Start()
	{
		CollimatedHUDUI componentInParent = GetComponentInParent<CollimatedHUDUI>();
		float num = 360f / (float)vertices;
		Vector3 vector = new Vector3(0f, 0f, componentInParent.depth);
		for (int i = 0; i < vertices; i++)
		{
			Vector3 vector2 = Quaternion.AngleAxis((float)i * num, Vector3.up) * vector;
			Vector3 vector3 = Quaternion.AngleAxis((float)(i + 1) * num, Vector3.up) * vector;
			Vector3 vector4 = vector3 - vector2;
			Vector3 forward = Vector3.Cross(vector4, Vector3.up);
			GameObject obj = Object.Instantiate(imageTemplate.gameObject, imageTemplate.transform.parent);
			obj.transform.localPosition = vector2;
			obj.transform.localRotation = Quaternion.LookRotation(forward, vector4);
			float magnitude = (vector3 - vector2).magnitude;
			obj.transform.localScale = new Vector3(1f, magnitude, 1f);
		}
		float num2 = 10f;
		for (int j = 0; j < 36; j++)
		{
			Vector3 vector5 = Quaternion.AngleAxis((float)j * num2, Vector3.up) * vector;
			vector5 += new Vector3(0f, headingHeight, 0f);
			Text component = Object.Instantiate(headingTemplate.gameObject, headingTemplate.transform.parent).GetComponent<Text>();
			component.text = j.ToString();
			component.transform.localPosition = vector5;
			component.transform.localRotation = Quaternion.LookRotation(vector5);
			component.transform.localScale = headingTemplate.transform.localScale;
		}
		headingTemplate.gameObject.SetActive(value: false);
		imageTemplate.gameObject.SetActive(value: false);
	}

	private void LateUpdate()
	{
		if ((bool)VRHead.instance)
		{
			base.transform.position = VRHead.instance.transform.position;
			base.transform.rotation = Quaternion.identity;
		}
	}
}
