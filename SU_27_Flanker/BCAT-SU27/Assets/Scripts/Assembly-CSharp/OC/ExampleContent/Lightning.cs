using UnityEngine;

namespace OC.ExampleContent{

[ExecuteInEditMode]
public class Lightning : MonoBehaviour
{
	[SerializeField]
	[Range(0f, 1f)]
	private float m_Phase;

	[SerializeField]
	private float m_PlaySpeed = 1f;

	[SerializeField]
	private bool m_PlayInEditor = true;

	[SerializeField]
	private LayerMask m_ImpactLayers = 0;

	[Header("Components")]
	[SerializeField]
	private Transform m_RendererPivot;

	[SerializeField]
	private MeshRenderer m_Renderer;

	[SerializeField]
	private Material m_Material;

	[SerializeField]
	private Light m_Light;

	[SerializeField]
	private AudioSource m_SoundEffect;

	[SerializeField]
	private ParticleSystem m_ImpactSparks;

	[Header("Point Light Settings")]
	[SerializeField]
	private Gradient m_PhaseColor;

	private void OnValidate()
	{
		if ((bool)m_Light && (bool)m_Material)
		{
			UpdateComponents();
		}
	}

	private void OnEnable()
	{
		RestartLightning();
	}

	private void OnDisable()
	{
	}

	private void EditorUpdate()
	{
		if (!Application.isPlaying && m_PlayInEditor)
		{
			UpdateLightning();
		}
	}

	private void Update()
	{
		if (Application.isPlaying)
		{
			UpdateLightning();
		}
	}

	private void RestartLightning()
	{
		if ((!Application.isPlaying && !m_PlayInEditor) || !Physics.Raycast(new Ray(base.transform.position, Vector3.down), out var hitInfo, 99999f, m_ImpactLayers))
		{
			return;
		}
		float num = base.transform.position.y - hitInfo.point.y;
		m_RendererPivot.localScale = Vector3.one * num;
		m_Material.SetInt("_TexIndex", Random.Range(0, 4));
		m_Phase = 0f;
		if (Application.isPlaying)
		{
			if ((bool)m_SoundEffect)
			{
				m_SoundEffect.Play();
			}
			if ((bool)m_ImpactSparks)
			{
				m_ImpactSparks.transform.position = hitInfo.point;
				m_ImpactSparks.Play();
			}
		}
	}

	private void UpdateLightning()
	{
		m_Phase = Mathf.Min(m_Phase + Time.deltaTime * m_PlaySpeed, 1f);
		UpdateComponents();
	}

	private void UpdateComponents()
	{
		bool flag = m_Phase < 1f;
		m_Renderer.enabled = flag;
		m_Light.enabled = flag;
		if (flag)
		{
			m_Light.color = m_PhaseColor.Evaluate(m_Phase);
			m_Material.SetFloat("_Phase", m_Phase);
		}
		if ((bool)m_SoundEffect)
		{
			flag = flag || m_SoundEffect.isPlaying;
		}
		if ((bool)m_ImpactSparks)
		{
			flag = flag || m_ImpactSparks.isPlaying;
		}
		if (!flag)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}}
