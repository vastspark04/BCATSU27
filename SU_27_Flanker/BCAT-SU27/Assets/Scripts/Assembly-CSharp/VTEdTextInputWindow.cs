using System;
using UnityEngine;
using UnityEngine.UI;

public class VTEdTextInputWindow : MonoBehaviour
{
	public VTScenarioEditor editor;

	public InputField inputField;

	public Text titleText;

	public Text descriptionText;

	public Text charCountText;

	private Action<string> onEntered;

	private string originalText;

	private Action onCancelled;

	private void Awake()
	{
		inputField.onValueChanged.AddListener(OnTextChanged);
	}

	private void OnTextChanged(string s)
	{
		if ((bool)charCountText && charCountText.gameObject.activeSelf)
		{
			charCountText.text = $"{s.Length}/{inputField.characterLimit}";
		}
	}

	private void Update()
	{
		if (!inputField.isFocused && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)))
		{
			ApplyChange();
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Cancel();
		}
	}

	public void ApplyChange()
	{
		if (onEntered != null)
		{
			onEntered(ConfigNodeUtils.SanitizeInputString(inputField.text));
		}
		Close();
	}

	public void Cancel()
	{
		if (onCancelled != null)
		{
			onCancelled();
		}
		else if (onEntered != null)
		{
			onEntered(originalText);
		}
		Close();
	}

	private void Close()
	{
		base.gameObject.SetActive(value: false);
		if ((bool)editor)
		{
			editor.UnblockEditor(base.transform);
			editor.editorCamera.inputLock.RemoveLock("textInput");
		}
	}

	public void Display(string title, string description, string currentText, int charLimit, Action<string> onEntered, Action onCancelled = null)
	{
		this.onEntered = onEntered;
		this.onCancelled = onCancelled;
		titleText.text = title;
		descriptionText.text = description;
		inputField.characterLimit = charLimit;
		inputField.text = currentText;
		originalText = currentText;
		base.gameObject.SetActive(value: true);
		base.transform.SetAsLastSibling();
		if ((bool)editor)
		{
			editor.BlockEditor(base.transform);
			editor.editorCamera.inputLock.AddLock("textInput");
		}
		inputField.SelectAllText();
		if ((bool)charCountText)
		{
			charCountText.gameObject.SetActive(charLimit > 0);
		}
	}
}
