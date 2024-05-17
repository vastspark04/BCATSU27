using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

public class DictationRecognizerTest : MonoBehaviour
{
	private Text text;

	private DictationRecognizer d;

	private void Start()
	{
		text = GetComponent<Text>();
		d = new DictationRecognizer();
		d.DictationHypothesis += D_DictationHypothesis;
		d.DictationResult += D_DictationResult;
		d.Start();
	}

	private void D_DictationResult(string text, ConfidenceLevel confidence)
	{
		this.text.text = text + "\n" + confidence;
		this.text.color = Color.green;
	}

	private void D_DictationHypothesis(string text)
	{
		this.text.color = Color.white;
		this.text.text = text;
	}

	private void OnDestroy()
	{
		d.Stop();
		d.Dispose();
	}
}
