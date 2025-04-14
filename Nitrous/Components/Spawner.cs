using UnityEngine;

namespace Nitrous.Components
{
	internal sealed class NitrousSpawner : MonoBehaviour
	{
		public void Start()
		{
			try
			{
				int num = 0;
				while (!NitrousOxide.ShouldReplace(num) || savedatascript.d.toSaveStuff.ContainsKey(num) || savedatascript.d.data.farStuff.ContainsKey(num) || savedatascript.d.data.nearStuff.ContainsKey(num))
					++num;

				GameObject g = Instantiate(itemdatabase.d.gszifon, transform.position, transform.rotation);
				g.GetComponent<tosaveitemscript>().FStart(num);
				mainscript.M.PostSpawn(g);
			}
			catch { }
			Destroy(gameObject, 0.0f);
			gameObject.SetActive(false);
		}
	}
}
