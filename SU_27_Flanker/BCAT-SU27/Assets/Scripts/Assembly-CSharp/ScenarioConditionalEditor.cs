using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class ScenarioConditionalEditor : MonoBehaviour
{
	public enum FinishStates
	{
		Empty,
		Complete,
		Incomplete,
		Cancelled
	}

	[Serializable]
	public struct ComponentUIInfo
	{
		public GameObject nodeTemplate;

		public string componentType;
	}

	public VTScenarioEditor editor;

	public ScrollRect scrollRect;

	public List<ComponentUIInfo> uiInfos;

	public GameObject connectionLineTemplate;

	public SCCOutputUI outputNode;

	private Dictionary<string, GameObject> uiPrefabs;

	private Dictionary<int, SCCNodeUI> nodeUIs = new Dictionary<int, SCCNodeUI>();

	private List<UILineRenderer> connectionLines = new List<UILineRenderer>();

	private ScenarioConditional activeConditional;

	private ScenarioConditional editingConditional;

	public GameObject[] hideOnMissionConditional;

	private ScenarioConditional testC;

	private bool initialized;

	private const string LOCK_NAME = "conditionalEditor";

	public float scaleRate = 16f;

	public float scaleLerpRate = 2f;

	private float scale = 1f;

	private float lerpedScale = 1f;

	private Vector3 scaleMousePos;

	private bool editingConnection;

	private SCCPortUI editOutputPort;

	private SCCPortUI editInputPort;

	private UILineRenderer editLineRenderer;

	private int editingOutputLineIdx;

	private RectTransform contentTransform => scrollRect.content;

	public event UnityAction<FinishStates> OnFinishedEdit;

	public void Test()
	{
		if (testC == null)
		{
			testC = new ScenarioConditional();
		}
		Open(testC, isMission: false);
	}

	public void SetRootNode(SCCNodeUI node)
	{
		if ((bool)node)
		{
			editingConditional.rootComponent = node.component;
		}
		else
		{
			editingConditional.rootComponent = null;
		}
	}

	public void RemoveConnectionLine(UILineRenderer line)
	{
		connectionLines.Remove(line);
	}

	private void Initialize()
	{
		uiPrefabs = new Dictionary<string, GameObject>();
		foreach (ComponentUIInfo uiInfo in uiInfos)
		{
			uiPrefabs.Add(uiInfo.componentType, uiInfo.nodeTemplate);
			uiInfo.nodeTemplate.gameObject.SetActive(value: false);
		}
		connectionLineTemplate.gameObject.SetActive(value: false);
		outputNode.Initialize(null);
		initialized = true;
	}

	public void Open(ScenarioConditional conditional, bool isMission)
	{
		if (editingConditional != null)
		{
			Debug.LogError("Tried to open conditional editor but it was already editing something.");
			return;
		}
		if (hideOnMissionConditional != null)
		{
			hideOnMissionConditional.SetActive(!isMission);
		}
		if ((bool)editor)
		{
			editor.BlockEditor(base.transform);
			editor.editorCamera.inputLock.AddLock("conditionalEditor");
			editor.editorCamera.scrollLock.AddLock("conditionalEditor");
			editor.editorCamera.doubleClickLock.AddLock("conditionalEditor");
			editor.OnBeforeSave += Save;
		}
		base.gameObject.SetActive(value: true);
		if (!initialized)
		{
			Initialize();
		}
		activeConditional = conditional;
		ConfigNode conditionalNode = activeConditional.SaveToNode();
		editingConditional = new ScenarioConditional();
		editingConditional.LoadFromNode(conditionalNode);
		outputNode.transform.localPosition = editingConditional.outputNodePos;
		foreach (ScenarioConditionalComponent value in editingConditional.components.Values)
		{
			CreateNodeForComponent(value);
		}
		foreach (SCCNodeUI value2 in nodeUIs.Values)
		{
			SetupConnectionsForLoadedNode(value2);
		}
		if (conditional.rootComponent != null)
		{
			SCCNodeUI sCCNodeUI = nodeUIs[conditional.rootComponent.id];
			sCCNodeUI.outputPort.connections.Add(outputNode.inputPorts[0]);
			outputNode.inputPorts[0].connections.Add(sCCNodeUI.outputPort);
			UILineRenderer item = NewConnectionLine();
			sCCNodeUI.outputPort.lines.Add(item);
			outputNode.inputPorts[0].lines.Add(item);
			connectionLines.Add(item);
			UpdateConnectionLines(sCCNodeUI);
		}
	}

	private void Update()
	{
		UpdateScaling();
	}

	private void UpdateScaling()
	{
		if (((RectTransform)scrollRect.transform).rect.Contains(Input.mousePosition - scrollRect.transform.position))
		{
			scaleMousePos = Input.mousePosition;
			float num = Input.mouseScrollDelta.y * Time.unscaledDeltaTime * scaleRate;
			scale *= 1f + num;
			scale = Mathf.Clamp(scale, 0.1f, 2f);
		}
		Vector3 vector = contentTransform.position - scaleMousePos;
		float num2 = lerpedScale;
		lerpedScale = Mathf.Lerp(lerpedScale, scale, scaleLerpRate * Time.unscaledDeltaTime);
		float num3 = (lerpedScale - num2) / num2;
		contentTransform.localScale = new Vector3(lerpedScale, lerpedScale, lerpedScale);
		vector *= 1f + num3;
		contentTransform.position = scaleMousePos + vector;
	}

	public void Save()
	{
		if (editingConditional == null)
		{
			return;
		}
		editingConditional.outputNodePos = outputNode.transform.localPosition;
		foreach (SCCNodeUI value in nodeUIs.Values)
		{
			value.UpdateComponent();
		}
		ConfigNode conditionalNode = editingConditional.SaveToNode();
		activeConditional.LoadFromNode(conditionalNode);
	}

	public void SaveAndClose()
	{
		Save();
		FinishStates finishState = FinishStates.Incomplete;
		if (activeConditional.components.Count == 0)
		{
			finishState = FinishStates.Empty;
		}
		else if (activeConditional.rootComponent != null && !HasEmptyInputs(outputNode))
		{
			finishState = FinishStates.Complete;
		}
		Close(finishState);
	}

	private bool HasEmptyInputs(SCCNodeUI nodeUI)
	{
		foreach (SCCPortUI inputPort in nodeUI.inputPorts)
		{
			if (inputPort.connections.Count == 0)
			{
				return true;
			}
			if (HasEmptyInputs(inputPort.connections[0].nodeUI))
			{
				return true;
			}
		}
		return false;
	}

	public void CancelAndClose()
	{
		Close(FinishStates.Cancelled);
	}

	private void Close(FinishStates finishState)
	{
		foreach (SCCNodeUI value in nodeUIs.Values)
		{
			UnityEngine.Object.Destroy(value.gameObject);
		}
		nodeUIs.Clear();
		foreach (UILineRenderer connectionLine in connectionLines)
		{
			UnityEngine.Object.Destroy(connectionLine.gameObject);
		}
		connectionLines.Clear();
		outputNode.inputPorts[0].connections.Clear();
		outputNode.inputPorts[0].lines.Clear();
		contentTransform.localPosition = Vector3.zero;
		if ((bool)editor)
		{
			editor.UnblockEditor(base.transform);
			editor.editorCamera.inputLock.RemoveLock("conditionalEditor");
			editor.editorCamera.scrollLock.RemoveLock("conditionalEditor");
			editor.editorCamera.doubleClickLock.RemoveLock("conditionalEditor");
			editor.OnBeforeSave -= Save;
		}
		if (this.OnFinishedEdit != null)
		{
			this.OnFinishedEdit(finishState);
		}
		activeConditional = null;
		editingConditional = null;
		base.gameObject.SetActive(value: false);
	}

	private void CreateNodeForComponent(ScenarioConditionalComponent component)
	{
		GameObject obj = UnityEngine.Object.Instantiate(uiPrefabs[component.GetType().ToString()], contentTransform);
		obj.gameObject.SetActive(value: true);
		obj.transform.localPosition = component.uiPos;
		SCCNodeUI componentImplementing = obj.GetComponentImplementing<SCCNodeUI>();
		nodeUIs.Add(component.id, componentImplementing);
		componentImplementing.Initialize(component);
	}

	private void SetupConnectionsForLoadedNode(SCCNodeUI nodeUI)
	{
		for (int i = 0; i < nodeUI.inputPorts.Count; i++)
		{
			int inputID = nodeUI.component.GetInputID(i);
			if (inputID >= 0)
			{
				if (nodeUIs.TryGetValue(inputID, out var value))
				{
					nodeUI.inputPorts[i].connections.Add(value.outputPort);
					UILineRenderer item = NewConnectionLine();
					nodeUI.inputPorts[i].lines.Add(item);
					connectionLines.Add(item);
					value.outputPort.connections.Add(nodeUI.inputPorts[i]);
					value.outputPort.lines.Add(item);
				}
				else
				{
					Debug.LogError("Loaded node is missing reference to input node.");
				}
			}
		}
		UpdateConnectionLines(nodeUI);
	}

	public void CreateNode(string nodeType)
	{
		ScenarioConditionalComponent scenarioConditionalComponent = (ScenarioConditionalComponent)Activator.CreateInstance(Type.GetType(nodeType));
		scenarioConditionalComponent.conditionalSys = editingConditional;
		scenarioConditionalComponent.id = editingConditional.GetNewComponentID();
		scenarioConditionalComponent.uiPos = contentTransform.InverseTransformPoint(contentTransform.parent.position);
		editingConditional.components.Add(scenarioConditionalComponent.id, scenarioConditionalComponent);
		CreateNodeForComponent(scenarioConditionalComponent);
		nodeUIs[scenarioConditionalComponent.id].UpdateComponent();
	}

	public void DeleteNode(SCCNodeUI nodeUI)
	{
		if (editingConnection)
		{
			CancelEditingLine();
		}
		editingConditional.components.Remove(nodeUI.component.id);
		List<SCCNodeUI> list = new List<SCCNodeUI>();
		foreach (SCCPortUI inputPort in nodeUI.inputPorts)
		{
			if (inputPort.connections.Count > 0 && inputPort.connections[0] != null)
			{
				SCCPortUI sCCPortUI = inputPort.connections[0];
				UILineRenderer uILineRenderer = inputPort.lines[0];
				sCCPortUI.connections.Remove(inputPort);
				sCCPortUI.lines.Remove(uILineRenderer);
				list.Add(sCCPortUI.nodeUI);
				connectionLines.Remove(uILineRenderer);
				UnityEngine.Object.Destroy(uILineRenderer.gameObject);
			}
		}
		if (nodeUI.outputPort != null)
		{
			foreach (SCCPortUI connection in nodeUI.outputPort.connections)
			{
				connection.connections.Clear();
				connection.lines.Clear();
				list.Add(connection.nodeUI);
			}
			foreach (UILineRenderer line in nodeUI.outputPort.lines)
			{
				connectionLines.Remove(line);
				UnityEngine.Object.Destroy(line.gameObject);
			}
		}
		nodeUIs.Remove(nodeUI.component.id);
		UnityEngine.Object.Destroy(nodeUI.gameObject);
		foreach (SCCNodeUI item in list)
		{
			item.UpdateComponent();
		}
	}

	public void ClickedOutputPort(SCCPortUI port)
	{
		if (editingConnection)
		{
			if ((bool)editInputPort && editInputPort.nodeUI != port.nodeUI)
			{
				editInputPort.connections.Clear();
				editInputPort.connections.Add(port);
				editInputPort.lines.Clear();
				editInputPort.lines.Add(editLineRenderer);
				port.connections.Add(editInputPort);
				port.lines.Add(editLineRenderer);
				connectionLines.Add(editLineRenderer);
				editInputPort.nodeUI.UpdateComponent();
				port.nodeUI.UpdateComponent();
				StopEditingLine();
				UpdateConnectionLines(port);
			}
			else if (editOutputPort == port)
			{
				CancelEditingLine();
			}
		}
		else
		{
			editOutputPort = port;
			editLineRenderer = NewConnectionLine();
			editingConnection = true;
			editingOutputLineIdx = port.connections.Count;
			StartCoroutine(ConnectionEditRoutine());
		}
	}

	public void ClickedInputPort(SCCPortUI port)
	{
		if (editingConnection)
		{
			if ((bool)editOutputPort && editOutputPort.nodeUI != port.nodeUI && port.connections.Count == 0)
			{
				editOutputPort.connections.Add(port);
				editOutputPort.lines.Add(editLineRenderer);
				port.connections.Add(editOutputPort);
				port.lines.Add(editLineRenderer);
				connectionLines.Add(editLineRenderer);
				editOutputPort.nodeUI.UpdateComponent();
				port.nodeUI.UpdateComponent();
				StopEditingLine();
				UpdateConnectionLines(port);
			}
			else if (editInputPort == port)
			{
				CancelEditingLine();
			}
			return;
		}
		if (port.connections.Count > 0)
		{
			editOutputPort = port.connections[0];
			editOutputPort.connections.Remove(port);
			editOutputPort.lines.Remove(port.lines[0]);
			editingOutputLineIdx = editOutputPort.connections.Count;
			editLineRenderer = port.lines[0];
			port.connections.Clear();
			port.lines.Clear();
			port.nodeUI.UpdateComponent();
			connectionLines.Remove(editLineRenderer);
		}
		else
		{
			editInputPort = port;
			editLineRenderer = NewConnectionLine();
		}
		editingConnection = true;
		StartCoroutine(ConnectionEditRoutine());
	}

	private void CancelEditingLine()
	{
		if ((bool)editOutputPort && editingOutputLineIdx < editOutputPort.connections.Count)
		{
			_ = editOutputPort.connections[editingOutputLineIdx];
			editOutputPort.connections.RemoveAt(editingOutputLineIdx);
			editOutputPort.lines.RemoveAt(editingOutputLineIdx);
			editOutputPort.nodeUI.UpdateComponent();
		}
		UnityEngine.Object.Destroy(editLineRenderer.gameObject);
		StopEditingLine();
	}

	private void StopEditingLine()
	{
		editOutputPort = null;
		editInputPort = null;
		editLineRenderer = null;
		editingOutputLineIdx = -1;
		editingConnection = false;
	}

	private UILineRenderer NewConnectionLine()
	{
		GameObject obj = UnityEngine.Object.Instantiate(connectionLineTemplate, contentTransform);
		obj.SetActive(value: true);
		obj.transform.localPosition = Vector3.zero;
		return obj.GetComponent<UILineRenderer>();
	}

	private IEnumerator ConnectionEditRoutine()
	{
		while (editingConnection)
		{
			if ((bool)editOutputPort)
			{
				UpdateLinePositions(editLineRenderer, editOutputPort.transform.position, MousePosUI());
			}
			else if ((bool)editInputPort)
			{
				UpdateLinePositions(editLineRenderer, MousePosUI(), editInputPort.transform.position);
			}
			if (Input.GetMouseButtonDown(1))
			{
				CancelEditingLine();
				break;
			}
			yield return null;
		}
	}

	private Vector2 MousePosUI()
	{
		return Input.mousePosition;
	}

	public void UpdateConnectionLines(SCCNodeUI node)
	{
		foreach (SCCPortUI inputPort in node.inputPorts)
		{
			UpdateConnectionLines(inputPort);
		}
		if ((bool)node.outputPort)
		{
			UpdateConnectionLines(node.outputPort);
		}
	}

	public void UpdateConnectionLines(SCCPortUI port)
	{
		for (int i = 0; i < port.connections.Count; i++)
		{
			if (port.isInput)
			{
				UpdateLinePositions(port.lines[i], port.connections[i].transform.position, port.transform.position);
			}
			else
			{
				UpdateLinePositions(port.lines[i], port.transform.position, port.connections[i].transform.position);
			}
		}
	}

	public void UpdateLinePositions(UILineRenderer line, Vector3 outputPosition, Vector3 inputPosition)
	{
		int num = 10;
		Vector2[] array = new Vector2[num];
		for (int i = 0; i < num; i++)
		{
			float num2 = (float)i / (float)(num - 1);
			float num3 = Mathf.Abs(outputPosition.x - inputPosition.x);
			Vector3 a = outputPosition + num3 * num2 * Vector3.right;
			Vector3 b = inputPosition + num3 * (1f - num2) * Vector3.left;
			float t = 0.5f + Mathf.Atan((num2 - 0.5f) * 8f) / 2.6389377f;
			Vector3 position = Vector3.Lerp(a, b, t);
			array[i] = contentTransform.InverseTransformPoint(position);
		}
		line.Points = array;
	}
}
