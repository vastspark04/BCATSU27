using UnityEngine;

public class EMUI_Animations : MonoBehaviour
{
	public void ToggleFadeIn()
	{
		Animator component = GetComponent<Animator>();
		bool value = ((!component.GetBool("fadeIn")) ? true : false);
		component.SetBool("fadeIn", value);
	}
}
