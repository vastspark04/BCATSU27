using UnityEngine;

public class PilotColorEditorUI : MonoBehaviour
{
	public GameObject previousMenuObject;

	public VRColorPicker suitPicker;

	public VRColorPicker vestPicker;

	public VRColorPicker gSuitPicker;

	public VRColorPicker strapsPicker;

	public VRColorPicker skinPicker;

	public GameObject pilotPreviewObject;

	public Renderer[] pilotRenderers;

	private Color suitColor;

	private Color vestColor;

	private Color gSuitColor;

	private Color strapsColor;

	private Color skinColor;

	private MaterialPropertyBlock colorProps;

	private int suitID = -1;

	private int vestID;

	private int gSuitID;

	private int strapsID;

	private int skinID;

	private PilotSave ps;

	private void Start()
	{
		suitPicker.onColorChanged.AddListener(OnSuitChanged);
		vestPicker.onColorChanged.AddListener(OnVestChanged);
		gSuitPicker.onColorChanged.AddListener(OnGSuitChanged);
		strapsPicker.onColorChanged.AddListener(OnStrapsChanged);
		skinPicker.onColorChanged.AddListener(OnSkinChanged);
	}

	public void OpenForPilot(PilotSave ps)
	{
		base.gameObject.SetActive(value: true);
		previousMenuObject.SetActive(value: false);
		pilotPreviewObject.SetActive(value: true);
		this.ps = ps;
		colorProps = new MaterialPropertyBlock();
		suitColor = ps.suitColor;
		vestColor = ps.vestColor;
		gSuitColor = ps.gSuitColor;
		strapsColor = ps.strapsColor;
		skinColor = ps.skinColor;
		suitPicker.SetColor(ps.suitColor);
		vestPicker.SetColor(ps.vestColor);
		gSuitPicker.SetColor(ps.gSuitColor);
		strapsPicker.SetColor(ps.strapsColor);
		skinPicker.SetColor(ps.skinColor);
		UpdateProperties();
	}

	public void Accept()
	{
		ps.suitColor = suitColor;
		ps.vestColor = vestColor;
		ps.strapsColor = strapsColor;
		ps.gSuitColor = gSuitColor;
		ps.skinColor = skinColor;
		PilotSaveManager.SavePilotsToFile();
		Cancel();
	}

	public void Cancel()
	{
		pilotPreviewObject.SetActive(value: false);
		base.gameObject.SetActive(value: false);
		previousMenuObject.SetActive(value: true);
	}

	private void UpdateProperties()
	{
		if (suitID == -1)
		{
			suitID = Shader.PropertyToID("_BaseColor");
			vestID = Shader.PropertyToID("_ColorB");
			strapsID = Shader.PropertyToID("_ColorG");
			gSuitID = Shader.PropertyToID("_ColorR");
			skinID = Shader.PropertyToID("_SkinColor");
		}
		colorProps.SetColor(suitID, suitColor);
		colorProps.SetColor(vestID, vestColor);
		colorProps.SetColor(strapsID, strapsColor);
		colorProps.SetColor(gSuitID, gSuitColor);
		colorProps.SetColor(skinID, skinColor);
		for (int i = 0; i < pilotRenderers.Length; i++)
		{
			pilotRenderers[i].SetPropertyBlock(colorProps);
		}
	}

	private void OnSuitChanged(Color c)
	{
		suitColor = c;
		UpdateProperties();
	}

	private void OnVestChanged(Color c)
	{
		vestColor = c;
		UpdateProperties();
	}

	private void OnGSuitChanged(Color c)
	{
		gSuitColor = c;
		UpdateProperties();
	}

	private void OnStrapsChanged(Color c)
	{
		strapsColor = c;
		UpdateProperties();
	}

	private void OnSkinChanged(Color c)
	{
		skinColor = c;
		UpdateProperties();
	}
}
