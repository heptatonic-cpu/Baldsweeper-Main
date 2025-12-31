
using System;
using HarmonyLib;
using MTM101BaldAPI.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BALDsweeper
{
	[HarmonyPatch]
	public class Patch
	{
		[HarmonyPatch(typeof(MainMenu), "Start")]
		[HarmonyPrefix]
		static void Postfix(MainMenu __instance)
		{
			Image image = UIHelpers.CreateImage(BasePlugin.Assets.Get<Sprite>("ms_unlit"), __instance.transform, Vector3.zero, false, 1f);
			image.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			image.rectTransform.anchoredPosition = new Vector2(60f, -88f);
			StandardMenuButton standardMenuButton = image.gameObject.ConvertToButton<StandardMenuButton>(true);
			standardMenuButton.highlightedSprite = BasePlugin.Assets.Get<Sprite>("ms_lit");
			standardMenuButton.unhighlightedSprite = BasePlugin.Assets.Get<Sprite>("ms_unlit");
			standardMenuButton.swapOnHigh = true;
			standardMenuButton.transitionOnPress = true;
			standardMenuButton.transitionTime = 0.0167f;
			standardMenuButton.transitionType = UiTransition.Dither;
			standardMenuButton.OnPress.AddListener(delegate()
			{
				Singleton<GlobalCam>.Instance.Transition(UiTransition.Dither, 0.01666667f);
				BasePlugin.Instance.StartCoroutine(BasePlugin.Instance.LoadGame());
			});
			CursorController.Instance.transform.SetAsLastSibling();
			__instance.transform.Find("Bottom").SetAsLastSibling();
			__instance.transform.Find("BlackCover").SetAsLastSibling();
		}
	}
}
