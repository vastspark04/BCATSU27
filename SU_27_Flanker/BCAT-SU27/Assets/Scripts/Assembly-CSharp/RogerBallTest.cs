using UnityEngine;
using UnityEngine.Windows.Speech;

public class RogerBallTest : MonoBehaviour
{
	private KeywordRecognizer rec;

	private void Start()
	{
		Debug.Log("Call the ball.");
		rec = new KeywordRecognizer(new string[1] { "kilo one one kestrel ball" });
		rec.Start();
		rec.OnPhraseRecognized += delegate
		{
			Debug.Log("Roger ball!");
			rec.Dispose();
		};
	}
}
