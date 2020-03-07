using MSCLoader;
using UnityEngine;
using HutongGames.PlayMaker;

namespace ExtraBinds
{
    public class ExtraBinds : Mod
    {
        public override string ID => "ExtraBinds"; //Your mod ID (unique)
        public override string Name => "Extra Binds (Alpha)"; //You mod name
        public override string Author => "Athlon"; //Your Username
        public override string Version => "0.0.1"; //Version

        FsmString playerCurrentVehicle;

        // Look
        const float LookBackAngle = 150;
        Transform playerTransform;
        static float currentLookAngle;
        float vehicleDefaultLookAngle;
        
        bool isAnyLookKeyUp;

        // Ignition
        string currentIgnitionFsm;
        PlayMakerFSM ignitionFsm;
        SphereCollider ignitionSphereCollider;
        float defaultSphereRadius;
        float ignitionSphereValueToAdd;

        // Radio
        PlayMakerFSM radioKnobFsm;
        SphereCollider radioKnobSphereCollider;
        float defaultRadioSphereRadius;

        // Set this to true if you will be load custom assets from Assets folder.
        // This will create subfolder in Assets folder for your mod.
        public override bool UseAssetsFolder => false;

        // Called once, when mod is loading after game is fully loaded
        public override void OnLoad()
        {
            playerTransform = GameObject.Find("PLAYER").GetComponent<Transform>();
            playerCurrentVehicle = PlayMakerGlobals.Instance.Variables.FindFsmString("PlayerCurrentVehicle");
            UpdateLookAngleValue();

        }

        static Settings angle = new Settings("angle", "Angle", 50, UpdateLookAngleValue);

        Keybind lookLeft = new Keybind("lookLeft", "Look Left", KeyCode.Comma);
        Keybind lookRight = new Keybind("lookRight", "Look Right", KeyCode.Period);
        Keybind lookBack = new Keybind("lookBack", "Look Back", KeyCode.Slash);
        KeybindExtended ignition = new KeybindExtended("ignition", "Ignition", KeyCode.R);
        KeybindExtended radioVolumeUp = new KeybindExtended("radioUp", "Radio Volume Up", KeyCode.KeypadPlus);
        KeybindExtended radioVolumeDown = new KeybindExtended("radioDown", "Radio Volume Down", KeyCode.KeypadMinus);

        // All settings should be created here. 
        // DO NOT put anything else here that settings.
        public override void ModSettings()
        {
            Settings.AddSlider(this, angle, 20, 90);

            Keybind.Add(this, lookLeft);
            Keybind.Add(this, lookRight);
            Keybind.Add(this, lookBack);
            Keybind.Add(this, ignition);
            Keybind.Add(this, radioVolumeUp);
            Keybind.Add(this, radioVolumeDown);
        }

        // Update is called once per frame
        public override void Update()
        {
            // If player is not in any car, exit
            if (playerCurrentVehicle.Value.Length == 0)
            {
                // Reset radius when player decides to leave the car while holding the ignition
                if (ignitionSphereCollider)
                {
                    ignitionSphereCollider.radius = defaultSphereRadius;
                    ignitionSphereCollider = null;
                }

                currentIgnitionFsm = "";
                return;
            }

            UpdateIgnitionInfo();

            // RADIO
            if (radioKnobFsm != null)
            {
                if (radioVolumeDown.IsPressed())
                {
                    radioKnobSphereCollider.radius = defaultRadioSphereRadius + 0.6f;
                    radioKnobFsm.SendEvent("TIGHTEN");
                }
                
                if (radioVolumeUp.IsPressed())
                {
                    radioKnobSphereCollider.radius = defaultRadioSphereRadius + 0.6f;
                    radioKnobFsm.SendEvent("UNTIGHTEN");
                }

                if (radioVolumeUp.IsUp() || radioVolumeDown.IsUp())
                {
                    radioKnobSphereCollider.radius = defaultRadioSphereRadius;
                }
            }

            // IGNITION
            if (currentIgnitionFsm.Length > 0 && currentIgnitionFsm == playerCurrentVehicle.Value)
            {
                if (ignition.IsPressed())
                {
                    ignitionSphereCollider.radius = defaultSphereRadius + ignitionSphereValueToAdd;
                    ignitionFsm.SendEvent("ACC");
                }
                else if (ignition.IsUp())
                {
                    ignitionSphereCollider.radius = defaultSphereRadius;
                    ignitionFsm.SendEvent("FINISHED");
                }
            }

            // LOOKING
            // For some reason, Satsuma's player pivot is rotated 180 degrees to the car's front
            // y u do dis, toplessgun?
            vehicleDefaultLookAngle = playerCurrentVehicle.Value == "Satsuma" ? 180 : 0;

            if (lookLeft.IsPressed() || lookRight.IsPressed() || lookBack.IsPressed())
            {
                isAnyLookKeyUp = true;
                float lookAngle = currentLookAngle;
                
                if (lookLeft.IsPressed())
                    lookAngle *= -1;

                // Look back if lookBack button is pressed, or lookLeft and lookRight are pressed at the same time
                if (lookBack.IsPressed() || (lookLeft.IsPressed() && lookRight.IsPressed()))
                    lookAngle = LookBackAngle;

                SetPlayerAngle(lookAngle);
            }
            else if (isAnyLookKeyUp)
            {
                isAnyLookKeyUp = false;
                SetPlayerAngle(0);
            }    
        }

        /// <summary>
        /// Sets the player rotation angle
        /// </summary>
        /// <param name="value"></param>
        void SetPlayerAngle(float value)
        {
            playerTransform.localEulerAngles = new Vector3(playerTransform.localEulerAngles.x, vehicleDefaultLookAngle + value, playerTransform.localEulerAngles.z);
        }

        /// <summary>
        /// Updatates currentLookAngle
        /// </summary>
        static void UpdateLookAngleValue()
        {
            currentLookAngle = float.Parse(angle.GetValue().ToString());
        }

        /// <summary>
        /// Updates FSMs related to car ignition
        /// </summary>
        void UpdateIgnitionInfo()
        {
            if (currentIgnitionFsm == playerCurrentVehicle.Value)
                return;

            currentIgnitionFsm = playerCurrentVehicle.Value;

            switch (playerCurrentVehicle.Value)
            {
                default:
                    currentIgnitionFsm = "";
                    radioKnobFsm = null;
                    break;
                case "Satsuma":
                    ignitionFsm = GameObject.Find("SATSUMA(557kg, 248)").transform.Find("Dashboard/Steering/steering_column2/Ignition").gameObject.GetComponent<PlayMakerFSM>();

                    Transform cd = GameObject.Find("SATSUMA(557kg, 248)").transform.Find("Dashboard/pivot_dashboard/dashboard(Clone)/" +
                        "pivot_meters/dashboard meters(Clone)/pivot_radio/cd player(Clone)/ButtonsCD/RadioVolume");

                    if (cd != null)
                    {
                        radioKnobFsm = cd.gameObject.GetComponent<PlayMakerFSM>();
                    }
                    else
                    {
                        Transform oldRadio = GameObject.Find("SATSUMA(557kg, 248)").transform.Find("Dashboard/pivot_dashboard/dashboard(Clone)/" +
                            "pivot_meters/dashboard meters(Clone)/pivot_radio/radio(Clone)/ButtonsRadio/RadioVolume");
                        if (oldRadio != null)
                        {
                            radioKnobFsm = oldRadio.gameObject.GetComponent<PlayMakerFSM>();
                        }
                        else
                        {
                            radioKnobFsm = null;
                        }
                    }
                    break;
                case "Hayosiko":
                    ignitionFsm = GameObject.Find("HAYOSIKO(1500kg, 250)").transform.Find("LOD/Dashboard/Knobs/Ignition").gameObject.GetComponent<PlayMakerFSM>();
                    radioKnobFsm = GameObject.Find("HAYOSIKO(1500kg, 250)").transform.Find("RadioPivot/Radio/ButtonsRadio/RadioVolume").gameObject.GetComponent<PlayMakerFSM>();
                    break;
                case "Ruscko":
                    ignitionFsm = GameObject.Find("RCO_RUSCKO12(270)").transform.Find("LOD/Dashboard/Knobs/Ignition").gameObject.GetComponent<PlayMakerFSM>();
                    radioKnobFsm = null;
                    break;
                case "Kekmet":
                    ignitionFsm = GameObject.Find("KEKMET(350-400psi)").transform.Find("LOD/Dashboard/Ignition").gameObject.GetComponent<PlayMakerFSM>();
                    radioKnobFsm = GameObject.Find("KEKMET(350-400psi)").transform.Find("RadioPivot/Radio/ButtonsRadio/RadioVolume").gameObject.GetComponent<PlayMakerFSM>();
                    break;
                case "Gifu":
                    ignitionFsm = GameObject.Find("GIFU(750/450psi)").transform.Find("LOD/Dashboard/Ignition").gameObject.GetComponent<PlayMakerFSM>();
                    radioKnobFsm = GameObject.Find("GIFU(750/450psi)").transform.Find("RadioPivot/Radio/ButtonsRadio/RadioVolume").gameObject.GetComponent<PlayMakerFSM>();
                    break;
                case "Ferndale":
                    ignitionFsm = GameObject.Find("FERNDALE(1630kg)").transform.Find("LOD/Dashboard/Knobs/Ignition").gameObject.GetComponent<PlayMakerFSM>();
                    radioKnobFsm = GameObject.Find("FERNDALE(1630kg)").transform.Find("RadioPivot/Radio/ButtonsRadio/RadioVolume").gameObject.GetComponent<PlayMakerFSM>();
                    break;
            }

            // Ignition
            ignitionSphereCollider = ignitionFsm.gameObject.GetComponent<SphereCollider>();
            defaultSphereRadius = ignitionSphereCollider.radius;
            ignitionSphereValueToAdd = currentIgnitionFsm == "Satsuma" ? 20 : currentIgnitionFsm == "Ferndale" ? 15 : 0.5f;

            // Radio
            if (radioKnobFsm != null)
            {
                radioKnobSphereCollider = radioKnobFsm.gameObject.GetComponent<SphereCollider>();
                defaultRadioSphereRadius = radioKnobSphereCollider.radius;
            }
        }
    }
}
