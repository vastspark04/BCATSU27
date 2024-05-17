using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class DisableProbesInChildren : MonoBehaviour
{
	public LightProbeUsage lightProbes;

	public ReflectionProbeUsage reflectionProbes;

	public MotionVectorGenerationMode motionVectors;

	public bool apply;

	private void Update()
	{
		if (apply)
		{
			apply = false;
			MeshRenderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer obj in componentsInChildren)
			{
				obj.lightProbeUsage = lightProbes;
				obj.reflectionProbeUsage = reflectionProbes;
				obj.motionVectorGenerationMode = motionVectors;
			}
		}
	}
}
