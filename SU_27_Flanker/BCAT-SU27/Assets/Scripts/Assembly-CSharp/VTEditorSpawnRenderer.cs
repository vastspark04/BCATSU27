using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(UnitSpawner))]
public class VTEditorSpawnRenderer : MonoBehaviour
{
	public class VTSUnitIconClicker : MonoBehaviour
	{
		public VTEditorSpawnRenderer icon;

		public void MouseDown()
		{
			icon.MouseDown();
		}

		public void MouseEnter()
		{
			icon.MouseEnter();
		}

		public void MouseExit()
		{
			icon.MouseExit();
		}
	}

	private UnitSpawner spawner;

	private Material mat;

	private UnitCatalogue.Unit unit;

	private Mesh[] meshes;

	private Matrix4x4[] matrices;

	public VTScenarioEditor editor;

	private VTScenarioEditor.EditorSprite edSprite;

	private SpriteRenderer sprite;

	private Color unitColor;

	private Color selectionColor = new Color(0.35f, 0.35f, 0f, 0.04f);

	private Color moveColor = new Color(0.5f, 0.4f, 0f, 0.1f);

	private TextMesh nameText;

	private float clickRadius = 5f;

	private List<Mesh> bakedMeshes = new List<Mesh>();

	private bool selected;

	public bool moving { get; private set; }

	private void Start()
	{
		spawner = GetComponent<UnitSpawner>();
		mat = new Material(Shader.Find("Particles/MF-Alpha Blended"));
		unitColor = ((spawner.team == Teams.Allied) ? Color.green : Color.red);
		unitColor.a = 0.06f;
		mat.SetColor("_TintColor", unitColor);
		unit = UnitCatalogue.GetUnit(spawner.unitID);
		bool flag = false;
		GameObject gameObject = (GameObject)Resources.Load(unit.resourcePath);
		MeshFilter[] componentsInChildren = gameObject.GetComponentsInChildren<MeshFilter>();
		Vector3 pos = -gameObject.transform.position;
		Quaternion q = Quaternion.Inverse(gameObject.transform.rotation);
		Matrix4x4 matrix4x = Matrix4x4.TRS(pos, q, Vector3.one);
		List<Mesh> list = new List<Mesh>();
		List<Matrix4x4> list2 = new List<Matrix4x4>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].GetComponent<MeshRenderer>().enabled && componentsInChildren[i].sharedMesh != null && !componentsInChildren[i].gameObject.name.ToLower().Contains("lod"))
			{
				flag = true;
				list.Add(componentsInChildren[i].sharedMesh);
				Matrix4x4 item = matrix4x * componentsInChildren[i].transform.localToWorldMatrix;
				list2.Add(item);
			}
		}
		SkinnedMeshRenderer[] componentsInChildren2 = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		foreach (SkinnedMeshRenderer skinnedMeshRenderer in componentsInChildren2)
		{
			flag = true;
			Mesh mesh = new Mesh();
			skinnedMeshRenderer.BakeMesh(mesh);
			list.Add(mesh);
			bakedMeshes.Add(mesh);
			Matrix4x4 item2 = matrix4x * skinnedMeshRenderer.transform.localToWorldMatrix;
			list2.Add(item2);
		}
		meshes = list.ToArray();
		matrices = list2.ToArray();
		GameObject gameObject2 = new GameObject("Sprite");
		gameObject2.transform.parent = base.transform;
		gameObject2.transform.localPosition = Vector3.zero;
		gameObject2.transform.localScale = Vector3.one;
		sprite = gameObject2.AddComponent<SpriteRenderer>();
		if (!string.IsNullOrEmpty(unit.editorSprite))
		{
			edSprite = editor.GetSprite(unit.editorSprite);
		}
		else
		{
			edSprite = editor.defaultSprite;
		}
		sprite.sprite = edSprite.sprite;
		sprite.color = edSprite.color;
		sprite.sharedMaterial = editor.spriteMaterial;
		IconScaleTest iconScaleTest = gameObject2.AddComponent<IconScaleTest>();
		iconScaleTest.maxDistance = editor.spriteMaxDist;
		iconScaleTest.applyScale = true;
		iconScaleTest.directional = false;
		iconScaleTest.faceCamera = true;
		iconScaleTest.scale = edSprite.size * editor.globalSpriteScale;
		iconScaleTest.cameraUp = true;
		iconScaleTest.updateRoutine = true;
		iconScaleTest.enabled = false;
		iconScaleTest.enabled = true;
		GameObject obj = new GameObject("Label");
		TextMesh textMesh = obj.AddComponent<TextMesh>();
		textMesh.text = spawner.GetUIDisplayName();
		obj.transform.parent = gameObject2.transform;
		obj.transform.localPosition = new Vector3(0f, 0.062f / iconScaleTest.scale, 0f);
		obj.transform.localRotation = Quaternion.identity;
		textMesh.fontSize = editor.iconLabelFontSize;
		obj.transform.localScale = 0.035f / iconScaleTest.scale * Vector3.one;
		textMesh.anchor = TextAnchor.LowerCenter;
		textMesh.color = sprite.color;
		nameText = textMesh;
		editor.OnScenarioObjectsChanged += Editor_OnScenarioObjectsChanged;
		SetupMouseDowns();
		if (!flag)
		{
			base.enabled = false;
		}
	}

	private void Editor_OnScenarioObjectsChanged(VTScenarioEditor.ScenarioChangeEventInfo e)
	{
		if (e.type == VTScenarioEditor.ChangeEventTypes.Units && spawner != null && nameText != null)
		{
			nameText.text = spawner.GetUIDisplayName();
		}
	}

	private void OnDestroy()
	{
		if (editor != null)
		{
			editor.OnScenarioObjectsChanged -= Editor_OnScenarioObjectsChanged;
		}
		if (bakedMeshes != null)
		{
			foreach (Mesh bakedMesh in bakedMeshes)
			{
				Object.Destroy(bakedMesh);
			}
		}
		if ((bool)mat)
		{
			Object.Destroy(mat);
		}
	}

	public void SetSelectedColor()
	{
		selected = true;
		moving = false;
		mat.SetColor("_TintColor", unitColor + selectionColor);
		sprite.color = Color.Lerp(edSprite.color, Color.red, 0.25f);
	}

	public void SetDeselectedColor()
	{
		selected = false;
		moving = false;
		mat.SetColor("_TintColor", unitColor);
		sprite.color = edSprite.color;
	}

	public void SetMovingColor()
	{
		moving = true;
		mat.SetColor("_TintColor", moveColor);
		sprite.color = Color.Lerp(edSprite.color, Color.yellow, 0.35f);
	}

	public void SetHoverColor()
	{
		mat.SetColor("_TintColor", moveColor);
		sprite.color = Color.Lerp(edSprite.color, Color.yellow, 0.35f);
	}

	private void LateUpdate()
	{
		if ((base.transform.position - editor.editorCamera.transform.position).sqrMagnitude < editor.spriteMinDist * editor.spriteMinDist)
		{
			sprite.enabled = false;
			for (int i = 0; i < matrices.Length; i++)
			{
				for (int j = 0; j < meshes[i].subMeshCount; j++)
				{
					Graphics.DrawMesh(meshes[i], base.transform.localToWorldMatrix * matrices[i], mat, 0, null, j, null, castShadows: false);
				}
			}
		}
		else
		{
			sprite.enabled = true;
		}
	}

	private void SetupMouseDowns()
	{
		base.gameObject.AddComponent<SphereCollider>().radius = clickRadius;
		base.gameObject.layer = 5;
		base.gameObject.AddComponent<VTSUnitIconClicker>().icon = this;
		sprite.gameObject.AddComponent<BoxCollider>();
		sprite.gameObject.layer = 5;
		sprite.gameObject.AddComponent<VTSUnitIconClicker>().icon = this;
	}

	public void MouseDown()
	{
		SelectUnit();
	}

	public void MouseEnter()
	{
		SetHoverColor();
	}

	public void MouseExit()
	{
		if (!moving)
		{
			if (selected)
			{
				SetSelectedColor();
			}
			else
			{
				SetDeselectedColor();
			}
		}
	}

	public void SelectUnit()
	{
		if (!editor.unitsTab.isOpen)
		{
			editor.unitsTab.tabMaster.ToggleTab(0);
		}
		editor.unitsTab.SelectUnit(spawner);
		editor.unitsTab.OpenToolsForUnit(spawner);
	}
}
