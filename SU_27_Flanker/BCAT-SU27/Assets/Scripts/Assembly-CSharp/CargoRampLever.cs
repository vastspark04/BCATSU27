using UnityEngine;

public class CargoRampLever : MonoBehaviour
{
	private VRInteractable interactable;

	public Transform switchModelTransform;

	public PassengerBay bay;

	private PassengerBay.RampStates state;

	private AudioSource audioSource;

	private void Start()
	{
		interactable = GetComponent<VRInteractable>();
		interactable.OnStartInteraction += ToggleButton;
		audioSource = GetComponent<AudioSource>();
	}

	private void ToggleButton(VRHandController controller)
	{
		if (state != PassengerBay.RampStates.Opening && state != PassengerBay.RampStates.Closing)
		{
			interactable.requiredMotion = -interactable.requiredMotion;
			audioSource.Play();
			bay.ToggleRamp();
		}
	}

	private void Update()
	{
		state = bay.rampState;
		switch (state)
		{
		case PassengerBay.RampStates.Open:
			switchModelTransform.localEulerAngles = new Vector3(-40f, 0f, 0f);
			break;
		case PassengerBay.RampStates.Closed:
			switchModelTransform.localEulerAngles = new Vector3(40f, 0f, 0f);
			break;
		default:
		{
			Quaternion to = Quaternion.Euler(40 * ((state == PassengerBay.RampStates.Closing) ? 1 : (-1)), 0f, 0f);
			switchModelTransform.localRotation = Quaternion.RotateTowards(switchModelTransform.localRotation, to, 240f * Time.deltaTime);
			break;
		}
		}
	}
}
