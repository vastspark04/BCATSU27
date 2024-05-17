using System.Collections.Generic;
using UnityEngine;

public class PoseBounds : MonoBehaviour
{
	public Vector3 size;

	public GloveAnimation.Poses pose = GloveAnimation.Poses.Point;

	private Bounds bounds;

	private List<GloveAnimation> glovesIn = new List<GloveAnimation>();

	public bool controllerInBounds { get; private set; }

	private void OnDrawGizmos()
	{
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, base.transform.lossyScale);
		Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
		Gizmos.DrawCube(Vector3.zero, size);
		Gizmos.matrix = Matrix4x4.identity;
	}

	private void Start()
	{
		bounds = new Bounds(Vector3.zero, size);
	}

	private void LateUpdate()
	{
		int count = VRHandController.controllers.Count;
		controllerInBounds = false;
		for (int i = 0; i < count; i++)
		{
			VRHandController vRHandController = VRHandController.controllers[i];
			if (!vRHandController.gloveAnimation || (bool)vRHandController.activeInteractable)
			{
				continue;
			}
			Vector3 point = base.transform.InverseTransformPoint(vRHandController.interactionTransform.position);
			if (bounds.Contains(point))
			{
				if (!glovesIn.Contains(vRHandController.gloveAnimation))
				{
					glovesIn.Add(vRHandController.gloveAnimation);
					vRHandController.gloveAnimation.SetBoundsPose(this);
				}
				controllerInBounds = true;
			}
			else if (glovesIn.Contains(vRHandController.gloveAnimation))
			{
				vRHandController.gloveAnimation.ClearBoundsPose(this);
				glovesIn.Remove(vRHandController.gloveAnimation);
			}
		}
	}

	private void OnDisable()
	{
		controllerInBounds = false;
	}

	public void ClearGlove(GloveAnimation ga)
	{
		glovesIn.Remove(ga);
	}
}
