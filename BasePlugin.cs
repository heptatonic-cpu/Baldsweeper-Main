
using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using MTM101BaldAPI;
using MTM101BaldAPI.UI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.AssetTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;

namespace BALDsweeper
{
	[BepInPlugin("heptatonic.bbplus.baldsweeper", "BALDsweeper", "1.0.0")]
	public class BasePlugin : BaseUnityPlugin
	{
		public static BasePlugin Instance { get; internal set; }
		public static AssetManager Assets { get; internal set; }
		
		public static Shader tileStandardShader
		{
			get
			{
				return BasePlugin.Assets.Get<Shader>("Shader Graphs/TileStandard");
			}
		}
		
		
		void Awake()
		{
			Instance = this;
			Assets = new AssetManager();
			
			Harmony harmony = new Harmony("heptatonic.bbplus.baldsweeper");
			harmony.PatchAllConditionals();
			
			LoadingEvents.RegisterOnAssetsLoaded(base.Info, PreloadAssets(), LoadingEventOrder.Pre);
		}
		
		IEnumerator PreloadAssets()
		{
			yield return 3;
			yield return "Preloading assets...";
			Assets.AddFromResources<Mesh>();
			Assets.AddFromResources<Shader>();
			Assets.AddFromResources<SoundObject>();
			Assets.Add<Sprite>("ms_dexneutral", AssetLoader.SpriteFromMod(this, new Vector3(0.5f, 0.5f), 40f, Path.Combine("Sprites", "dexNeutral.png")));
			Assets.Add<Sprite>("ms_dexsurprised", AssetLoader.SpriteFromMod(this, new Vector3(0.5f, 0.5f), 40f, Path.Combine("Sprites", "dexSurprised.png")));
			Assets.Add<Sprite>("ms_dexgameover", AssetLoader.SpriteFromMod(this, new Vector3(0.5f, 0.5f), 40f, Path.Combine("Sprites", "dexgameover.png")));
			Assets.Add<Sprite>("ms_dexcool", AssetLoader.SpriteFromMod(this, new Vector3(0.5f, 0.5f), 40f, Path.Combine("Sprites", "dexCool.png")));
			Assets.Add<Sprite>("ms_unlit", AssetLoader.SpriteFromMod(this, new Vector3(0.5f, 0.5f), 40f, Path.Combine("Sprites", "Minesweeper_Unlit.png")));
			Assets.Add<Sprite>("ms_lit", AssetLoader.SpriteFromMod(this, new Vector3(0.5f, 0.5f), 40f, Path.Combine("Sprites", "Minesweeper_Lit.png")));
			Assets.Add<Texture2D>("tile_unk", AssetLoader.TextureFromMod(this, Path.Combine("Sprites", "TileUnknown.png")));
			Assets.Add<Texture2D>("tile_wf", AssetLoader.TextureFromMod(this, Path.Combine("Sprites", "TileWrongFlagged.png")));
			Assets.Add<Texture2D>("tile_bomb", AssetLoader.TextureFromMod(this, Path.Combine("Sprites", "TileBomb.png")));
			Assets.Add<Texture2D>("tile_flag", AssetLoader.TextureFromMod(this, Path.Combine("Sprites", "TileFlagged.png")));
			for (int i = 0; i < 9; i++)
			{
				Assets.Add<Texture2D>("tile_" + i, AssetLoader.TextureFromMod(this, Path.Combine("Sprites", "Tile" + i +".png")));
			}
			yield break;
		}
		
		public IEnumerator LoadGame()
		{
			AsyncOperation a = SceneManager.LoadSceneAsync("Game");
			while (!a.isDone)
			{
				yield return null;
			}
			MinesweeperManager component = new GameObject("BaseController", new Type[]
			{
				typeof(MinesweeperManager)
			}).GetComponent<MinesweeperManager>();
			component.Init();
			Shader.SetGlobalTexture("_Skybox", (from x in Resources.FindObjectsOfTypeAll<Cubemap>()
			where x.name == "Cubemap_DayStandard"
			select x).First<Cubemap>());
			Shader.SetGlobalColor("_SkyboxColor", Color.white);
			Shader.SetGlobalColor("_FogColor", Color.white);
			Shader.SetGlobalFloat("_FogStartDistance", 5f);
			Shader.SetGlobalFloat("_FogMaxDistance", 100f);
			Shader.SetGlobalFloat("_FogStrength", 0f);
			component.gameCamera = UnityEngine.Object.Instantiate<GameCamera>(Resources.FindObjectsOfTypeAll<GameCamera>().First<GameCamera>());
			component.BuildCells(20, 20, 60);
			Canvas canvas = UIHelpers.CreateBlankUIScreen("BlankScreen", true, false);
			component.canvas = canvas;
			component.InitUI();
			if ((float)Singleton<PlayerFileManager>.Instance.resolutionX / (float)Singleton<PlayerFileManager>.Instance.resolutionY >= 1.3333f)
			{
				canvas.scaleFactor = (float)Mathf.RoundToInt((float)Singleton<PlayerFileManager>.Instance.resolutionY / 360f);
			}
			else
			{
				canvas.scaleFactor = (float)Mathf.FloorToInt((float)Singleton<PlayerFileManager>.Instance.resolutionY / 480f);
			}
			CursorInitiator ci = UIHelpers.AddCursorInitiatorToCanvas(canvas, new Vector2((float)Screen.width / canvas.scaleFactor, (float)Screen.height / canvas.scaleFactor), null);
			canvas.gameObject.SetActive(false);
			canvas.gameObject.SetActive(true);
			component.cursor = ci.cursorPre;
			component.cursorBounds = ci.screenSize;
			canvas.worldCamera = component.gameCamera.canvasCam;
			
			yield break;
		
				
		}
	}
}