using System;
using System.Collections.Generic;
using UnityEngine;

public class ParticlesOnPoints : MonoBehaviour
{
	[Serializable]
	public class ParticleGroup
	{
		public Color color;

		public float size;

		public bool sizeByBrightness = true;

		public List<Transform> positions;
	}

	public ParticleSystem ps;

	public ParticleGroup[] particleGroups;

	public bool spawnOnEnable = true;

	private void OnEnable()
	{
		if (spawnOnEnable)
		{
			SpawnParticles();
		}
	}

	[ContextMenu("Spawn Particles")]
	public void SpawnParticles()
	{
		ps.Stop();
		ps.Clear();
		int num = 0;
		ParticleGroup[] array = particleGroups;
		foreach (ParticleGroup particleGroup in array)
		{
			particleGroup.positions.RemoveAll((Transform x) => x == null);
			num += particleGroup.positions.Count;
		}
		ParticleSystem.MainModule main = ps.main;
		main.maxParticles = num;
		ps.Play();
		int num2 = 0;
		array = particleGroups;
		foreach (ParticleGroup particleGroup2 in array)
		{
			foreach (Transform position in particleGroup2.positions)
			{
				ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
				emitParams.startColor = particleGroup2.color;
				emitParams.startSize = particleGroup2.size;
				emitParams.position = ps.transform.InverseTransformPoint(position.position);
				emitParams.startLifetime = float.MaxValue;
				ps.Emit(emitParams, 1);
				num2++;
			}
		}
	}
}
