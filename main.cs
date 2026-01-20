using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using static MilkAMilky.MilkAMilky;
using static MilkAMilky.SharedState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Runtime.CompilerServices;

namespace MilkAMilky
{
    public static class SharedState
    {
        public static Dictionary<TraderScript, MilkController> traderMilkies = new Dictionary<TraderScript, MilkController>();
        public static bool isTransferring = false;
        public static MilkController transferringController;

        public static string ParseRegenToTime(float time, float goal, float current)
        {
            float currentTime = (goal - current) / time;
            int minutes = Mathf.CeilToInt(currentTime / 60 / 5) * 5;
            return minutes + " minutes";
        }

        // turn into a locale patcher?
        public static class MilkyLines
        {
            public static string[] MilkHostile = { "Stop asking me to piss in that. Get out", "I'm not going to let you use me for my milk", "I don't even know you, and I don't want to anymore", "I don't want to see you or your containers ever again", "I'm not going to stand for this sexual harrassment", "You are lucky I'm not killing you where you stand", "Your inappropriate demands end here" };

            public static string[] MilkFail = { "What is wrong with you...", "I'm not going to do that", "Thats a weird question", "Theres plenty more to me than just milk", "I'm not going to piss in that...", "Wh- You know what I won't question it", "Is this a hobby of yours or something?", "I don't care if it's technically the same liquid, it's weird" };

            public static string[] MilkSuccess = { "T-thanks...", "I-I hope you enjoy it", "T-This is kind of embarrassing...", "Why was that even a thought?", "What are you going to do with it anyways...", "I'm only doing this because I like you", "How does it taste?", "Thanks! I've been needing that", "Anything for my bestest bud. Even... This..." };

            public static string[] MilkEmpty = { "You literally just got some", "I have no more left", "Thats all I had", "I can only give so much", "You'll have to wait about... <time>", "You'll have to ask another time" };

            public static string[] DrinkAccept = { "Yummy!", "Thanks for the drink!", "Oh, my favorite snack!", "I feel likke a w-aateeer.... -bballoon right now...", "G-gwrph >//<", "Okayyyy, no-ppp-ee. Tha-aa-t-tt's the limit...", "I'm feeling... Heavy...", "T-hha-tts a.... l-little too much" };

            public static string[] DrinkDeny = { "I'm not drinking that...", "What is even in that...", "Why should I trust you?", "Do you just drink random things off the ground?", "What is it, a drug?", "I'll stick to water, thanks..." };

            public static string[] Full = { "I-I can't take any more", "It's too much... P-please stop...", "My stomach can't handle another sip...", "This is torture...", "T-too ffull...", "Please..." };

            public static string[,] ExpieMilkyConvo = { { "Good little milky...", "T-thats unfair..." }, { "Who's my good booooyyy", "MEME MEM E MEM M EMEM EM EMEM" }, { "*click*", "*cums*" } };
        }
    }

    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class MilkAMilky : BaseUnityPlugin
    {
        public static ManualLogSource logger;
        public const string pluginGuid = "shushu.casualtiesunknown.milkamilky";
        public const string pluginName = "Milk A Milky";
        public const string pluginVersion = "25.12.1.0";

        public static MilkAMilky Instance;

        public void Awake()
        {
            Instance = this;
            logger = Logger;

            logger.LogInfo("Awake() ran - mod loaded!");

            Harmony harmony = new Harmony(pluginGuid);

            var PlayerCamera_TryPerformWorldActions_MyPatchOriginal = AccessTools.Method(typeof(PlayerCamera), "TryPerformWorldActions");
            var PlayerCamera_TryPerformWorldActions_MyPatchPre = typeof(MyPatches).GetMethod("PlayerCamera_TryPerformWorldActions_MyPatch");

            harmony.Patch(PlayerCamera_TryPerformWorldActions_MyPatchOriginal, prefix: new HarmonyMethod(PlayerCamera_TryPerformWorldActions_MyPatchPre));
            Log("Patched PlayerCamera_TryPerformWorldActions_MyPatch");

            var TraderScript_Awake_MyPatchesOriginal = AccessTools.Method(typeof(TraderScript), "Awake");
            var TraderScript_Awake_MyPatchesPost = typeof(MyPatches).GetMethod("TraderScript_Awake_MyPatches");

            harmony.Patch(TraderScript_Awake_MyPatchesOriginal, postfix: new HarmonyMethod(TraderScript_Awake_MyPatchesPost));
            Log("Patched TraderScript_Awake_MyPatches");

            var TraderScript_OnWillRenderObject_MyPatchesOriginal = AccessTools.Method(typeof(TraderScript), "OnWillRenderObject");
            var TraderScript_OnWillRenderObject_MyPatchesPost = typeof(MyPatches).GetMethod("TraderScript_OnWillRenderObject_MyPatches");

            harmony.Patch(TraderScript_OnWillRenderObject_MyPatchesOriginal, prefix: new HarmonyMethod(TraderScript_OnWillRenderObject_MyPatchesPost));
            Log("Patched TraderScript_OnWillRenderObject_MyPatches");

            var LiquidTransfer_Finish_MyPatchesOriginal = AccessTools.Method(typeof(LiquidTransfer), "Finish");
            var LiquidTransfer_Finish_MyPatchesPost = typeof(MyPatches).GetMethod("LiquidTransfer_Finish_MyPatches");

            harmony.Patch(LiquidTransfer_Finish_MyPatchesOriginal, prefix: new HarmonyMethod(LiquidTransfer_Finish_MyPatchesPost));
            Log("Patched LiquidTransfer_Finish_MyPatches");

            var LiquidTransfer_OnDestroy_MyPatchesOriginal = AccessTools.Method(typeof(LiquidTransfer), "OnDestroy");
            var LiquidTransfer_OnDestroy_MyPatchesPost = typeof(MyPatches).GetMethod("LiquidTransfer_OnDestroy_MyPatches");

            harmony.Patch(LiquidTransfer_OnDestroy_MyPatchesOriginal, prefix: new HarmonyMethod(LiquidTransfer_OnDestroy_MyPatchesPost));
            Log("Patched LiquidTransfer_OnDestroy_MyPatches");
        }

        public static void Log(string message)
        {
            logger.LogInfo(message);
        }

        public static IEnumerator InitMilkContainerNextFrame(TraderScript __instance)
        {
            yield return null;
            Log(__instance.character.ToString());
            if (__instance.character == 1)
            {
                GameObject milkObj = new GameObject("MilkyHiddenContainer");
                milkObj.transform.SetParent(__instance.transform, false);

                Item item = milkObj.AddComponent<Item>();
                item.id = "minibarrel";
                WaterContainerItem water = milkObj.AddComponent<WaterContainerItem>();

                var milkyMilkStats = new MilkController
                {
                    container = water,
                    effectiveCapacity = UnityEngine.Random.Range(300f, 500f),
                    regenPerSecond = UnityEngine.Random.Range(0.5f, 1f) * (UnityEngine.Random.Range(-10f, 10f) * 0.34f + 50f) / 60 / 60,
                    hasBeenMilked = false,
                    sapDrinkCount = 0,
                    sapDrinkMax = UnityEngine.Random.Range(1, 5),
                    sapInSystem = 0
                };

                water.AddLiquid("milk", UnityEngine.Random.Range(0f, milkyMilkStats.effectiveCapacity));
                milkObj.SetActive(false);

                traderMilkies[__instance] = milkyMilkStats;
                Log(traderMilkies[__instance].ToString());
            }
        }

        public static IEnumerator RegenMilkyMilk(TraderScript __instance)
        {
            var oldInstance = __instance;
            if (!traderMilkies.ContainsKey(__instance)) yield break;
            while (true)
            {
                if (!__instance)
                {
                    traderMilkies.Remove(oldInstance);
                    yield break;
                }
                yield return new WaitForSeconds(1f);
                if (!traderMilkies.ContainsKey(__instance)) yield break;
                var regenRate = traderMilkies[__instance].regenPerSecond * (traderMilkies[__instance].sapInSystem > 0 ? 10 : 1);
                traderMilkies[__instance].sapInSystem = Mathf.Max(0, traderMilkies[__instance].sapInSystem - UnityEngine.Random.Range(1, 4));
                traderMilkies[__instance].container.AddLiquid("milk", regenRate);
            }
        }

        public static IEnumerator ExpieTalksFirst(PlayerCamera __instance, TraderScript trader, String ExpieText, String MilkyText, float WaitTime)
        {
            __instance.body.talker.Talk(ExpieText, null, false, false);
            yield return new WaitForSeconds(WaitTime);
            trader.talker.Talk(MilkyText, null, false, false);
        }
    }

    public class MilkController
    {
        public WaterContainerItem container;
        public float effectiveCapacity;
        public float regenPerSecond;
        public bool hasBeenMilked;
        public int sapDrinkCount;
        public int sapDrinkMax;
        public float sapInSystem;
    }

    public class MilkCoroutineMarker : MonoBehaviour
    { }

    public class MyPatches
    {
        [HarmonyPatch(typeof(TraderScript))]
        [HarmonyPatch("OnWillRenderObject")]
        [HarmonyPrefix]
        public static void TraderScript_OnWillRenderObject_MyPatches(TraderScript __instance)
        {
            if (__instance == null || !traderMilkies.ContainsKey(__instance))
                return;

            if (__instance.gameObject.GetComponent<MilkCoroutineMarker>() == null)
            {
                __instance.gameObject.AddComponent<MilkCoroutineMarker>();
                MilkAMilky.Instance.StartCoroutine(RegenMilkyMilk(__instance));
            }
        }

        [HarmonyPatch(typeof(TraderScript))]
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void TraderScript_Awake_MyPatches(TraderScript __instance)
        {
            MilkAMilky.Instance.StartCoroutine(InitMilkContainerNextFrame(__instance));
        }

        [HarmonyPatch(typeof(PlayerCamera))]
        [HarmonyPatch("TryPerformWorldActions")]
        [HarmonyPrefix]
        public static bool PlayerCamera_TryPerformWorldActions_MyPatch(PlayerCamera __instance)
        {
            if (!__instance.currentTrader)
                return true;
            if (__instance.currentTrader.character != 1)
                return true;
            if (!(__instance.dragItem is Item && traderMilkies.ContainsKey(__instance.currentTrader) && Vector3.Distance(__instance.dragImage.transform.position, Camera.main.WorldToScreenPoint(__instance.currentTrader.transform.position + new Vector3(0, 2.85f, 0))) < 65f))
                return true;
            TraderScript trader = __instance.currentTrader;
            Log("Container dropped on milky, opening milk transfer menu...");
            if (__instance.dragItem.TryGetComponent<WaterContainerItem>(out WaterContainerItem wat1) && traderMilkies.TryGetValue(trader, out MilkController controller))
            {
                // !!!!!Suggestions:
                //     custom milk liquid
                //         expie is happier after drinking
                //         filling
                //         normal hunger/liquid increase
                //         color is rgba(187, 187, 187, 255)
                if (wat1.stack.Count == 1 && wat1.stack.First().liquidId == "sap")
                {
                    if (trader.reputation >= 100)
                    {
                        if (controller.sapDrinkCount >= controller.sapDrinkMax)
                        {
                            trader.talker.Talk(MilkyLines.Full[UnityEngine.Random.Range(0, MilkyLines.Full.Length)], null, false, false);
                            return false;
                        }
                        controller.sapDrinkCount++;
                        List<float> wat1Stack = wat1.CalculateDrain(100f);
                        wat1.Drain(wat1Stack);
                        controller.container.AddLiquid("milk", 100);
                        controller.sapInSystem += wat1Stack.First();
                        Sound.Play("drink", trader.transform.position, false, true, null, 1f, 1f, false, false);
                        trader.talker.Talk(MilkyLines.DrinkAccept[UnityEngine.Random.Range(0, MilkyLines.DrinkAccept.Length)], null, false, false);
                    }
                    else
                    {
                        trader.talker.Talk(MilkyLines.DrinkDeny[UnityEngine.Random.Range(0, MilkyLines.DrinkDeny.Length)], null, false, false);
                    }
                    return false;
                }

                if (trader.reputation >= 140)
                {
                    isTransferring = true;
                    transferringController = controller;
                    if (controller.container.CurrentTotal > 10f)
                    {
                        __instance.StartLiquidTransfer(wat1, controller.container);
                    }
                    else
                    {
                        trader.talker.Talk(MilkyLines.MilkEmpty[UnityEngine.Random.Range(0, MilkyLines.MilkEmpty.Length)].Replace("<time>", ParseRegenToTime(controller.regenPerSecond, 10, controller.container.CurrentTotal)), null, false, false);
                    }
                }
                else
                {
                    trader.reputation -= Mathf.Max(UnityEngine.Random.Range(10, 25), 0);
                    __instance.body.happiness -= 2.5f;
                    __instance.body.SetVelocity((__instance.body.transform.position - trader.torso.transform.position).normalized * 3f);
                    __instance.body.Ragdoll();
                    Sound.Play("BSSwing1", trader.transform.position, false, true, null, 1f, 1f, false, false);
                    trader.UpdateScreen();
                    if (trader.reputation <= 50f)
                    {
                        trader.reputation = 0f;
                        trader.talker.Talk(MilkyLines.MilkHostile[UnityEngine.Random.Range(0, MilkyLines.MilkHostile.Length)], null, false, false);

                        GameObject gameObject = UnityEngine.Object.Instantiate(Resources.Load("ItemBreakParticle"), __instance.dragItem.transform.position, __instance.dragItem.transform.rotation) as GameObject;
                        ParticleSystem.ShapeModule shape = gameObject.GetComponent<ParticleSystem>().shape;
                        shape.texture = __instance.dragItem.GetComponent<SpriteRenderer>().sprite.texture;
                        shape.sprite = __instance.dragItem.GetComponent<SpriteRenderer>().sprite;
                        gameObject.GetComponent<ParticleSystem>().Play();
                        UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("DustMini"), __instance.dragItem.transform.position, Quaternion.identity);
                        UnityEngine.Object.Destroy(__instance.dragItem.gameObject);
                        Sound.Play("glassshard", __instance.transform.position, false, true, null, 1f, 1f, false, false);
                        __instance.body.DoGoreSound();

                        int throwLimbTarget = UnityEngine.Random.Range(0, 10);
                        Limb limb = __instance.body.limbs[throwLimbTarget];

                        float armorReduction = limb.GetArmorReduction();
                        Log(limb.name);

                        if (limb.name == "Head")
                            __instance.body.brainHealth -= 15f;
                        float bleedAmount = UnityEngine.Random.Range(25f, 45f) / armorReduction;
                        float adjacentBleedAmount = bleedAmount / 2.3f;
                        Log("bleedAmount: " + bleedAmount.ToString());
                        Log("adjacentBleedAmount: " + adjacentBleedAmount.ToString());
                        limb.bleedAmount += bleedAmount;
                        limb.muscleHealth -= limb.bleedAmount / 2;
                        limb.skinHealth -= limb.bleedAmount / 1.5f;
                        if (!limb.hasShrapnel)
                            limb.shrapnel = 5;
                        foreach (var connectedLimb in limb.connectedLimbs)
                        {
                            connectedLimb.bleedAmount += adjacentBleedAmount;
                            connectedLimb.muscleHealth -= connectedLimb.bleedAmount / 2;
                            connectedLimb.skinHealth -= connectedLimb.bleedAmount / 1.5f;
                            if (!connectedLimb.hasShrapnel)
                                connectedLimb.shrapnel = 2;
                        }
                    }
                    else
                    {
                        trader.talker.Talk(MilkyLines.MilkFail[UnityEngine.Random.Range(0, MilkyLines.MilkFail.Length)], null, false, false);
                    }
                }
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(LiquidTransfer))]
        [HarmonyPatch("Finish")]
        [HarmonyPrefix]
        public static void LiquidTransfer_Finish_MyPatches(LiquidTransfer __instance)
        {
            if (transferringController != null)
            {
                if (__instance.ml > 25 && !transferringController.hasBeenMilked)
                {
                    PlayerCamera.main.body.happiness += 10f;
                    transferringController.hasBeenMilked = true;
                }
                if (UnityEngine.Random.Range(0, MilkyLines.MilkSuccess.Length + MilkyLines.ExpieMilkyConvo.GetLength(0)) >= MilkyLines.ExpieMilkyConvo.GetLength(0))
                {
                    int index = UnityEngine.Random.Range(0, MilkyLines.ExpieMilkyConvo.GetLength(0));
                    MilkAMilky.Instance.StartCoroutine(ExpieTalksFirst(PlayerCamera.main, PlayerCamera.main.currentTrader, MilkyLines.ExpieMilkyConvo[index, 0], MilkyLines.ExpieMilkyConvo[index, 1], 1.5f));
                }
                else
                    PlayerCamera.main.currentTrader.talker.Talk(MilkyLines.MilkSuccess[UnityEngine.Random.Range(0, MilkyLines.MilkSuccess.Length)], null, false, false);
            }
        }

        [HarmonyPatch(typeof(LiquidTransfer))]
        [HarmonyPatch("OnDestroy")]
        [HarmonyPrefix]
        public static void LiquidTransfer_OnDestroy_MyPatches(LiquidTransfer __instance)
        {
            transferringController = null;
            isTransferring = false;
        }
    }
}