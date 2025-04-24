using Nitrous.Extensions;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Logger = Nitrous.Modules.Logger;

namespace Nitrous.Components
{
	[DisallowMultipleComponent]
	public sealed class NitrousOxide : MonoBehaviour
	{
		private GameObject _nitrous;
		private float[] _sizes = new float[]
		{
			0.5f,
			0.75f,
			1f,
			1.25f,
			1.5f,
		};
		private float _size;
		private NitrousSystem _system;
		public tankscript Tank;
		private attachablescript _attach;
		private carscript _car;

		// 1 in 2 chance of replacing the siphon.
		public static bool ShouldReplace(int id) => new System.Random(id).Next(2) == 0;

		public void Start()
		{
			tosaveitemscript save = gameObject.GetComponent<tosaveitemscript>();
			if (save == null || !ShouldReplace(save.idInSave)) 
			{
				enabled = false;
				return;
			}

			System.Random random = new System.Random(save.idInSave);

			// Throwaway call to improve randomness of the size.
			// I have no idea why this works, but it does.
			random.Next(1000);

			// Disable default meshes and colliders.
			foreach (MeshRenderer mesh in gameObject.GetComponentsInChildren<MeshRenderer>())
				mesh.enabled = false;
			foreach (Collider collider in gameObject.GetComponentsInChildren<Collider>())
				collider.enabled = false;

			int sizeIndex = random.Next(_sizes.Length);
			_size = _sizes[sizeIndex];

			_nitrous = Instantiate(Nitrous.bottle);
			_nitrous.transform.SetParent(transform, false);
			_nitrous.transform.position = Vector3.zero;
			_nitrous.transform.localScale = new Vector3(_size, _size, _size) * 100f;
			_nitrous.transform.localPosition = Vector3.zero;
			_nitrous.transform.localEulerAngles = Vector3.zero;
			Tank = gameObject.GetComponent<tankscript>();

			// Set size on tank.
			TextMeshPro text = _nitrous.GetComponentInChildren<TextMeshPro>();
			text.text = (sizeIndex + 1).ToString();

			// Adjust max tank to account for size.
			Tank.F.maxC = 1f * _size;

			// For fresh spawns, adjust fluids to match the tank size.
			if (!save.loaded)
			{
				Tank.F.fluids.Clear();
				Tank.F.ChangeOne(Tank.F.maxC, mainscript.fluidenum.oil);
			}

			_attach = GetComponent<attachablescript>();
			pickupable pickup = GetComponent<pickupable>();

			// Attach anything mod not used, replicate the behaviour.
			if (_attach == null)
			{
				if (save.attachable != null)
					gameObject.CopyComponent(save.attachable);
				attachablescript attach = itemdatabase.d.graklap.GetComponent<attachablescript>();
				gameObject.CopyComponent(attach);
				_attach = GetComponent<attachablescript>();
				pickup.attach = _attach;
				save.attachable = _attach;
			}

			// Fix any missing collider data.
			_attach.C = _nitrous.GetComponentsInChildren<Collider>().ToList();
			List<layerScript> layerScripts = new List<layerScript>();
			foreach (Collider col in _nitrous.GetComponentsInChildren<Collider>())
				layerScripts.Add(col.gameObject.AddComponent<layerScript>());
			pickup.cols = layerScripts.ToArray();
			pickup.cols2 = layerScripts.ToArray();
		}

		public void Update()
		{
			if (!enabled) return;

			// Show tank when looking at bottle.
			fpscontroller player = mainscript.M.player;
			if (Physics.Raycast(player.Cam.transform.position, player.Cam.transform.forward, out RaycastHit hitInfo1, player.FrayRange, (int)player.useLayer))
			{
				if (hitInfo1.collider.transform.parent?.parent?.gameObject == gameObject)
				{
					player.capString = $"{(Tank.F.GetAmount() * 10).ToString("F2")} / {(Tank.F.maxC * 10).ToString("F2")} kg";
				}
			}

			if (_attach.targetTosave != null)
			{
				_car = _attach.targetTosave.GetComponentInParent<carscript>();
			}
			else
				_car = null;

			if (_car != null)
			{
				// Car doesn't have a nitrous system, add one.
				if (_car.GetComponent<NitrousSystem>() == null)
					_car.gameObject.AddComponent<NitrousSystem>();

				// Register the mounted bottle.
				_system = _car.GetComponent<NitrousSystem>();
				_system.RegisterBottle(this);
			}
			else
			{
				if (_system != null)
				{
					_system.UnregisterBottle(this);
					_system = null;
				}

				// Mounting support.
				// Source: Turbocharger.
				try
				{
					if (mainscript.M.player.pickedUp != null && mainscript.M.player.pickedUp.attach != null && mainscript.M.player.pickedUp.attach == _attach)
					{
						RaycastHit hitInfo;
						if (Physics.Raycast(mainscript.M.player.mainCam.transform.position, mainscript.M.player.mainCam.transform.forward, out hitInfo, mainscript.M.player.FrayRange, (int)mainscript.M.player.useLayer))
						{
							if (!mainscript.M.player.pushgripping)
							{
								if (hitInfo.collider.gameObject.layer != 9)
								{
									if (hitInfo.collider.gameObject.GetComponentInParent<carscript>() != null)
									{
										mainscript.M.player.BcanMount = true;
										mainscript.M.menu.GCF.sprite = mainscript.M.menu.IMount;
										Nitrous.mount = true;
										if (inputscript.i.GetKeyDown(inputscript.IN.mountdismount))
										{
											if (transform.root == transform)
											{
												if (_attach.targetTosave == null)
												{
													mainscript.M.player.Drop();
													mainscript.M.player.BAttach = true;
													Transform transform = new GameObject("NitrousAttachPoint").transform;
													transform.parent = hitInfo.collider.transform;
													transform.position = hitInfo.point + hitInfo.normal * _attach.offsetback;
													transform.forward = hitInfo.normal;
													_attach.Attach(transform, hitInfo);
												}
											}
										}
									}
								}
							}
						}
					}
				}
				catch
				{
				}
			}
		}

#if DEBUG
		public void OnGUI()
		{
			if (mainscript.M.player != null && mainscript.M.player.seat == null)
			{
				Physics.Raycast(mainscript.M.player.Cam.transform.position, mainscript.M.player.Cam.transform.forward, out var raycastHit, float.PositiveInfinity, mainscript.M.player.useLayer);
				if (raycastHit.collider.transform.parent?.parent?.gameObject != gameObject) return;

				if (_size == 0) return;

				GUI.Button(new Rect(0, 0, 200, 20), $"Size: {_size}");
				GUI.Button(new Rect(0, 20, 200, 20), $"Tank max: {Tank.F.maxC}");
				GUI.Button(new Rect(0, 40, 200, 20), $"Tank current: {Tank.F.GetAmount()}");
			}
		}
#endif
	}
}
