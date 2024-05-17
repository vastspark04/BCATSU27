using UnityEngine;

public class AudioUpdateModeSetter : MonoBehaviour
{
	public AudioVelocityUpdateMode mode;

	public bool setToAllChildren;

	private void Awake()
	{
		if (setToAllChildren)
		{
			AudioSource[] componentsInChildren = GetComponentsInChildren<AudioSource>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].velocityUpdateMode = mode;
			}
		}
		else
		{
			GetComponent<AudioSource>().velocityUpdateMode = mode;
		}
	}
}
