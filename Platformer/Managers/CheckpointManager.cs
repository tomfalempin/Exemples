///-----------------------------------------------------------------
/// Author : Tom FALEMPIN
/// Date : 27/01/2020 16:58
///-----------------------------------------------------------------

using System;
using System.Collections.Generic;
using Com.IsartDigital.Platformer.InteractableObjects;
using Com.IsartDigital.Platformer.InteractableObjects.CollisionableObjects;
using UnityEngine;

namespace Com.IsartDigital.Platformer.Managers {
	public class CheckpointManager : MonoBehaviour {

		[Header("Position")]
		[SerializeField] private Transform StartingPosition;

		///POSITIONS DES CHECKPOINTS
		//
		static private Vector3 _smallCheckPointPosition;
		static private Vector3 _mainCheckPointPosition;
		//
		////////////////////////////////////////////////


		///EVENT HANDLER
		//
		public delegate void CheckpoinManagertEventHandler(bool isSmall);
		public static event CheckpoinManagertEventHandler OnCheck;
		//
		/////////////////////////////////////////////////////////////////

		///GETTER DES POSITIONS DE CHECKPOINTS
		//
		static public Vector3 SmallCheckPointPosition => _smallCheckPointPosition;
		static public Vector3 MainCheckPointPosition => _mainCheckPointPosition;
		//
		//////////////////////////////////////////////////////////////////////////

		private void Start() {

			Checkpoint.OnCheck += Checkpoint_OnCheck;
			Init();
		}


		/// <summary>
		/// 
		/// Initialise les positions de checkpoint avec la position de start
		/// 
		/// </summary>
		private void Init()
		{
			_smallCheckPointPosition = _mainCheckPointPosition = StartingPosition.position;
		}


		/// <summary>
		/// 
		/// la position du dernière petit checkpoint correspond à celle du dernier gros checkpoint
		/// 
		/// </summary>
		static public void RespawnAtPreviousBigCheckpoint()
		{
			_smallCheckPointPosition = _mainCheckPointPosition;
		}


		/// <summary>
		/// 
		/// permet de sauvegarder la nouvelle position de checkpoint en fonction de si le checkpoint
		/// checké est un petit ou un gros checkpoint
		/// 
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="isSmall"></param>
		static private void Checkpoint_OnCheck(Vector3 pos, bool isSmall)
		{
            if (!isSmall) SoundManager.Instance.Play(SoundManager.Instance.Sounds.Checkpoint);

            if (isSmall && pos != _smallCheckPointPosition)
			{
				_smallCheckPointPosition = pos;
				OnCheck?.Invoke(isSmall);
			}
			else if (!isSmall && pos != _mainCheckPointPosition)
			{
				_mainCheckPointPosition = pos;
				_smallCheckPointPosition = pos;
				OnCheck?.Invoke(isSmall);
			}

		}

		private void OnDestroy()
		{
			Checkpoint.OnCheck -= Checkpoint_OnCheck;
		}
	}
}