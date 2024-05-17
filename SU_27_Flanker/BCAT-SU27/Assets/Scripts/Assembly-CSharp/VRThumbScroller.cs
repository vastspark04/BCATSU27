using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class VRThumbScroller : MonoBehaviour
{
	public float scrollRate = 500f;

	public bool xAxis;

	public bool yAxis = true;

	private ScrollRect _s;

	private List<VRHandController> controllers = new List<VRHandController>();

	private ScrollRect scrollRect
	{
		get
		{
			if (!_s)
			{
				_s = GetComponent<ScrollRect>();
				if (!_s)
				{
					Debug.LogError("VRThumbScroller does not have a ScrollRect component!", base.gameObject);
				}
			}
			return _s;
		}
	}

	private void OnEnable()
	{
		StartCoroutine(EnableRoutine());
	}

	private IEnumerator EnableRoutine()
	{
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		while (VRHandController.controllers == null || VRHandController.controllers.Count < 1)
		{
			yield return null;
		}
		int i = 0;
		while (true)
		{
			VRHandController vRHandController = VRHandController.controllers[i];
			if ((bool)vRHandController)
			{
				vRHandController.OnStickAxis += C_OnStickAxis;
				controllers.Add(vRHandController);
			}
			while (VRHandController.controllers.Count < i + 2)
			{
				yield return null;
				if (VRHandController.controllers == null)
				{
					yield break;
				}
			}
			i++;
		}
	}

	private void OnDisable()
	{
		foreach (VRHandController controller in controllers)
		{
			if ((bool)controller)
			{
				controller.OnStickAxis -= C_OnStickAxis;
			}
		}
		controllers.Clear();
	}

	private void C_OnStickAxis(VRHandController ctrlr, Vector2 vector)
	{
		Vector3 localPosition = scrollRect.content.localPosition;
		if (xAxis)
		{
			localPosition += new Vector3((0f - scrollRate) * vector.x * Time.deltaTime, 0f, 0f);
		}
		if (yAxis)
		{
			localPosition += new Vector3(0f, (0f - scrollRate) * vector.y * Time.deltaTime, 0f);
		}
		scrollRect.content.localPosition = localPosition;
	}
}
