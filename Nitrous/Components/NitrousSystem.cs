using System.Collections.Generic;
using UnityEngine;
using Logger = Nitrous.Modules.Logger;

namespace Nitrous.Components
{
	internal class NitrousSystem : MonoBehaviour
	{
		private carscript _car;
		private AudioSource _audio;
		private List<NitrousOxide> _mountedBottles = new List<NitrousOxide>();
		private Color _nitrousColour = new Color(0.1f, 0.6f, 1f);

		private bool _isBoosting = false;

		private float _currentBoost = 0f;
		private float _effectiveBoost = 0f;
		private float _boostedModifier;
		private float _boostedFuelConsumption;

		private const float _coreBoostModifier = 2f;
		private const float _boostRampSpeed = 0.75f;
		private const float _boostRampDownSpeed = 2f;
		private const float _bottleConsumption = 0.002f;
		private const float _fuelUsageMultiplier = 5f;
		private const float _tempMultiplier = 0.75f;

		public void Start()
		{
			_car = GetComponent<carscript>();
			_audio = _car.gameObject.AddComponent<AudioSource>();
			_audio.clip = Nitrous.hiss;
			_audio.pitch = 1.5f;
			_audio.loop = true;
		}

		public void Update()
		{
			List<NitrousOxide> bottles = GetUsableBottles();

			enginescript engine = _car.Engine;

			if (engine == null)
			{
				_currentBoost = Mathf.MoveTowards(_currentBoost, 0f, _boostRampDownSpeed * Time.deltaTime);
				return;
			}

			_isBoosting = mainscript.M.player.Car == _car && engine.running && engine.rpm > 1000 && bottles.Count > 0 && Input.GetKey(Nitrous.boostKey);

			// Cause bottles to provide diminishing returns based on number mounted.
			float totalBoostMultiplier = 0f;
			for (int i = 0; i < bottles.Count; i++)
			{
				totalBoostMultiplier += 1f / (1f + i);
			}

			bool rampDown = !_isBoosting || (!engine?.running ?? false);

			// Handle boost ramping up or down.
			_currentBoost = Mathf.MoveTowards(_currentBoost, rampDown ? 0f : 1f, (rampDown ? _boostRampDownSpeed : _boostRampSpeed) * Time.deltaTime);

			if (_currentBoost > 0 && !_audio.isPlaying)
				_audio.Play();
			else if (_currentBoost <= 0 && _audio.isPlaying)
				_audio.Stop();

			// Stop the audio on game pause.
			if (_car.RB == null)
				_audio.Stop();

			// Fade audio in and out with boost.
			if (_audio.isPlaying)
				_audio.volume = _currentBoost * settingsscript.s.S.FSound * 0.50f;

			_effectiveBoost = _currentBoost * totalBoostMultiplier;

			// Apply the power boost and increased fuel consumption.
			engine.modifier -= _boostedModifier;
			if (engine.FuelConsumption.fluids.Count == 1)
				engine.FuelConsumption.ChangeWithoutMix(-_boostedFuelConsumption, false);
			if (_currentBoost <= 0f)
			{
				_boostedModifier = 0f;
				_boostedFuelConsumption = 0f;
			}
			else
			{
				_boostedModifier = _coreBoostModifier * _effectiveBoost;
				engine.modifier += _boostedModifier;

				if (engine.FuelConsumption.fluids.Count == 1)
				{
					_boostedFuelConsumption = _fuelUsageMultiplier * _effectiveBoost;
					engine.FuelConsumption.ChangeWithoutMix(_boostedFuelConsumption, false);
				}
			}

			// Bit of a power kick whilst the modifier boost increases.
			float boostDrpm = engine.maxRpm * (1f + _coreBoostModifier * _effectiveBoost * 0.2f);
			engine.drpm = Mathf.Max(engine.drpm, boostDrpm);

			// Increase engine temperature.
			engine.temp += Time.deltaTime * _tempMultiplier * _effectiveBoost;

			if (_currentBoost > 0)
			{
				// Set exhaust smoke colour.
				for (int index = 0; index < _car.psSmokeEmission.Length; ++index)
					_car.psSmokeMain[index].startColor = (ParticleSystem.MinMaxGradient)_nitrousColour;

				// Handle bottle usage.
				float consume = _bottleConsumption * Time.deltaTime / bottles.Count;
				foreach (NitrousOxide bottle in bottles)
				{
					bottle.Tank.F.ChangeWithoutMix(-consume);
					bottle.Tank.Upd();
				}
			}
		}

		/// <summary>
		/// Register a mounted nitrous bottle.
		/// </summary>
		/// <param name="bottle">Bottle to register</param>
		public void RegisterBottle(NitrousOxide bottle)
		{
			if (!_mountedBottles.Contains(bottle))
				_mountedBottles.Add(bottle);
		}

		/// <summary>
		/// Unregister an unmounted nitrous bottle.
		/// </summary>
		/// <param name="bottle">Bottle to unregister</param>
		public void UnregisterBottle(NitrousOxide bottle)
		{
			_mountedBottles.Remove(bottle);
		}

		/// <summary>
		/// Get the nitrous bottles which are not empty.
		/// </summary>
		/// <returns>List of usable nitrous bottles</returns>
		private List<NitrousOxide> GetUsableBottles()
		{
			List<NitrousOxide> available = new List<NitrousOxide>();
			foreach (NitrousOxide bottle in _mountedBottles)
			{
				if (bottle.Tank.F.GetAmount() > 0)
					available.Add(bottle);
			}

			return available;
		}

		public float GetBoost() => _effectiveBoost;
		public float GetAmount()
		{
			float amount = 0;
			foreach (NitrousOxide bottle in GetUsableBottles())
			{
				amount += bottle.Tank.F.GetAmount();
			}
			return amount;
		}

#if DEBUG
		public void OnGUI()
		{
			if (mainscript.M.player.Car == null || mainscript.M.player.Car != _car) return;

			GUI.Button(new Rect(0, 0, 200, 20),	  $"Boosting: {(_isBoosting ? "yes" : "no")}");
			GUI.Button(new Rect(0, 20, 200, 20),  $"Bottles: {GetUsableBottles().Count}");
			GUI.Button(new Rect(0, 40, 200, 20),  $"Current boost: {_currentBoost}");
			GUI.Button(new Rect(0, 60, 200, 20),  $"Effective boost: {_effectiveBoost}");
			GUI.Button(new Rect(0, 80, 200, 20),  $"Temp: {(_car.Engine != null ? _car.Engine.temp.ToString() : "-")}");
			GUI.Button(new Rect(0, 100, 200, 20), $"Fuel consumption: {(_car.Engine != null ? _car.Engine.FuelConsumption.GetAmount().ToString() : "-")}");
			GUI.Button(new Rect(0, 120, 200, 20), $"Modifier: {(_car.Engine != null ? _car.Engine.modifier.ToString() : "-")}");
		}
#endif
	}
}
