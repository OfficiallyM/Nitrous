using System;
using TLDLoader;
using UnityEngine;
using System.Reflection;
using Nitrous.Components;
using System.Linq;
using Logger = Nitrous.Modules.Logger;

namespace Nitrous
{
	public class Nitrous : Mod
	{
		// Mod meta stuff.
		public override string ID => "M_Nitrous";
		public override string Name => "Nitrous";
		public override string Author => "M-";
		public override string Version => "1.1.0";
		public override bool LoadInDB => true;

		internal static Nitrous Mod;
		internal static GameObject bottle;
		internal static AudioClip hiss;
		internal static Texture gauge;

		internal static FieldInfo mountField;
		internal static bool mount;

		internal static KeyCode boostKey = KeyCode.R;

		public Nitrous()
		{
			Mod = this;

			Logger.Init();
		}

		public override void Config()
		{
			SettingAPI settings = new SettingAPI(this);
			boostKey = settings.GUIKeyBinding(boostKey, "Boost key", 10, 10);
		}

		public override void dbLoad()
		{
			AssetBundle assetBundle = AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(Nitrous)}.nitrous"));
			bottle = assetBundle.LoadAsset<GameObject>("bottle");
			hiss = assetBundle.LoadAsset<AudioClip>("hiss.mp3");
			gauge = assetBundle.LoadAsset<Texture2D>("gauge.dds");

			if (itemdatabase.d.gszifon.GetComponent<NitrousOxide>() == null)
				itemdatabase.d.gszifon.AddComponent<NitrousOxide>();
			itemdatabase.d.gszifon.name = "Siphon or Nitrous Bottle";

			if (itemdatabase.d.ggazpalack.GetComponent<Gauge>() == null)
				itemdatabase.d.ggazpalack.AddComponent<Gauge>();
			itemdatabase.d.ggazpalack.name += " or Nitrous Gauge";

			foreach (GameObject item in itemdatabase.d.items)
			{
				if (item.GetComponent<carscript>() != null && item.GetComponent<NitrousSystem>() == null)
					item.AddComponent<NitrousSystem>();
			}

			// Create placeholders to show in M-ultiTool mod items category.
			try
			{
				GameObject nitrousPlaceholder = new GameObject("NitrousPlaceholder");
				nitrousPlaceholder.transform.SetParent(mainscript.M.transform);
				nitrousPlaceholder.SetActive(false);
				GameObject nitrous = new GameObject("Nitrous Oxide Bottle");
				nitrous.transform.SetParent(nitrousPlaceholder.transform, false);
				UnityEngine.Object.Instantiate(bottle, nitrous.transform, false).transform.Rotate(0f, 180f, 0f);
				nitrous.AddComponent<NitrousSpawner>();
				itemdatabase.d.items = Enumerable.Append(itemdatabase.d.items, nitrous).ToArray();
				nitrous.GetComponentInChildren<Collider>().enabled = false;

				GameObject gaugePlaceholder = new GameObject("NitrousGaugePlaceholder");
				gaugePlaceholder.transform.SetParent(mainscript.M.transform);
				gaugePlaceholder.SetActive(false);
				GameObject gauge = new GameObject("Nitrous Oxide Gauge");
				gauge.transform.SetParent(gaugePlaceholder.transform, false);
				UnityEngine.Object.Instantiate(itemdatabase.d.ggazpalack, gauge.transform, false).transform.Rotate(0f, 180f, 0f);
				gauge.AddComponent<GaugeSpawner>();
				itemdatabase.d.items = Enumerable.Append(itemdatabase.d.items, gauge).ToArray();
				gauge.GetComponentInChildren<Collider>().enabled = false;
			}
			catch (Exception ex)
			{
				Logger.Log($"Failed to create placeholders. Details: {ex}");
			}
		}

		public override void Update()
		{
			// Mounting support.
			// Source: Turbocharger.
			if (mount)
			{
				try
				{
					if (mountField == null)
					{
						string[] strArray = new string[3]
						{
							"F",
							"StringMountDismount",
							"G"
						};
						foreach (string name in strArray)
						{
							try
							{
								mountField = typeof(fpscontroller).GetField(name);
							}
							catch
							{
							}
							if (mountField != null)
								break;
						}
					}
					mountField.SetValue(mainscript.M.player, "Mount Nitrous Bottle");
				}
				catch
				{
				}
			}
			mount = false;
		}
	}
}
