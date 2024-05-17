using System;
using UnityEngine;

public class MFDVerticalPositionAdjustment : MonoBehaviour
{
	[Serializable]
	public class ObjectConfig
	{
		public Transform transform;

		public MFD.MFDButtons button;

		public bool x;

		public bool y = true;
	}

	public MFD sampleMfd;

	public MFDPage page;

	public ObjectConfig[] transforms;

	private Vector2[] offsets;

	private void Awake()
	{
		page.OnActivatePage.AddListener(OnActivate);
		offsets = new Vector2[transforms.Length];
		for (int i = 0; i < offsets.Length; i++)
		{
			Transform transform = sampleMfd.buttons[(int)transforms[i].button].transform;
			Vector3 vector = transforms[i].transform.parent.InverseTransformPoint(transform.position);
			offsets[i] = transforms[i].transform.localPosition - vector;
		}
	}

	private void OnActivate()
	{
		for (int i = 0; i < transforms.Length; i++)
		{
			ObjectConfig obj = transforms[i];
			Transform transform = page.mfd.buttons[(int)transforms[i].button].transform;
			Vector3 vector = obj.transform.parent.InverseTransformPoint(transform.position);
			Vector3 localPosition = obj.transform.localPosition;
			if (obj.x)
			{
				localPosition.x = vector.x + offsets[i].x;
			}
			if (obj.y)
			{
				localPosition.y = vector.y + offsets[i].y;
			}
			obj.transform.localPosition = localPosition;
		}
	}
}
