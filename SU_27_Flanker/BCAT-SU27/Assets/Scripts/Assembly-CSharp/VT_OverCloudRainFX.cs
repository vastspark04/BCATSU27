using OC;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(AudioSource))]
public class VT_OverCloudRainFX : MonoBehaviour
{
	[SerializeField]
	private OverCloudProbe m_CloudProbe;

	[SerializeField]
	private float m_VelocityRotationSpeed = 10f;

	[SerializeField]
	private float m_RotationRelax = 10f;

	private ParticleSystem m_ParticleSystem;

	private ParticleSystem.EmissionModule m_Emission;

	private AudioSource m_AudioSource;

	private Vector3 m_LastPos;

	private void Start()
	{
		m_ParticleSystem = GetComponent<ParticleSystem>();
		m_Emission = m_ParticleSystem.emission;
		m_AudioSource = GetComponent<AudioSource>();
	}

	private void OnEnable()
	{
		m_LastPos = base.transform.position;
	}

	private void LateUpdate()
	{
		float rain = m_CloudProbe.rain;
		if (rain > Mathf.Epsilon)
		{
			m_ParticleSystem.Play();
		}
		m_Emission.rateOverTime = rain * 10000f;
		m_AudioSource.volume = rain;
		Vector3 vector = (base.transform.position - m_LastPos) / Time.deltaTime;
		vector += Vector3.up * m_RotationRelax;
		base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.LookRotation(-vector.normalized), m_VelocityRotationSpeed * Time.deltaTime);
		m_LastPos = base.transform.position;
	}
}
