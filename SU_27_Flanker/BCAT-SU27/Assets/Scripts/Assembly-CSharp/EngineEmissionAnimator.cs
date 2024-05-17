using UnityEngine;

public class EngineEmissionAnimator : MonoBehaviour
{
	public MeshRenderer meshRenderer;

	public MeshRenderer[] meshRenderers;

	public Gradient colorOverThrottle;

	public AnimationCurve intensityCurve;

	public float abMultiplier;

	public ModuleEngine engine;

	private MaterialPropertyBlock properties;

	private int emissionID;

	private bool useArr;

	private bool useLodBase;

	private LODBase lodBase;

	private void Awake()
	{
		lodBase = GetComponentInParent<LODBase>();
		if (lodBase != null)
		{
			useLodBase = true;
		}
	}

	private void Start()
	{
		if (meshRenderers != null && meshRenderers.Length != 0)
		{
			useArr = true;
			meshRenderer = meshRenderers[0];
		}
		properties = new MaterialPropertyBlock();
		meshRenderer.GetPropertyBlock(properties);
		emissionID = Shader.PropertyToID("_EmissionColor");
	}

	private void Update()
	{
		if (useLodBase && !(lodBase.sqrDist < 9000000f))
		{
			return;
		}
		properties.SetColor(emissionID, colorOverThrottle.Evaluate(engine.finalThrottle) * intensityCurve.Evaluate(engine.finalThrottle) * (1f + abMultiplier * engine.abMult));
		meshRenderer.SetPropertyBlock(properties);
		if (useArr)
		{
			for (int i = 1; i < meshRenderers.Length; i++)
			{
				meshRenderers[i].SetPropertyBlock(properties);
			}
		}
	}

	[ContextMenu("Clear MRs")]
	public void ClearMRs()
	{
		meshRenderers = new MeshRenderer[0];
	}
}
