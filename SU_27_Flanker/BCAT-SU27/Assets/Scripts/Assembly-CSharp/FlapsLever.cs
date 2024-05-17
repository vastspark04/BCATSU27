using UnityEngine;

public class FlapsLever : MonoBehaviour
{
	public AeroController aeroController;

	public VRLever vrLever;

	private float stateMult;

	public FlightControlComponent[] outputs;

	private void Start()
	{
		if (!aeroController)
		{
			Debug.LogWarning("Flaps lever has no assigned aero controller.");
			base.enabled = false;
			return;
		}
		int states = vrLever.states;
		stateMult = 1f / ((float)states - 1f);
		vrLever.OnSetState.AddListener(SetState);
		SetState(vrLever.initialState);
	}

	public void SetState(int st)
	{
		float flaps = (float)st * stateMult;
		aeroController.flaps = flaps;
		if (outputs != null)
		{
			for (int i = 0; i < outputs.Length; i++)
			{
				outputs[i].SetFlaps(flaps);
			}
		}
	}
}
