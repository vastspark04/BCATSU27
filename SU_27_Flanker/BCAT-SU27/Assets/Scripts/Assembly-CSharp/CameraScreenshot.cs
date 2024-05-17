using System.IO;
using UnityEngine;

public class CameraScreenshot : MonoBehaviour
{
	public Camera cam;

	[ContextMenu("Take Screenshot")]
	public void Screenshot()
	{
		int num = 3840;
		RenderTexture temporary = RenderTexture.GetTemporary(num, num, 32);
		temporary.antiAliasing = 8;
		RenderTexture targetTexture = cam.targetTexture;
		bool flag = cam.enabled;
		cam.enabled = false;
		cam.targetTexture = temporary;
		cam.Render();
		cam.targetTexture = targetTexture;
		cam.enabled = flag;
		int num2 = num;
		Texture2D texture2D = new Texture2D(num, num2, TextureFormat.RGB24, mipChain: false);
		RenderTexture.active = temporary;
		texture2D.ReadPixels(new Rect(0f, (num - num2) / 2, num, num2), 0, 0);
		RenderTexture.ReleaseTemporary(temporary);
		string dir = Path.Combine(VTResources.gameRootDirectory, "Screenshots");
		string newScreenshotFilepath = GetNewScreenshotFilepath(dir);
		byte[] bytes = texture2D.EncodeToPNG();
		File.WriteAllBytes(newScreenshotFilepath, bytes);
		Object.Destroy(texture2D);
	}

	private string GetNewScreenshotFilepath(string dir)
	{
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
		string text = ".png";
		string text2 = "screenshot";
		int num = 0;
		string path = text2 + num + text;
		string text3 = Path.Combine(dir, path);
		while (File.Exists(text3))
		{
			num++;
			path = text2 + num + text;
			text3 = Path.Combine(dir, path);
		}
		return text3;
	}
}
