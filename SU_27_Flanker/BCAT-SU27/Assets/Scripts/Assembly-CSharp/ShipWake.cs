using UnityEngine;

public class ShipWake : MonoBehaviour
{
	public ShipMover shipMover;

	public Transform wakeTransform;

	public LineRenderer wakeRenderer;

	public TrailParticles trailParticles;

	public float worldToWakeSpeed;

	public Color fullSpeedColor;

	private Color noSpeedColor;

	public AnimationCurve alphaCurve;

	private MaterialPropertyBlock matProps;

	private float lineWidth;

	private float tOffset;

	private int tOffsetID;

	private void Start()
	{
		if (!shipMover)
		{
			shipMover = GetComponentInParent<ShipMover>();
		}
		tOffsetID = Shader.PropertyToID("_TextureOffset");
		matProps = new MaterialPropertyBlock();
		noSpeedColor = fullSpeedColor;
		noSpeedColor.a = 0f;
		lineWidth = wakeRenderer.widthMultiplier;
		Vector3 localPosition = base.transform.localPosition;
		localPosition.y = 0f;
		base.transform.localPosition = localPosition;
		wakeRenderer.textureMode = LineTextureMode.Stretch;
	}

	private void Update()
	{
		float magnitude = shipMover.rb.velocity.magnitude;
		tOffset += worldToWakeSpeed * magnitude * Time.deltaTime;
		matProps.SetFloat(tOffsetID, tOffset);
		float time = magnitude / shipMover.maxSpeed;
		time = alphaCurve.Evaluate(time);
		wakeRenderer.SetPropertyBlock(matProps);
		trailParticles.startColor = Color.Lerp(noSpeedColor, fullSpeedColor, time);
	}
}
