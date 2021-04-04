using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

using UnityEngine;
//using UnityEngine.SceneManagement;

using AIChara;
using CharaUtils;

namespace HS2_ExpressionLinked2Feel
{
    class HS2_ExpressionLinked2Feel
    {
        public static HScene hScene;
        public static HSceneFlagCtrl hFlagCtrl;

        private static HSceneSprite hSprite;

        public static List<ChaControl> characters;
        public static List<ChaControl> maleCharacters;
        public static List<ChaControl> femaleCharacters;


        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartAnimationInfo")]
        public static void HScene_SetStartAnimationInfo_Patch(HScene __instance, HSceneSprite ___sprite)
        {
            hScene = __instance;
            hSprite = ___sprite;

            if (hScene == null || hSprite == null)
                return;

            hFlagCtrl = hScene.ctrlFlag;
            if (hFlagCtrl == null)
                return;

            characters = new List<ChaControl>();
            maleCharacters = new List<ChaControl>();
            ChaControl[] males = __instance.GetMales();
            foreach (var male in males.Where(male => male != null))
            {
                maleCharacters.Add(male);
                characters.Add(male);
            }

            femaleCharacters = new List<ChaControl>();
            ChaControl[] females = __instance.GetFemales();
            foreach (var female in females.Where(female => female != null))
            {
                femaleCharacters.Add(female);
                characters.Add(female);
            }

            if (characters == null)
                return;

            foreach (var character in characters.Where(character => character != null))
            {
                Expression expression = character.GetComponent<Expression>();
                if (expression != null)
                    continue;

                expression = character.gameObject.AddComponent<Expression>();
                expression.SetCharaTransform(character.transform);
                expression.LoadSetting("list/expression.unity3d", "cf_expression");
                expression.Initialize();
            }

        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetShapeBodyValue))]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetShapeFaceValue))]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ResetDynamicBoneAll))]
        public static void ChangeValuePostfix(ChaControl __instance)
        {
            ChangeLocalRotationn();
        }

        static bool forceChangeEye = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Transform))]
        [HarmonyPatch("localRotation", MethodType.Setter)]
        public static bool LocalRotationSetterPrefix(Transform __instance)
        {
            if (__instance.name.Equals("cf_J_EyePos_rz_L") || __instance.name.Equals("cf_J_EyePos_rz_R"))
            {
                if (forceChangeEye)
                {
                    forceChangeEye = false;
                    return true;
                }
                else
                {
                    //たまに意図しないところからEyePosのSetterが呼び出されるので取り消す。
                    //(これはもっと上手いやりかたがあるかも
                    return false;
                }
            }
            return true;
        }
        public static void ChangeLocalRotationn()
        {
            if (femaleCharacters == null)
            {
                return;
            }
            if (femaleCharacters.Count < 1)
            {
                return;
            }
            if (hFlagCtrl == null)
            {
                return;
            }
            ChaControl female = femaleCharacters[0];

            female.eyesCtrl.OpenMax = 1.0f;// (1.0f - hFlagCtrl.feel_f) + 0.8f;
            female.eyesCtrl.OpenMin = 1.0f;
            female.eyesCtrl.SetOpenRateForce(1.0f);


            float shikii = 0.3f;

            float feel_f = hFlagCtrl.feel_f;

            Transform tfEyePosRzL = female.GetComponentsInChildren<Transform>
                    ().Where(x => x.name.Contains("cf_J_EyePos_rz_L")).FirstOrDefault();
            Transform tfEyePosRzR = female.GetComponentsInChildren<Transform>
                    ().Where(x => x.name.Contains("cf_J_EyePos_rz_R")).FirstOrDefault();

            if (shikii < feel_f)
            {
                //寄り目            //L x:-40 y:15 
                //離れ x-40 y-30

                //L z増やす -180 ~ 180

                float rx = Mathf.Lerp(0, -40, (feel_f - shikii) / (1 - shikii));
                float ry = Mathf.Lerp(0, 30, (feel_f - shikii) / (1 - shikii));

                float rotationZ = 0;

                forceChangeEye = true;
                tfEyePosRzL.localRotation = Quaternion.Euler(rx, ry, rotationZ);

                forceChangeEye = true;
                tfEyePosRzR.localRotation = Quaternion.Euler(rx, ry * -1, rotationZ * -1);

            }
            else
            {
                //TODO 初期化で戻したほうがいいかも
                tfEyePosRzL.localRotation = Quaternion.Euler(0, 0, 0);
                tfEyePosRzR.localRotation = Quaternion.Euler(0, 0 * -1, 0 * -1);

            }
        }
    }
}

