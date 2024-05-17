using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MFDAntiRadarAttackDisplay : MonoBehaviour, IQSVehicleComponent, IPersistentVehicleData, ILocalizationUser
{
	public MFDPage mfdPage;

	public ModuleRWR rwr;

	public WeaponManager wm;

	public float maxDisplayRange;

	public Transform[] antennaFovLines;

	public RectTransform outerCircleTf;

	public Transform cursorTf;

	public Transform selectionBoxTf;

	public Transform centerTf;

	public GameObject rwrOffObject;

	public GameObject contactTemplate;

	private Text[] contactObjects;

	public float updateInterval = 0.25f;

	public float cursorMoveSpeed = 75f;

	public float cursorSnapDist = 5f;

	[Header("Runtime")]
	public Actor selectedActor;

	private int selectedIdx;

	[Header("HUD")]
	public GameObject hudDisplayObject;

	private bool hudMode;

	public GameObject hudIconTemplate;

	public Transform hudIconSelectTf;

	public CollimatedHUDUI hud;

	private Transform[] hudIconTransforms;

	private float dispRadius;

	private float minRadius;

	private string s_DECL;

	private string s_ON;

	private string s_OFF;

	private int hoverIdx = -1;

	public bool hideCluttered;

	public bool magnifyCursor;

	public float magFactor;

	public float magRadius;

	public void ApplyLocalization()
	{
		s_ON = VTLocalizationManager.GetString("ON");
		s_OFF = VTLocalizationManager.GetString("OFF");
		s_DECL = VTLocalizationManager.GetString("arad_DECL", "DECL", "ARAD declutter button label");
	}

	private void Awake()
	{
		ApplyLocalization();
		if ((bool)mfdPage)
		{
			mfdPage.OnActivatePage.AddListener(OnActivatePage);
			mfdPage.OnDeactivatePage.AddListener(OnDeactivatePage);
			mfdPage.OnInputAxis.AddListener(OnSOIInput);
			mfdPage.OnInputAxisReleased.AddListener(OnReleaseSOIAxis);
			mfdPage.OnInputButtonDown.AddListener(OnSOIButton);
			mfdPage.SetText("hudModeText", hudMode ? s_ON : s_OFF, hudMode ? Color.green : Color.white);
			UpdateDeclutterText();
		}
		rwr.OnEnableRWR += OnEnableRWR;
		rwr.OnDisableRWR += OnDisableRWR;
		contactObjects = new Text[rwr.maxContacts];
		for (int i = 0; i < contactObjects.Length; i++)
		{
			contactObjects[i] = Object.Instantiate(contactTemplate, contactTemplate.transform.parent).GetComponent<Text>();
			contactObjects[i].gameObject.SetActive(value: false);
		}
		contactTemplate.SetActive(value: false);
		selectionBoxTf.gameObject.SetActive(value: false);
		hudIconTransforms = new Transform[rwr.maxContacts];
		for (int j = 0; j < hudIconTransforms.Length; j++)
		{
			hudIconTransforms[j] = Object.Instantiate(hudIconTemplate, hudDisplayObject.transform).transform;
			hudIconTransforms[j].gameObject.SetActive(value: false);
		}
		hudIconSelectTf.gameObject.SetActive(value: false);
		dispRadius = outerCircleTf.rect.height / 2f;
		minRadius = ((RectTransform)centerTf).rect.height / 2f;
		if (rwr.enabled)
		{
			OnEnableRWR();
		}
	}

	private void OnEnableRWR()
	{
		rwrOffObject.SetActive(value: false);
		StartCoroutine(ContactsUpdateRoutine());
	}

	private void OnDisableRWR()
	{
		if ((bool)rwrOffObject)
		{
			rwrOffObject.SetActive(value: true);
		}
		DisableAllContacts();
	}

	private void OnActivatePage()
	{
		rwrOffObject.SetActive(!rwr.enabled);
	}

	private void OnDeactivatePage()
	{
		UpdateWeaponTarget();
	}

	private void DeselectTarget()
	{
		if (selectedActor != null)
		{
			Debug.Log("ARAD target deselected.");
		}
		selectedIdx = -1;
		selectedActor = null;
		if ((bool)selectionBoxTf)
		{
			selectionBoxTf.gameObject.SetActive(value: false);
		}
		if ((bool)hudIconSelectTf)
		{
			hudIconSelectTf.gameObject.SetActive(value: false);
		}
		hoverIdx = -1;
	}

	private void OnSOIInput(Vector3 axis)
	{
		Vector3 vector = cursorTf.localPosition + cursorMoveSpeed * Time.deltaTime * axis;
		vector.x = Mathf.Clamp(vector.x, -50f, 50f);
		vector.y = Mathf.Clamp(vector.y, -50f, 50f);
		Vector3 vector2 = vector - centerTf.localPosition;
		vector2 = Vector3.ClampMagnitude(vector2, dispRadius);
		vector = centerTf.localPosition + vector2;
		cursorTf.localPosition = vector;
		UpdateWeaponTarget();
		if (axis.sqrMagnitude > 0.0025000002f)
		{
			hoverIdx = -1;
		}
	}

	private void OnReleaseSOIAxis()
	{
		int num = -1;
		float num2 = float.MaxValue;
		for (int i = 0; i < contactObjects.Length; i++)
		{
			if (contactObjects[i].gameObject.activeSelf)
			{
				float sqrMagnitude = (contactObjects[i].transform.localPosition - centerTf.InverseTransformPoint(cursorTf.position)).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					num = i;
				}
			}
		}
		if (num2 < cursorSnapDist * cursorSnapDist)
		{
			Vector3 localPosition = cursorTf.parent.InverseTransformPoint(contactObjects[num].transform.position);
			localPosition.z = 0f;
			cursorTf.localPosition = localPosition;
			hoverIdx = num;
		}
	}

	private void OnSOIButton()
	{
		if (hoverIdx >= 0)
		{
			ModuleRWR.RWRContact rWRContact = rwr.contacts[hoverIdx];
			Vector3 localPosition = cursorTf.parent.InverseTransformPoint(contactObjects[hoverIdx].transform.position);
			localPosition.z = 0f;
			cursorTf.localPosition = localPosition;
			if (rWRContact.active)
			{
				selectedIdx = hoverIdx;
				selectedActor = rWRContact.radarActor;
				selectionBoxTf.gameObject.SetActive(value: true);
				selectionBoxTf.localPosition = contactObjects[hoverIdx].transform.localPosition;
				hudIconSelectTf.gameObject.SetActive(value: true);
				hudIconSelectTf.transform.localPosition = hudIconTransforms[hoverIdx].localPosition;
			}
			return;
		}
		int num = -1;
		float num2 = float.MaxValue;
		for (int i = 0; i < contactObjects.Length; i++)
		{
			if (contactObjects[i].gameObject.activeSelf)
			{
				float sqrMagnitude = (contactObjects[i].transform.localPosition - centerTf.InverseTransformPoint(cursorTf.position)).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					num = i;
				}
			}
		}
		if (num2 < cursorSnapDist * cursorSnapDist)
		{
			ModuleRWR.RWRContact rWRContact2 = rwr.contacts[num];
			Vector3 localPosition2 = cursorTf.parent.InverseTransformPoint(contactObjects[num].transform.position);
			localPosition2.z = 0f;
			cursorTf.localPosition = localPosition2;
			if (rWRContact2.active)
			{
				selectedIdx = num;
				selectedActor = rWRContact2.radarActor;
				selectionBoxTf.gameObject.SetActive(value: true);
				selectionBoxTf.localPosition = contactObjects[num].transform.localPosition;
				hudIconSelectTf.gameObject.SetActive(value: true);
				hudIconSelectTf.transform.localPosition = hudIconTransforms[num].localPosition;
			}
		}
		else
		{
			DeselectTarget();
		}
	}

	private void UpdateWeaponTarget()
	{
		if (!wm)
		{
			return;
		}
		ModuleRWR moduleRWR = null;
		if (wm.currentEquip != null && wm.currentEquip is HPEquipARML)
		{
			HPEquipARML obj = (HPEquipARML)wm.currentEquip;
			obj.targetActor = selectedActor;
			Missile nextMissile = obj.ml.GetNextMissile();
			if ((bool)nextMissile)
			{
				moduleRWR = nextMissile.antiRadRWR;
			}
		}
		if (!mfdPage || !mfdPage.isOpen)
		{
			return;
		}
		if (moduleRWR != null)
		{
			int num = -1;
			for (int i = 0; i < 2; i++)
			{
				if ((bool)antennaFovLines[i])
				{
					antennaFovLines[i].gameObject.SetActive(value: true);
					antennaFovLines[i].localRotation = Quaternion.Euler(0f, 0f, (float)num * moduleRWR.antennaFov / 2f);
				}
				num *= -1;
			}
			return;
		}
		for (int j = 0; j < 2; j++)
		{
			if ((bool)antennaFovLines[j])
			{
				antennaFovLines[j].gameObject.SetActive(value: false);
			}
		}
	}

	private IEnumerator ContactsUpdateRoutine()
	{
		while (rwr.contacts == null)
		{
			yield return null;
		}
		while (rwr.enabled)
		{
			for (int i = 0; i < rwr.contacts.Length; i++)
			{
				ModuleRWR.RWRContact rWRContact = rwr.contacts[i];
				if (rWRContact.active)
				{
					if ((bool)mfdPage && mfdPage.isOpen)
					{
						contactObjects[i].gameObject.SetActive(value: true);
						contactObjects[i].text = rWRContact.radarSymbol;
						Vector3 vector = WorldToDisplayPosition(rWRContact.detectedPosition);
						if (magnifyCursor && i != hoverIdx)
						{
							Vector3 vector2 = vector - cursorTf.localPosition;
							float magnitude = vector2.magnitude;
							float num = 1f - Mathf.Clamp01(magnitude / magRadius);
							num *= num;
							vector2 = Vector3.Lerp(vector2, magFactor * vector2, num);
							vector = cursorTf.localPosition + vector2;
						}
						contactObjects[i].transform.localPosition = vector;
						if (i == selectedIdx)
						{
							selectionBoxTf.localPosition = contactObjects[i].transform.localPosition;
						}
						if (i == hoverIdx)
						{
							Vector3 localPosition = cursorTf.parent.InverseTransformPoint(contactObjects[i].transform.position);
							localPosition.z = 0f;
							cursorTf.localPosition = localPosition;
						}
					}
					if (hudMode)
					{
						hudIconTransforms[i].gameObject.SetActive(value: true);
						hudIconTransforms[i].position = WorldToHUDPosition(rWRContact.detectedPosition);
						hudIconTransforms[i].rotation = Quaternion.LookRotation(hudIconTransforms[i].position - VRHead.position, hudDisplayObject.transform.up);
						if (i == selectedIdx)
						{
							hudIconSelectTf.localPosition = hudIconTransforms[i].localPosition;
							hudIconSelectTf.localRotation = hudIconTransforms[i].localRotation;
						}
					}
				}
				else
				{
					if (i == selectedIdx)
					{
						DeselectTarget();
					}
					if ((bool)mfdPage && mfdPage.isOpen)
					{
						contactObjects[i].gameObject.SetActive(value: false);
					}
					if (hudMode)
					{
						hudIconTransforms[i].gameObject.SetActive(value: false);
					}
				}
			}
			if (hideCluttered)
			{
				float num2 = 7f * 7f;
				for (int j = 0; j < contactObjects.Length; j++)
				{
					if (!contactObjects[j].gameObject.activeSelf)
					{
						continue;
					}
					Text text = contactObjects[j];
					for (int k = 0; k < j; k++)
					{
						Text text2 = contactObjects[k];
						if (text2.gameObject.activeSelf && (text.transform.localPosition - text2.transform.localPosition).sqrMagnitude < num2)
						{
							int num3 = ((j == selectedIdx) ? k : ((k == selectedIdx) ? j : ((rwr.contacts[k].locked && !rwr.contacts[j].locked) ? j : ((rwr.contacts[j].locked && !rwr.contacts[k].locked) ? k : ((!(rwr.contacts[k].signalStrength > rwr.contacts[j].signalStrength)) ? k : j)))));
							contactObjects[num3].gameObject.SetActive(value: false);
						}
					}
				}
			}
			UpdateWeaponTarget();
			yield return null;
		}
	}

	public void ToggleDeclutterButton()
	{
		hideCluttered = !hideCluttered;
		UpdateDeclutterText();
	}

	private void UpdateDeclutterText()
	{
		mfdPage.SetText("declText", s_DECL, hideCluttered ? Color.green : Color.white);
	}

	private Vector3 WorldToHUDPosition(Vector3 worldPos)
	{
		Vector3 vector = worldPos - VRHead.position;
		return VRHead.position + vector.normalized * hud.depth;
	}

	private Vector3 WorldToDisplayPosition(Vector3 worldPos)
	{
		Vector3 lhs = worldPos - rwr.transform.position;
		lhs.y = 0f;
		float magnitude = lhs.magnitude;
		Vector3 forward = rwr.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 rhs = Vector3.Cross(Vector3.up, forward);
		float x = Vector3.Dot(lhs, rhs);
		float y = Vector3.Dot(lhs, forward);
		Vector3 vector = new Vector3(x, y, 0f);
		float num = ((RectTransform)cursorTf).rect.width * 0.7f;
		return Mathf.Lerp(minRadius, dispRadius - num, magnitude / maxDisplayRange) * vector.normalized;
	}

	private void DisableAllContacts()
	{
		if (contactObjects != null)
		{
			for (int i = 0; i < contactObjects.Length; i++)
			{
				if ((bool)contactObjects[i])
				{
					contactObjects[i].gameObject.SetActive(value: false);
				}
				if ((bool)hudIconTransforms[i])
				{
					hudIconTransforms[i].gameObject.SetActive(value: false);
				}
			}
		}
		DeselectTarget();
		UpdateWeaponTarget();
	}

	public void ToggleHUDMode()
	{
		hudMode = !hudMode;
		hudDisplayObject.SetActive(hudMode);
		mfdPage.SetText("hudModeText", hudMode ? s_ON : s_OFF, hudMode ? Color.green : Color.white);
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode("ARAD");
		qsNode.AddNode(configNode);
		configNode.SetValue("selectedIdx", selectedIdx);
		configNode.SetValue("hudMode", hudMode);
		rwr.OnQuicksave(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = "ARAD";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			rwr.OnQuickload(node);
			Debug.Log("Quickloading ARAD");
			int value = node.GetValue<int>("selectedIdx");
			if (value >= 0 && rwr.contacts[value] != null && rwr.contacts[value].active)
			{
				selectedIdx = value;
				selectedActor = rwr.contacts[value].radarActor;
				selectionBoxTf.gameObject.SetActive(value: true);
				selectionBoxTf.localPosition = contactObjects[value].transform.localPosition;
				hudIconSelectTf.gameObject.SetActive(value: true);
				hudIconSelectTf.transform.localPosition = hudIconTransforms[value].localPosition;
				Debug.Log(" - selectedActor quickloaded: " + (selectedActor ? selectedActor.name : "null"));
			}
			bool value2 = node.GetValue<bool>("hudMode");
			hudMode = !value2;
			ToggleHUDMode();
		}
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		vDataNode.AddOrGetNode("ARAD").SetValue("hudMode", hudMode);
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		ConfigNode node = vDataNode.GetNode("ARAD");
		if (node != null && node.GetValue<bool>("hudMode") != hudMode)
		{
			ToggleHUDMode();
		}
	}
}
