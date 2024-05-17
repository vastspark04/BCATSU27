using System;
using UnityEngine;
using UnityEngine.UI;

public class ExamplesController : MonoBehaviour
{
	[Serializable]
	public class Example
	{
		public string Name;

		[Multiline]
		public string Description;

		public Transform Prefab;
	}

	public Example[] m_Examples;

	public GameObject m_Canvas;

	public Slider m_ExamplesSlider;

	private Text m_Decription;

	private GameObject m_CurrentPrefab;

	private int m_CurrentExample;

	private void Start()
	{
		m_Decription = GameObject.Find("ExampleDescription").GetComponent<Text>();
		activateExample(0);
		if (m_ExamplesSlider != null)
		{
			m_ExamplesSlider.maxValue = m_Examples.Length - 1;
		}
	}

	public void NextExample()
	{
		if (m_Examples[m_CurrentExample].Prefab != m_CurrentPrefab && m_CurrentPrefab != null)
		{
			UnityEngine.Object.Destroy(m_CurrentPrefab);
		}
		m_CurrentExample++;
		ClampExampleCount();
		m_CurrentPrefab = UnityEngine.Object.Instantiate(m_Examples[m_CurrentExample].Prefab, Vector3.zero, Quaternion.identity).gameObject;
		if (m_Decription != null)
		{
			m_Decription.text = m_Examples[m_CurrentExample].Description;
		}
	}

	public void PreviousExample()
	{
		if (m_Examples[m_CurrentExample].Prefab != m_CurrentPrefab && m_CurrentPrefab != null)
		{
			UnityEngine.Object.Destroy(m_CurrentPrefab);
		}
		m_CurrentExample--;
		ClampExampleCount();
		m_CurrentPrefab = UnityEngine.Object.Instantiate(m_Examples[m_CurrentExample].Prefab, Vector3.zero, Quaternion.identity).gameObject;
		if (m_Decription != null)
		{
			m_Decription.text = m_Examples[m_CurrentExample].Description;
		}
	}

	private void ClampExampleCount()
	{
		if (m_CurrentExample < 0)
		{
			m_CurrentExample = m_Examples.Length - 1;
		}
		if (m_CurrentExample > m_Examples.Length - 1)
		{
			m_CurrentExample = 0;
		}
	}

	public void activateExampleFromSlider()
	{
		if (m_ExamplesSlider != null)
		{
			activateExample((int)m_ExamplesSlider.value);
		}
	}

	public void activateExample(int index)
	{
		if (m_Examples[index].Prefab != m_CurrentPrefab && m_CurrentPrefab != null)
		{
			UnityEngine.Object.Destroy(m_CurrentPrefab);
		}
		m_CurrentPrefab = UnityEngine.Object.Instantiate(m_Examples[index].Prefab, Vector3.zero, Quaternion.identity).gameObject;
		if (m_Decription != null)
		{
			m_Decription.text = m_Examples[index].Description;
		}
	}
}
