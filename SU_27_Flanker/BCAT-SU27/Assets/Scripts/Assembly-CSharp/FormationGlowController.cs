using System;
using UnityEngine;

public class FormationGlowController : MonoBehaviour
{
	public EmissiveTextureLight[] lights;

	private int state;

	public int currentState => state;

	public event Action<int> OnSetState;

	public void SetStatus(int st)
	{
		if (state != st)
		{
			state = st;
			for (int i = 0; i < lights.Length; i++)
			{
				lights[i].SetStatus(st);
			}
			this.OnSetState?.Invoke(st);
		}
	}
}
