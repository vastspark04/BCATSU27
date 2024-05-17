using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Unity;
using TMPro;
using UnityEngine.UI;

namespace CustomAircraftTemplateSU27
{
    public class Main : VTOLMOD
    {
        
        
        public static Main instance;

        //Stores a prefab of the aircraft in order to spawn it in whenever you want
        public static GameObject aircraftPrefab;
        
        public static PlayerVehicle customAircraftPV;
        public static BuiltInCampaigns customBICampaigns;
        public static GameObject aircraftLoadoutConfiguratorPrefab;
        public static GameObject DebugTools;
        public static GameObject aircraftCustom;
        public static GameObject BOQuad;
        public static Single currentGAlpha;
        public static GameObject customAircraftPVobject;
        public static bool checkPVListFull = false;
        //public static MultiplayerSpawn.Vehicles aircraftMSVId;
        public static int unitListCount = 1;
        public static GameObject vehicleObject;
        public static float flapssetting =1;
        

        public static int i=0;

        public static GameObject playerGameObject;
       
        public static string pathToBundle;
        private string pathToAddonsBundle;
        public static bool logging = true;
        public static List<Campaign> campaignslist;
        public static String unitList;
        
        public static bool aircraftLoaded = false;
        public static TextMeshPro radarcontactlist;
        public static GameObject miniicp;
       
        public static AssetBundle bundleLoad;
        public static Campaign customCampaign;
        public static Campaign steamCampaign;
        public static WheelsController wc;
        public static GearAnimator animator;

        public bool tgpset = false;
        public bool fiset = false;
        public bool conton = true;
        FlightInfo fi = null;
        GameObject go = null;
        float prevalt = 0f;


        // This method is run once, when the Mod Loader is done initialising this game object
        public override void ModLoaded()
        {
            
            //Debug.unityLogger.logEnabled = Main.logging;
            instance = this;

            //Debug.Log("F4PII ML3" + AircraftInfo.AircraftAssetbundleName);

            pathToBundle = Path.Combine(instance.ModFolder, AircraftInfo.AircraftAssetbundleName);
            

            //Debug.Log("F4PII ML3" + pathToBundle);
            
            
            HarmonyInstance harmonyInstance = HarmonyInstance.Create(AircraftInfo.HarmonyId);
            
            harmonyInstance.PatchAll();
           

            
            base.ModLoaded();


            
            
            
            //Debug.Log("F4PII ML6");
            VTResources.OnLoadingPlayerVehicles += AircraftAPI.VehicleAdd;

            AircraftAPI.VehicleAdd();

                           
             
            




        }


        //This method is called every frame by Unity. Here you'll probably put most of your code
        void Update()
        {
            if (go == null)
            {
                go = VTOLAPI.GetPlayersVehicleGameObject();
            }
            if (go != null)
            {
                if (!fiset)
                {
                    fi = go.GetComponent<FlightInfo>();
                    if (fi != null)
                    {
                        //Debug.Log("finotnull");
                        fiset = true;
                    }
                }
                else if (prevalt == fi.altitudeASL)
                {
                    tgpset = false;
                    fiset = false;
                    conton = true;
                }
                if (fiset)
                {
                    //Debug.Log("fielse");
                    if (!conton && fi.altitudeASL >= 9000f)
                    {
                        AircraftAPI.GetChildWithName(go, "portContrail", false).GetComponent<ParticleSystem>().SetEmissionRate(100f);
                        AircraftAPI.GetChildWithName(go, "starContrail", false).GetComponent<ParticleSystem>().SetEmissionRate(100f);
                        conton = true;
                    }
                    else if (conton && fi.altitudeASL < 9000f)
                    {
                        AircraftAPI.GetChildWithName(go, "portContrail", false).GetComponent<ParticleSystem>().SetEmissionRate(0f);
                        AircraftAPI.GetChildWithName(go, "starContrail", false).GetComponent<ParticleSystem>().SetEmissionRate(0f);
                        conton = false;
                        //Debug.Log("contrails");
                    }
                    prevalt = fi.altitudeASL;
                }

            }

        }


        //This method is like update but it's framerate independent. This means it gets called at a set time interval instead of every frame. This is useful for physics calculations
        void FixedUpdate()
        {
            

           
        }

        //This function is called every time a scene is loaded. this behaviour is defined in Awake().
        private void SceneLoaded(VTOLScenes scene)
        {
            //If you want something to happen in only one (or more) scenes, this is where you define it.
            
            //For example, lets say you're making a mod which only does something in the ready room and the loading scene. This is how your code could look:
            switch (scene)
            {
                
                case VTOLScenes.VehicleConfiguration:
                    //Debug.Log("F4PII Reload the configurator");
                    StartCoroutine(InitWaiter());
                    

                    break;
                default:
                    //Debug.Log("F4PII In scene: " + scene);
                    tgpset = false;
                    fiset = false;
                    conton = true;
                    fi = null;
                    go = null;
                    break;
                    break;

            }


        }

        private IEnumerator InitWaiter()
        {
        //Debug.unityLogger.logEnabled = Main.logging;
        //Debug.Log("F4PII InitWaiter Started");
            yield return new WaitForSeconds(1f);
          
            yield break;
        }

       
    }
    }
