///-----------------------------------------------------------------
/// Author : Tom FALEMPIN
/// Date : 21/01/2020 17:52
///-----------------------------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;
using Com.DefaultCompany.Platformer.Platformer.InteractableObjects;
using System.Linq;
using Com.IsartDigital.Platformer.InteractableObjects.CollisionableObjects.Collectibles;
using Com.IsartDigital.Platformer.Managers;
using Com.IsartDigital.Platformer.PlayerScripts;
using Com.DefaultCompany.Platformer.Platformer.Managers;
using Com.IsartDigital.Platformer.InteractableObjects.CollisionableObjects;

namespace Com.IsartDigital.Platformer.Platformer.Managers {

	public class CollectibleManager : MonoBehaviour {

		[Header("Lists")]
		[SerializeField] private List<GameObject> Artefacts = new List<GameObject>();
		[SerializeField] private List<GameObject> Scores = new List<GameObject>();
		[SerializeField] private List<GameObject> Lives = new List<GameObject>();

		///ARTEFACTS
		//
		static private int ArtefactsToCollect = 0;
		static private int ArtefactsCollected = 0;
		static public int NbrOfArtefacts => ArtefactsCollected;
		static public bool IsAllArtefactsCollect => ArtefactsCollected >= ArtefactsToCollect;
		//
		/////////////////////////////////////////////////////////////////////////////////////


		///LISTS
		//
		static private List<GameObject> CollectiblesOnBigCP = new List<GameObject>();
		static private List<GameObject> CollectiblesOnSmallCP = new List<GameObject>();
		//static private List<GameObject> AlreadyCollectedArtefacts = new List<GameObject>();
		///////////////////////////////////////////////////////////////////////////////


		public delegate void CollectibleManagerEventHandler(int nbr, GameObject artefact);
		public static event CollectibleManagerEventHandler UpdateArtefacts;


		/// <summary>
		/// 
		/// Initialise le manager, les lists, les events
		/// 
		/// </summary>
		private void Start()
		{
			Collectible.OnCollected += Collectible_OnCollected;
			CheckpointManager.OnCheck += CheckpointManager_OnCheck;

			Init();
		}


		/// <summary>
		/// 
		/// Nettoie les listes et initialise les nbr d'artefacts correctement
		/// 
		/// </summary>
		private void Init()
		{
			CollectiblesOnBigCP.Clear();
			CollectiblesOnSmallCP.Clear();

			ArtefactsToCollect = Artefacts.Count;
			ArtefactsCollected = 0;
		}


		/// <summary>
		/// 
		/// Si le collectible passé en paramètre n'est pas déjà stocké dans la liste de Restart de gros checkpoint, le stocke dedans
		/// stock le collectible dans la liste de Restart de petit checkpoint en fonction de son type
		/// 
		/// </summary>
		/// <param name="typeOfCollectible"></param>
		/// <param name="collectible"></param>
		private void Collectible_OnCollected(Collectible.Type typeOfCollectible, GameObject collectible)
		{
			if (CollectiblesOnBigCP.IndexOf(collectible) <= -1)
				CollectiblesOnBigCP.Add(collectible);


			switch (typeOfCollectible)
			{
				case Collectible.Type.Artefact:
					ArtefactsCollected++;
					CollectiblesOnSmallCP.Add(collectible);
					//AlreadyCollectedArtefacts.Add(collectible);

					UpdateArtefacts?.Invoke(ArtefactsCollected, collectible);
					//Debug.Log(ArtefactsCollected);
					CameraShake.Instance.ScreenShake(0.2f, 8f);

					if (LevelManager.CurrentLevelIndex == 1 && IsAllArtefactsCollect)
					{
						//Debug.Log("all artefacts + triggr");
						//CutSceneTrigger.SendAllArtefactTrigger();
						Invoke(nameof(callTriggerAll), 1.6f);
						
					}
					break;

				case Collectible.Type.Life:
					break;

				case Collectible.Type.Score:
					CollectiblesOnSmallCP.Add(collectible);
					break;

				default:
					break;
			}
		}

		private void callTriggerAll()
		{
			CutSceneTrigger.SendAllArtefactTrigger();
		}


		/// <summary>
		/// 
		/// parcourt la bonne liste en fonction du booléen afin de les initialiser
		/// nettoie ensuite les lists qu'il faut vider
		/// 
		/// </summary>
		/// <param name="isSmallCheckPoint"></param>
		static public void RestartCollectible(bool isSmallCheckPoint)
		{
			//Debug.Log("restart");
			List<GameObject> listToRestart = isSmallCheckPoint ? CollectiblesOnSmallCP : CollectiblesOnBigCP;

			foreach (GameObject collectible in listToRestart)
			{
				collectible.GetComponent<Collectible>().init();
				if (collectible.GetComponent<Collectible>().GetTypeOfCollectible == Collectible.Type.Artefact)
				{
					//Debug.Log("artefact");
					ArtefactsCollected--;
				}
				//if (AlreadyCollectedArtefacts.IndexOf(collectible) >= -1) AlreadyCollectedArtefacts.Remove(collectible);
			}
			listToRestart.Clear();

			if (isSmallCheckPoint)
				CollectiblesOnSmallCP.Clear();

			UpdateArtefacts?.Invoke(ArtefactsCollected, null);
		}


		/// <summary>
		/// 
		/// Nettoie les listes en fonction du booléen en paramètre
		/// 
		/// </summary>
		/// <param name="isSmall"></param>
		private void CheckpointManager_OnCheck(bool isSmall)
		{
			if (!isSmall) CollectiblesOnBigCP.Clear();
			CollectiblesOnSmallCP.Clear();
		}


		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		private void OnDestroy()
		{
			Collectible.OnCollected -= Collectible_OnCollected;
			CheckpointManager.OnCheck -= CheckpointManager_OnCheck;
		}
	}
}