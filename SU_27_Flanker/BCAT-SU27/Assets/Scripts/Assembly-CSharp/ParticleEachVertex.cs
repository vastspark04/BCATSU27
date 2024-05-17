using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleEachVertex : MonoBehaviour
{
	public bool emit = true;

	private float lastEmitTime;

	private ParticleSystem ps;

	private Mesh mesh;

	private Vector3[] verts;

	private Vector3[] norms;

	private void Start()
	{
		ps = GetComponent<ParticleSystem>();
	}

	private void Update()
	{
		if (emit)
		{
			Emit();
		}
		else if (!ps.isStopped)
		{
			ps.Stop();
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (!ps)
		{
			ps = GetComponent<ParticleSystem>();
		}
		if (emit)
		{
			Emit();
		}
	}

	private void Emit()
	{
		if (!(Time.time - lastEmitTime > ps.main.duration))
		{
			return;
		}
		if (!ps.isPlaying)
		{
			ps.Play();
		}
		lastEmitTime = Time.time;
		if (!mesh)
		{
			mesh = ps.shape.mesh;
			if ((bool)mesh)
			{
				verts = mesh.vertices;
				norms = mesh.normals;
			}
		}
		if ((bool)mesh)
		{
			for (int i = 0; i < verts.Length; i++)
			{
				ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
				emitParams.position = verts[i] + ps.shape.normalOffset * norms[i];
				ps.Emit(emitParams, 1);
			}
		}
	}
}
