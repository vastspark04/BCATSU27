using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Harmony;
using UnityEngine.Networking;
using UnityEngine.UI;

public class JesterAI : MonoBehaviour
{
	public AudioClip[] badCarrierLanding;

	public AudioClip[] goodCarrierLanding;

    public AudioClip[] bolterLanding;

	[Header("Numbers")]
    public AudioClip Number_0;
    public AudioClip Number_1;
    public AudioClip Number_2;
    public AudioClip Number_3;
    public AudioClip Number_4;
    public AudioClip Number_5;
    public AudioClip Number_6;
    public AudioClip Number_7;
    public AudioClip Number_8;
    public AudioClip Number_9;
    public AudioClip Number_10;
    public AudioClip Number_11;
    public AudioClip Number_12;
    private void Awake()
	{
        GameObject go = VTOLAPI.GetPlayersVehicleGameObject();

        Debug.Log("Jester 0.0");
        GameObject radar = GetChildWithName(go, "Radar", true);
        

    }
    public static GameObject GetChildWithName(GameObject obj, string name, bool check)
    {

        //Debug.unityLogger.logEnabled = Main.logging;
        Transform[] children = obj.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (check)
            {
                //debug.log("Looking for:" + name + ", Found:" + child.name); 
            }
            if (child.name == name || child.name == (name + "(Clone)") || child.name.Contains(name))
            {
                return child.gameObject;
            }
        }


        return null;

    }
}
