using MFlight.Demo;
using UnityEngine;

namespace OC.ExampleContent{

public class Airplane : MonoBehaviour
{
	[SerializeField]
	private MFlight.Demo.Plane m_Plane;

	[Header("Transforms")]
	[SerializeField]
	private Transform m_Propeller;

	[SerializeField]
	private Transform m_FlapL;

	[SerializeField]
	private Transform m_FlapR;

	[SerializeField]
	private Transform m_FlapRearL;

	[SerializeField]
	private Transform m_FlapRearR;

	[SerializeField]
	private Transform m_Rudder;

	[Header("Audio")]
	[SerializeField]
	private AudioSource m_PropellerSound;

	private void Start()
	{
		m_PropellerSound.volume = 0f;
	}

	private void Update()
	{
		m_Propeller.Rotate(Vector3.right, 720f * Time.deltaTime, Space.Self);
		float num = Mathf.Clamp(0f - m_Plane.Pitch - m_Plane.Roll, -1f, 1f);
		float num2 = Mathf.Clamp(0f - m_Plane.Pitch + m_Plane.Roll, -1f, 1f);
		m_FlapL.localRotation = Quaternion.Lerp(m_FlapL.localRotation, Quaternion.Euler(0f, num * 50f, 0f), Time.deltaTime * 4f);
		m_FlapR.localRotation = Quaternion.Lerp(m_FlapR.localRotation, Quaternion.Euler(0f, num2 * 50f, 0f), Time.deltaTime * 4f);
		m_FlapRearL.localRotation = Quaternion.Lerp(m_FlapRearL.localRotation, Quaternion.Euler(0f, num * 50f, 0f), Time.deltaTime * 4f);
		m_FlapRearR.localRotation = Quaternion.Lerp(m_FlapRearR.localRotation, Quaternion.Euler(0f, num2 * 50f, 0f), Time.deltaTime * 4f);
		m_Rudder.localRotation = Quaternion.Lerp(m_Rudder.localRotation, Quaternion.Euler(0f, 0f, (0f - m_Plane.Yaw) * 60f), Time.deltaTime * 4f);
		m_PropellerSound.volume = Mathf.Lerp(m_PropellerSound.volume, 1f, Time.deltaTime * 0.5f);
		m_PropellerSound.pitch = Mathf.Lerp(m_PropellerSound.pitch, 1f + Mathf.Pow(Mathf.Abs(m_Plane.Pitch), 2f) * 0.5f, Time.deltaTime * 0.5f);
	}
}
}