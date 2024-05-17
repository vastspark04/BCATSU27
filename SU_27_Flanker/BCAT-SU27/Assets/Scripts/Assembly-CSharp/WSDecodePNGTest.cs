using UnityEngine;
using UnityEngine.UI;

public class WSDecodePNGTest : MonoBehaviour
{
	public RawImage rawImage;

	public string imgPath;

	private void Start()
	{
		Texture2D texture = VTResources.GetTexture(imgPath);
		rawImage.texture = texture;
	}

	private void Update()
	{
	}
}
