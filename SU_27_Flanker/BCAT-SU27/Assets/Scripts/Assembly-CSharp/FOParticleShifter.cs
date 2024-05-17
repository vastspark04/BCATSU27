using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public class FOParticleShifter : MonoBehaviour
{
	private ParticleSystem ps;

	private static ParticleSystem.Particle[] particleBuffer;

	private static Transform worldSimSpace;

	private void Start()
	{
		ps = GetComponent<ParticleSystem>();
		if (!worldSimSpace)
		{
			worldSimSpace = new GameObject("Particle Sim Space").transform;
			worldSimSpace.transform.position = Vector3.zero;
			if ((bool)VTCustomMapManager.instance)
			{
				float num = VTCustomMapManager.instance.mapGenerator.chunkSize * (float)VTCustomMapManager.instance.mapGenerator.gridSize;
				Vector3D globalPoint = new Vector3D(num / 2f, 2000.0, num / 2f);
				worldSimSpace.transform.position = VTMapManager.GlobalToWorldPoint(globalPoint);
			}
			worldSimSpace.gameObject.AddComponent<FloatingOriginTransform>();
		}
		ParticleSystem.MainModule main = ps.main;
		main.simulationSpace = ParticleSystemSimulationSpace.Custom;
		main.customSimulationSpace = worldSimSpace;
	}
}
