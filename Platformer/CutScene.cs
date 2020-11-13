///-----------------------------------------------------------------
/// Author : Tom FALEMPIN
/// Date : 21/02/2020 19:38
///-----------------------------------------------------------------

using Com.DefaultCompany.Platformer.Platformer.Managers;
using Com.IsartDigital.Platformer.InteractableObjects.CollisionableObjects;
using Com.IsartDigital.Platformer.Managers;
using Com.IsartDigital.Platformer.Objects;
using Com.IsartDigital.Platformer.Platformer.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Com.IsartDigital.Platformer.UI {
	public class CutScene : Screen {
		//[SerializeField] private Button NextButton;
		[SerializeField] private TextMeshProUGUI text;
		[SerializeField] private GameObject ChoicePanel;
		[SerializeField] private TMP_FontAsset Angry_font;

		[SerializeField] private Button GiveButton;
		[SerializeField] private Button DontGiveButton;
		[SerializeField] private Button WaitButton;

		//private int AnimationIndex;

		public delegate void CutSceneEventHandler(CutScene CutScene);
		public static CutSceneEventHandler OnEnd;
		public static CutSceneEventHandler OnWin;

		private float typingSpeed = 0.005f;
		//private bool noMoreAnims;

		//private CutSceneSettings Settings;
		private int Index = 0;
		private int DialIndex = 0;
		private int DialGiveDontGive = 0;
		private List<string> Sentences;

		private bool IsInChoice;
		private bool isAngry;

		private int waitCount;
        private SoundManager soundManager;


        private void Awake()
        {
            soundManager = SoundManager.Instance;
        }

        private void Update()
		{
			if (!IsInChoice && Input.anyKeyDown && Sentences != null && text.text == Sentences[Index])
			{
				OnNext();
			}
		}

		public void SetIndex(int index)
		{
			DialIndex = index;
			Sentences = new List<string>();

			if (LocalizationManager.IsLocalizationLoaded)
				for (int i = 0; i < 10; i++)
				{
					if (LocalizationManager.Localization.ContainsKey("Dial" + LevelManager.CurrentLevelIndex + "_" + DialIndex + "_" + i))
						Sentences.Add(LocalizationManager.Localization["Dial" + LevelManager.CurrentLevelIndex + "_" + DialIndex + "_" + i]);
				}

			Debug.Log("Dial" + LevelManager.CurrentLevelIndex + "_" + DialIndex + "_");
			Debug.Log(Sentences.Count);

			StartCoroutine(DisplaySentence());
		}

		private IEnumerator DisplaySentence()
		{
			GetComponent<Animator>().SetTrigger("Change");
			text.text = "";
			foreach(char letter in Sentences[Index].ToCharArray())
			{
				text.text += letter;
                soundManager.Play(soundManager.Sounds.RaBlip);
                yield return new WaitForSeconds(typingSpeed);
			}
		}


		private void OnNext()
		{
			if (Index < Sentences.Count - 1)
			{
				Index++;
				if (Sentences[Index] == "Win")
				{
					OnEnd?.Invoke(this);
					OnWin?.Invoke(this);
					return;
				}
				else if (Sentences[Index] == "Choice" && !IsInChoice)
				{
					IsInChoice = true;
					Choice();
					text.text = "";

					return;
				}
				text.text = "";
				//if (isAngry) CameraShake.Instance.ScreenShake(0.4f, 15f);
				StartCoroutine(DisplaySentence());
				if (DialGiveDontGive == 3 && Index == 5) ApoRa.Instance.Transition();// HolorRa.Instance.chan
				else if (DialGiveDontGive == 2 && Index == 3) ApoRa.Instance.Transition();// HolorRa.Instance.tra
				if (isAngry) CameraShake.Instance.ScreenShake((float)Sentences[Index].Length * typingSpeed, 15);
			}
			else
			{
				OnEnd?.Invoke(this);
				if (isAngry) EndLevelTrigger.End();
			}
		}

		private void Choice()
		{
			ChoicePanel.SetActive(true);
			GiveButton.onClick.AddListener(Gived);
			DontGiveButton.onClick.AddListener(DontGived);
			if (waitCount < 3)
				WaitButton.onClick.AddListener(Wait);
			else WaitButton.gameObject.SetActive(false);
		}

		private void Wait()
		{
			waitCount++;
			if (waitCount > 0) CameraShake.Instance.ScreenShake(0.2f, 8f * (float)waitCount);
			if (waitCount >= 3)
			{
				text.font = Angry_font;
				isAngry = true;
			}
			string path = "DialWait_" + waitCount + "_";
			ContinueDialogue(path);

		}

		private void DontGived()
		{
			string path = "DialDontGive_";
			text.font = Angry_font;
			isAngry = true;
			UIManager.LosedGame = false;
			DialGiveDontGive = 2;
			ContinueDialogue(path);
		}

		private void Gived()
		{
			string path = "DialGived_";
			text.font = Angry_font;
			isAngry = true;
			UIManager.LosedGame = true;
			DialGiveDontGive = 3;
			ContinueDialogue(path);
		}

		private void ContinueDialogue(string path)
		{
			Index = 0;
			Sentences = new List<string>();
			if (LocalizationManager.IsLocalizationLoaded)
				for (int i = 0; i < 10; i++)
				{
					if (LocalizationManager.Localization.ContainsKey(path+ i))
						Sentences.Add(LocalizationManager.Localization[path + i]);
				}

			//("DialChoice" + IndexChoice + "_" + Index + "_" + i)
			ChoicePanel.SetActive(false);
			GiveButton.onClick.RemoveListener(Gived);
			DontGiveButton.onClick.RemoveListener(DontGived);
			WaitButton.onClick.RemoveListener(Wait);
			IsInChoice = false;
			Index = 0;
			//OnNext();
			//CameraShake.Instance.ScreenShake(0.4f, 15f);
			CameraShake.Instance.ScreenShake((float)Sentences[Index].Length * typingSpeed, 15);
			StartCoroutine(DisplaySentence());
            soundManager.Play(soundManager.Sounds.RaVoice);
        }
	}
}