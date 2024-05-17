using System.Collections.Generic;
using UnityEngine;

public class ControllerEventHandler : MonoBehaviour
{
	public delegate void HoverEvent(VRHandController controller, VRInteractable interactable);

	private static bool _eventsPaused;

	private List<VRHandController> controllers = new List<VRHandController>();

	private List<VRInteractable> interactables = new List<VRInteractable>();

	public static ControllerEventHandler fetch { get; private set; }

	public static bool eventsPaused => _eventsPaused;

	public event HoverEvent OnHover;

	public event HoverEvent OnUnHover;

	public static void RegisterController(VRHandController c)
	{
		if (!fetch)
		{
			Debug.LogWarning("Tried to register a controller to event handler but ControllerEventHandler instance doesn't exist.");
		}
		else
		{
			fetch._RegisterController(c);
		}
	}

	public void _RegisterController(VRHandController c)
	{
		controllers.RemoveAll((VRHandController x) => x == null);
		controllers.Add(c);
	}

	public static void UnregisterController(VRHandController c)
	{
		if ((bool)fetch)
		{
			fetch._UnregisterController(c);
		}
	}

	public void _UnregisterController(VRHandController c)
	{
		controllers.RemoveAll((VRHandController x) => x == null || x == c);
	}

	public static void RegisterInteractable(VRInteractable i)
	{
		if (!fetch)
		{
			Debug.LogError("Tried to register an interactable to event handler but ControllerEventHandler instance doesn't exist.");
		}
		else
		{
			fetch._RegisterInteractable(i);
		}
	}

	public static void UnegisterInteractable(VRInteractable i)
	{
		if ((bool)fetch)
		{
			fetch._UnregisterInteractable(i);
		}
	}

	public static void PauseEvents()
	{
		Debug.Log("Pausing Controller Events");
		_eventsPaused = true;
		foreach (VRHandController controller in VRHandController.controllers)
		{
			if ((bool)controller.activeInteractable)
			{
				controller.ReleaseFromInteractable();
			}
		}
	}

	public static void UnpauseEvents()
	{
		Debug.Log("Unpausing Controller Events");
		_eventsPaused = false;
	}

	private void _RegisterInteractable(VRInteractable i)
	{
		interactables.Add(i);
	}

	private void _UnregisterInteractable(VRInteractable i)
	{
		interactables.Remove(i);
	}

	private void Awake()
	{
		if ((bool)fetch)
		{
			Object.Destroy(fetch);
		}
		fetch = this;
	}

	private void Update()
	{
		if (_eventsPaused)
		{
			return;
		}
		int count = controllers.Count;
		int count2 = interactables.Count;
		for (int i = 0; i < count; i++)
		{
			VRHandController vRHandController = controllers[i];
			if ((bool)vRHandController.activeInteractable)
			{
				if (!vRHandController.activeInteractable.enabled || !vRHandController.activeInteractable.gameObject.activeInHierarchy)
				{
					vRHandController.ReleaseFromInteractable();
				}
				continue;
			}
			float num = float.MaxValue;
			VRInteractable vRInteractable = null;
			for (int j = 0; j < count2; j++)
			{
				VRInteractable vRInteractable2 = interactables[j];
				if (!vRInteractable2.enabled || !vRInteractable2.gameObject.activeInHierarchy || ((bool)vRInteractable2.nearbyController && vRInteractable2.nearbyController != vRHandController) || ((bool)vRInteractable2.poseBounds && !vRInteractable2.poseBounds.controllerInBounds))
				{
					continue;
				}
				if (vRInteractable2.useRect)
				{
					Vector3 vector = vRInteractable2.transform.InverseTransformPoint(vRHandController.interactionTransform.position);
					if (vRInteractable2.rect.Contains(vector))
					{
						float num2 = VectorUtils.MinAxialDistance(vector, vRInteractable2.rect.center);
						float num3 = num2 * num2;
						if (num3 < num)
						{
							num = num3;
							vRInteractable = vRInteractable2;
						}
					}
				}
				else
				{
					float sqrMagnitude = (vRInteractable2.transform.position - vRHandController.interactionTransform.position).sqrMagnitude;
					if (sqrMagnitude < vRInteractable2.sqrRadius && sqrMagnitude < num)
					{
						num = sqrMagnitude;
						vRInteractable = vRInteractable2;
					}
				}
			}
			if (vRInteractable == null)
			{
				vRInteractable = vRHandController.overrideHoverInteractable;
			}
			if (vRInteractable != null)
			{
				if (vRHandController.hoverInteractable != vRInteractable)
				{
					if ((bool)vRHandController.hoverInteractable)
					{
						UnHoverController(vRHandController);
					}
					vRHandController.hoverInteractable = vRInteractable;
					vRInteractable.Hover(vRHandController);
					if (this.OnHover != null)
					{
						this.OnHover(vRHandController, vRInteractable);
					}
				}
			}
			else if ((bool)vRHandController.hoverInteractable)
			{
				UnHoverController(vRHandController);
			}
		}
	}

	private void UnHoverController(VRHandController c)
	{
		VRInteractable hoverInteractable = c.hoverInteractable;
		c.hoverInteractable = null;
		hoverInteractable.UnHover();
		if (this.OnUnHover != null)
		{
			this.OnUnHover(c, hoverInteractable);
		}
	}
}
