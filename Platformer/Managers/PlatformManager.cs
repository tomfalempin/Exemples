///-----------------------------------------------------------------
/// Author : Tom FALEMPIN
/// Date : 16/01/2020 17:15
///-----------------------------------------------------------------

using Com.IsartDigital.Platformer.Managers;
using Com.IsartDigital.Platformer.Platformer.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Com.DefaultCompany.Platformer.Platformer.Managers {
	public class PlatformManager : MonoBehaviour {

		///LISTS DES PLATFORMES EN FONCTION DES CHECKPOINTS
		//
		static private List<Platform> PlatformsOnBigCP = new List<Platform>();
		static private List<Platform> PlatformsOnSmallCP = new List<Platform>();
		//
		////////////////////////////////////////////////////////////////////////


		/// <summary>
		/// 
		/// Initialise le manager, les events
		/// 
		/// </summary>
		private void Start()
		{
			Platform.OnCollision += Platform_OnCollision;
			CheckpointManager.OnCheck += CheckpointManager_OnCheck;

			Init();
		}


		/// <summary>
		/// 
		/// initialise en nettoyant les listes
		/// 
		/// </summary>
		private void Init()
		{
			PlatformsOnBigCP.Clear();
			PlatformsOnSmallCP.Clear();
		}


		/// <summary>
		/// 
		/// Si la platform passé en paramètre n'est pas déjà stocké dans la liste de Restart de gros checkpoint, la stocke dedans
		/// stock la platform dans la liste de Restart de petit checkpoint
		/// 
		/// </summary>
		/// <param name="platform"></param>
		private void Platform_OnCollision(Platform platform)
		{
			if(PlatformsOnBigCP.IndexOf(platform) <= -1)
				PlatformsOnBigCP.Add(platform);
			PlatformsOnSmallCP.Add(platform);
        }


		/// <summary>
		/// 
		/// Nettoie les listes en fonction du booléen en paramètre
		/// 
		/// </summary>
		/// <param name="isSmall"></param>
		private void CheckpointManager_OnCheck(bool isSmall)
		{
			if (!isSmall) PlatformsOnBigCP.Clear();
			PlatformsOnSmallCP.Clear();
		}


		/// <summary>
		/// 
		/// parcourt la bonne liste en fonction du booléen afin de les initialiser 
		/// nettoie ensuite les lists qu'il faut vider
		/// 
		/// </summary>
		/// <param name="isSmallCheckPoint"></param>
		static public void RestartPlatforms(bool isSmallCheckPoint)
		{
			List<Platform> listToRestart = isSmallCheckPoint ? PlatformsOnSmallCP : PlatformsOnBigCP;

			foreach (Platform platform in listToRestart)
			{
				platform.init();
			}

			if(!isSmallCheckPoint)
				PlatformsOnBigCP.Clear();
			PlatformsOnSmallCP.Clear();
		}


		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		private void OnDestroy()
		{
			Platform.OnCollision -= Platform_OnCollision;
			CheckpointManager.OnCheck -= CheckpointManager_OnCheck;
		}
	}
}