using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestCustomScenarioLauncherUI : MonoBehaviour
{
	public class TestScenarioButton : MonoBehaviour
	{
		public TestCustomScenarioLauncherUI ui;

		public int idx;

		public void Click()
		{
			ui.LaunchScenario(idx);
		}
	}

	public GameObject buttonTemplate;

	public RectTransform contentTf;

	private List<VTScenarioInfo> scenarios;

	private void Start()
	{
		VTResources.LoadCustomScenarios();
		scenarios = VTResources.GetCustomScenarios();
		float height = ((RectTransform)buttonTemplate.transform).rect.height;
		for (int i = 0; i < scenarios.Count; i++)
		{
			GameObject obj = Object.Instantiate(buttonTemplate, contentTf);
			obj.transform.localPosition = new Vector3(0f, (float)(-i) * height, 0f);
			obj.SetActive(value: true);
			TestScenarioButton testScenarioButton = obj.AddComponent<TestScenarioButton>();
			testScenarioButton.idx = i;
			testScenarioButton.ui = this;
			obj.GetComponent<Button>().onClick.AddListener(testScenarioButton.Click);
			obj.GetComponentInChildren<Text>().text = scenarios[i].name;
		}
		contentTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height * (float)scenarios.Count);
		buttonTemplate.SetActive(value: false);
	}

	public void LaunchScenario(int idx)
	{
		VTScenario.LaunchScenario(scenarios[idx]);
	}
}
