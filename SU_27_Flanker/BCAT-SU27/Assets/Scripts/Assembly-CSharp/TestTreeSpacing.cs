using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestTreeSpacing : MonoBehaviour
{
	public GameObject textTemplate;

	public float distScale;

	public int count;

	private List<GameObject> objs = new List<GameObject>();

	private void Awake()
	{
		textTemplate.SetActive(value: false);
	}

	[ContextMenu("Test")]
	public void Generate()
	{
		foreach (GameObject obj in objs)
		{
			Object.Destroy(obj);
		}
		objs.Clear();
		int num = Mathf.FloorToInt(Mathf.Sqrt((float)count * 2f));
		int num2 = num;
		Vector3 zero = Vector3.zero;
		Vector3 b = new Vector3(distScale, 0f, 0f);
		Vector3 b2 = new Vector3(0f, distScale, 0f);
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < count; i++)
		{
			GameObject gameObject = Object.Instantiate(textTemplate, textTemplate.transform.parent);
			Vector3 vector = Vector3.Lerp(zero, b, 0.25f / (float)num + (float)num3 / (float)num);
			Vector3 vector2 = Vector3.Lerp(zero, b2, 0.25f / (float)num + (float)num4 / (float)num);
			Vector3 localPosition = vector + vector2;
			gameObject.transform.localPosition = localPosition;
			objs.Add(gameObject);
			gameObject.SetActive(value: true);
			gameObject.GetComponent<Text>().text = i.ToString();
			num3++;
			if (num3 >= num2)
			{
				num4++;
				num3 = 0;
				num2--;
			}
		}
	}
}
