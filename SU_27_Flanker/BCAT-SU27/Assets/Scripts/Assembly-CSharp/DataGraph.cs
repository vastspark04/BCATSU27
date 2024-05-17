using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class DataGraph : MonoBehaviour
{
	public RectTransform graphTf;

	public UILineRenderer graphLine;

	public GameObject pointButtonTemplate;

	public Text xValText;

	public Text yValText;

	public Text titleText;

	public float minWidth;

	public float minHeight;

	private List<Vector2> values = new List<Vector2>();

	private List<GameObject> buttonObjects = new List<GameObject>();

	private Vector3 lastMousePos;

	private Vector3 dragOffset;

	private static Canvas dataGraphCanvas;

	private void Awake()
	{
		pointButtonTemplate.SetActive(value: false);
		graphLine.Points = new Vector2[0];
		xValText.text = string.Empty;
		yValText.text = string.Empty;
	}

	public void SetTitle(string title)
	{
		titleText.text = title;
	}

	public void AddValue(Vector2 value)
	{
		if (values.Count > 0)
		{
			bool flag = false;
			for (int i = 0; i < values.Count; i++)
			{
				if (flag)
				{
					break;
				}
				if (value.x < values[i].x)
				{
					values.Insert(i, value);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				values.Add(value);
			}
			UpdateGraph();
		}
		else
		{
			values.Add(value);
		}
	}

	private void UpdateGraph()
	{
		float num = float.MinValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MaxValue;
		foreach (Vector2 value in values)
		{
			if (value.x < num2)
			{
				num2 = value.x;
			}
			if (value.x > num)
			{
				num = value.x;
			}
			if (value.y < num4)
			{
				num4 = value.y;
			}
			if (value.y > num3)
			{
				num3 = value.y;
			}
		}
		float height = graphTf.rect.height;
		float width = graphTf.rect.width;
		float num5 = num - num2;
		float num6 = num3 - num4;
		if (num5 == 0f)
		{
			num5 = 1f;
		}
		if (num6 == 0f)
		{
			num6 = 1f;
		}
		foreach (GameObject buttonObject in buttonObjects)
		{
			Object.Destroy(buttonObject);
		}
		buttonObjects.Clear();
		Vector2[] array = new Vector2[values.Count];
		for (int i = 0; i < values.Count; i++)
		{
			Vector2 val = values[i];
			Vector2 vector = new Vector2((val.x - num2) / num5, (val.y - num4) / num6);
			vector.Scale(new Vector2(width, height));
			array[i] = vector;
			GameObject gameObject = Object.Instantiate(pointButtonTemplate, graphLine.transform.parent);
			gameObject.transform.localPosition = vector;
			gameObject.transform.localScale = pointButtonTemplate.transform.localScale;
			gameObject.transform.localRotation = pointButtonTemplate.transform.localRotation;
			gameObject.SetActive(value: true);
			DataGraphButton component = gameObject.GetComponent<DataGraphButton>();
			component.val = val;
			component.graph = this;
			buttonObjects.Add(gameObject);
		}
		graphLine.Points = array;
	}

	public void DisplayValue(Vector2 value)
	{
		xValText.text = $"X: {value.x:N1}";
		yValText.text = $"Y: {value.y:N1}";
	}

	public void OnResizeButtonDown()
	{
		lastMousePos = Input.mousePosition;
	}

	public void OnResizeButtonHeld()
	{
		RectTransform obj = (RectTransform)base.transform;
		Vector3 vector = Input.mousePosition - lastMousePos;
		lastMousePos = Input.mousePosition;
		vector = base.transform.InverseTransformVector(vector);
		float a = obj.rect.width + vector.x;
		a = Mathf.Max(a, minWidth);
		float a2 = obj.rect.height - vector.y;
		a2 = Mathf.Max(a2, minHeight);
		obj.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, a);
		obj.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, a2);
		UpdateGraph();
	}

	public void OnDragButtonDown()
	{
		Vector3 mousePosition = Input.mousePosition;
		dragOffset = base.transform.position - mousePosition;
	}

	public void OnDragButtonHeld()
	{
		Vector3 mousePosition = Input.mousePosition;
		base.transform.position = mousePosition + dragOffset;
	}

	public void ClearGraph()
	{
		graphLine.Points = new Vector2[0];
		foreach (GameObject buttonObject in buttonObjects)
		{
			Object.Destroy(buttonObject);
		}
		buttonObjects.Clear();
		values.Clear();
	}

	public static DataGraph CreateGraph(string graphTitle, Vector3 graphPosition)
	{
		if (!dataGraphCanvas)
		{
			dataGraphCanvas = new GameObject("DataGraphCanvas").AddComponent<Canvas>();
			dataGraphCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
			dataGraphCanvas.gameObject.AddComponent<GraphicRaycaster>();
			if (!EventSystem.current)
			{
				new GameObject("EventSystem").AddComponent<EventSystem>().gameObject.AddComponent<StandaloneInputModule>();
			}
		}
		GameObject obj = (GameObject)Object.Instantiate(Resources.Load("DataGraph/DataGraph"));
		obj.name = graphTitle;
		DataGraph component = obj.GetComponent<DataGraph>();
		obj.transform.SetParent(dataGraphCanvas.transform);
		component.SetTitle(graphTitle);
		component.transform.localPosition = graphPosition;
		return component;
	}
}
