using Nitrous.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Logger = Nitrous.Modules.Logger;

namespace Nitrous.Components
{
	[DisallowMultipleComponent]
	public sealed class Gauge : MonoBehaviour
	{
		private GameObject _compass;
		private GameObject _kmGauge;
		private meterscript _compassMeter;
		private meterscript.szamlap _boostColour;
		private meterscript.szamlap _kmGaugeMeter;
		private tankscript _tank;
		private float _lastColourVal = 0f;

		private Color[] _colors = new Color[]
		{
			Color.grey,
			Color.red,
			Color.magenta,
			Color.blue,
			Color.cyan,
			Color.green,
			Color.yellow,
			Color.white,
		};

		// 1 in 3 chance of replacing.
		public static bool ShouldReplace(int id) => new System.Random(id).Next(3) == 0;

		public void Start()
		{
			tosaveitemscript save = gameObject.GetComponent<tosaveitemscript>();
			if (save == null || !ShouldReplace(save.idInSave))
			{
				enabled = false;
				return;
			}

			// Destroy existing object children.
			string[] namesToDestroy = { "Collider", "gazpalack" };
			foreach (Transform child in transform)
			{
				if (Array.Exists(namesToDestroy, name => child.name == name))
				{
					Destroy(child.gameObject);
				}
			}

			_tank = GetComponent<tankscript>();
			_tank.neverExplode = true;
			_tank.F.maxC = 360f;
			if (!save.loaded)
				_tank.F.fluids.Clear();

			massScript mass = GetComponent<massScript>();
			mass.tanks = new List<tankscript>();
			mass.SetMass(1);

			// Replace with compass as the base.
			_compass = Instantiate(itemdatabase.d.gww2compass);
			tosaveitemscript compassSave = _compass.GetComponent<tosaveitemscript>();
			compassSave.removeFromMemory = true;
			Destroy(_compass.GetComponent<Rigidbody>());
			Destroy(_compass.GetComponent<pickupable>());
			Destroy(_compass.GetComponent<partscript>());
			Destroy(_compass.GetComponent<massScript>());
			Destroy(_compass.GetComponent<partconditionscript>());
			// Prevent any conflicts from compass replacement mods.
			DestroyNonGameComponents(_compass);
			_compass.transform.SetParent(transform, false);
			_compass.transform.position = Vector3.zero;
			_compass.transform.localPosition = Vector3.zero;

			compassscript compass = _compass.GetComponent<compassscript>();
			foreach (meterscript.szamlap szamlap in compass.meter.szamlapok)
			{
				if (szamlap.tipus == "compasstarget")
				{
					// Remove the direction guide needle thing.
					if (szamlap.forgo != null)
					{
						forgoszamlap.szam[] szamok = szamlap.forgo.szamok;
						for (int index = 0; index < szamok.Length; ++index)
						{
							szamok[index].r.transform.localScale = new Vector3(1f, 1f, 1f);
							szamok[index].r.enabled = false;
						}
					}
					szamlap.value = _tank.F.GetAmount();
					_boostColour = szamlap;
					break;
				}
			}

			GameObject face = new GameObject("GaugeFace");
			face.transform.SetParent(_compass.transform, false);
			face.transform.localScale = new Vector3(-0.075f, 0.075f, 1E-06f);
			face.transform.localPosition = new Vector3(0.0f, 0.0f, -0.008f);
			face.AddComponent<MeshFilter>().mesh = itemdatabase.d.gerror.GetComponentInChildren<MeshFilter>().mesh;
			compass.meter.R = face.AddComponent<MeshRenderer>();
			compass.meter.OffM = new Material(Shader.Find("Standard"));
			compass.meter.OffM.mainTexture = Nitrous.gauge;
			compass.meter.OffM.mainTextureScale = new Vector2(1, -1);
			compass.meter.OffM.SetFloat("_Mode", 2f);
			compass.meter.OffM.SetInt("_SrcBlend", 5);
			compass.meter.OffM.SetInt("_DstBlend", 10);
			compass.meter.OffM.SetInt("_ZWrite", 0);
			compass.meter.OffM.EnableKeyword("_ALPHATEST_ON");
			compass.meter.OffM.EnableKeyword("_ALPHABLEND_ON");
			compass.meter.OffM.EnableKeyword("_ALPHAPREMULTIPLY_ON");
			compass.meter.OffM.SetFloat("_SpecularHighlights", 0.0f);
			compass.meter.OffM.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
			compass.meter.OffM.renderQueue = 3000;
			compass.meter.OnM = new Material(compass.meter.OffM);
			compass.meter.OnM.EnableKeyword("_EMISSION");
			compass.meter.OnM.SetTexture("_EmissionMap", compass.meter.OnM.mainTexture);
			compass.meter.OnM.name = "ON_Material";
			compass.meter.R.material = compass.meter.OffM;
			compass.meter.R.reflectionProbeUsage = ReflectionProbeUsage.Off;

			Color color = GetMeterColour();
			foreach (meterscript.meterstuff meterstuff in compass.meter.mutatok)
			{
				meterstuff.mutato.minAngle = -68f;
				meterstuff.mutato.maxAngle = 68f;
				meterstuff.mutato.minValue = 0;
				meterstuff.mutato.maxValue = 100;
				meterstuff.mutato.OffM = new Material(meterstuff.mutato.OffM);
				meterstuff.mutato.OffM.color = color;
				meterstuff.mutato.OnM = new Material(meterstuff.mutato.OnM);
				meterstuff.mutato.OnM.color = color;
				meterstuff.mutato.OnM.SetColor("_EmissionColor", new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f));
				meterstuff.mutato.R.material = meterstuff.mutato.OffM;
				meterstuff.mutato.inv = false;
				meterstuff.mutato.inputValueOffset = 0;
				meterstuff.tipus = "NitrousBoost";
				meterstuff.mutato.lerpAngle = false;
			}
			_compassMeter = compass.meter;

			compass.enabled = false;

			// Use a km gauge for the nitrous remaining count.
			_kmGauge = Instantiate(itemdatabase.d.gbus02Kmh);
			tosaveitemscript kmSave = _kmGauge.GetComponent<tosaveitemscript>();
			kmSave.removeFromMemory = true;
			Destroy(_kmGauge.GetComponent<Rigidbody>());
			Destroy(_kmGauge.GetComponent<pickupable>());
			Destroy(_kmGauge.GetComponent<partscript>());
			Destroy(_kmGauge.GetComponent<massScript>());
			// Prevent any conflicts from gauge replacement mods.
			DestroyNonGameComponents(_kmGauge);
			_kmGauge.transform.SetParent(_compass.transform, false);
			_kmGauge.transform.localPosition = new Vector3(0.0f, 0.0546f, -0.005f);
			_kmGauge.transform.localScale = Vector3.one * 1.4f;
			for (int index = 0; index < _kmGauge.transform.childCount; index++)
			{
				Transform child = _kmGauge.transform.GetChild(index);
				if (child.name == "kmszamlalo")
				{
					foreach (MeshRenderer renderer in child.GetComponentsInChildren<MeshRenderer>())
						renderer.material.color = color;
					child.GetChild(5).gameObject.SetActive(false);
					child.GetChild(4).gameObject.SetActive(false);
					child.GetChild(3).gameObject.SetActive(false);
					child.localPosition = new Vector3(-0.0065f, child.localPosition.y - 0.008f, child.localPosition.z + 0.0011f);
					child.localRotation = new Quaternion(0, 180, 0, 0);
				}
				else
					child.gameObject.SetActive(false);
			}
			foreach (meterscript.szamlap szamlap in _kmGauge.GetComponent<meterscript>().szamlapok)
			{
				if (szamlap.tipus == "kilometerszamlalo")
				{
					_kmGaugeMeter = szamlap;
					szamlap.tipus = "NitrousTank";
					break;
				}
			}

			// Make attachable.
			attachablescript attach = GetComponent<attachablescript>();
			pickupable pickup = GetComponent<pickupable>();
			if (attach == null)
			{
				attachablescript coreAttach = itemdatabase.d.gww2compass.GetComponent<attachablescript>();
				pickupable compassPickup = itemdatabase.d.gww2compass.GetComponent<pickupable>();
				Destroy(gameObject.GetComponent<pickupable>());
				gameObject.CopyComponent(coreAttach);
				pickup = gameObject.CopyComponent(compassPickup);
				attach = GetComponent<attachablescript>();
				pickup.attach = attach;
				save.attachable = attach;
			}
			pickup.cols = _compass.GetComponentsInChildren<layerScript>();
			attach.C = _compass.GetComponentsInChildren<Collider>().ToList();
		}

		public void Update()
		{
			if (!enabled) return;

			// Clamp colour dial value between 0 and 360, allowing for wrapping values.
			_boostColour.value = (_boostColour.value % 360 + 360) % 360;
			if (_lastColourVal != _boostColour.value)
			{
				// Save the selected colour using the tank as it's unused.
				_tank.F.fluids.Clear();
				_tank.F.ChangeOne(_boostColour.value, mainscript.fluidenum.water);
				_lastColourVal = _boostColour.value;

				// Set dial colour.
				Color color = GetMeterColour();
				foreach (meterscript.meterstuff meterstuff in _compassMeter.mutatok)
				{
					meterstuff.mutato.OffM.color = color;
					meterstuff.mutato.OnM.color = color;
					meterstuff.mutato.OnM.SetColor("_EmissionColor", new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f));
				}

				// Set dial text colour.
				for (int index = 0; index < _kmGauge.transform.childCount; index++)
				{
					Transform child = _kmGauge.transform.GetChild(index);
					if (child.name == "kmszamlalo")
					{
						foreach (MeshRenderer renderer in child.GetComponentsInChildren<MeshRenderer>())
							renderer.material.color = color;
					}
				}
			}

			carscript car = GetComponentInParent<carscript>();

            if (car == null || !car.ignition)
            {
				UpdateCompassMeter(0);
				_compassMeter.R.material = _compassMeter.OffM;
				_kmGaugeMeter.value = 0;
				return;
            }

			_compassMeter.R.material = car.lampak != 0 ? _compassMeter.OnM : _compassMeter.OffM;
			foreach (meterscript.meterstuff meter in _compassMeter.mutatok)
				meter.mutato.R.material = car.lampak != 0 ? meter.mutato.OnM : meter.mutato.OffM;

			NitrousSystem system = car.GetComponent<NitrousSystem>();
			UpdateCompassMeter(Mathf.InverseLerp(0f, 4f, system.GetBoost()) * 100);
			_kmGaugeMeter.value = system.GetAmount();
        }

		private void UpdateCompassMeter(float val)
		{
			foreach (meterscript.meterstuff meter in _compassMeter.mutatok)
				meter.value = val;
		}

		private Color GetMeterColour()
		{
			float segmentSize = 360f / _colors.Length;
			int indexA = Mathf.FloorToInt(_boostColour.value / segmentSize) % _colors.Length;
			int indexB = (indexA + 1) % _colors.Length;

			float t = (_boostColour.value % segmentSize) / segmentSize;
			return Color.Lerp(_colors[indexA], _colors[indexB], t);
		}

		private void DestroyNonGameComponents(GameObject target)
		{
			foreach (Component component in target.GetComponents<Component>())
			{
				Type type = component.GetType();
				string assemblyName = type.Assembly.GetName().Name;
				if (!(assemblyName.StartsWith("UnityEngine") || assemblyName == "Assembly-CSharp"))
				{
					Destroy(component);
				}
			}
		}
	}
}
