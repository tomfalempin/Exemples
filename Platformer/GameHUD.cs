///-----------------------------------------------------------------
/// Author : Tom FALEMPIN
/// Date : 11/02/2020 14:41
///-----------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Com.DefaultCompany.Platformer.Platformer.Managers;
using Com.IsartDigital.Platformer.InteractableObjects.CollisionableObjects.Collectibles;
using Com.IsartDigital.Platformer.Objects;
using Com.IsartDigital.Platformer.Platformer.Managers;
using Com.IsartDigital.Platformer.PlayerScripts;
using UnityEngine;
using UnityEngine.UI;

namespace Com.IsartDigital.Platformer.UI {
	public class GameHUD : Screen {


		[Header("Buttons")]
		[SerializeField] private Button PauseButton;
		[SerializeField] private Button ResumeButton;
		[SerializeField] private Button RestartButton;
		[SerializeField] private Button QuitButton;

		[Header("Panels")]
		[SerializeField] private GameObject PausePanel;
		[SerializeField] private Sprite Level1Frame;
		[SerializeField] private Sprite Level2Frame;
		[SerializeField] private Image FrameToChange;

		[Header("Artefacts")]
		[SerializeField] private Image[] Artefacts;
		[SerializeField] private Sprite[] Sprites;
		[SerializeField] private GameObject MovingArtefact;

		[Header("Text")]
		[SerializeField] private Text ScoreText;

		[Header("Mobile")]
		[SerializeField] private List<GameObject> mobileControlsInput = new List<GameObject>();

		[Header("Prefab")]
		[SerializeField] private GameObject lifePrefab;

		[Header("Curve")]
		[SerializeField] private AnimationCurve curve;

		static public GameHUD Instance => instance;
		static private GameHUD instance;

		///LifeUI
		private GameObject LifeUI;
		//////////////////////////

		//BOOLEEN
		private bool IsInPause;
		///////////////////////
		///

		private float score;

		private List<float> toScores = new List<float>();
		private Coroutine currentUpdate = null;
		private int previousFontSize;
		Vector3 FinalPos;

		/// <summary>
		/// 
		/// Initialise l'HUD avec les events, le score, etc
		/// 
		/// </summary>
		override protected void Start () {

			if (instance == null) instance = this;
			else Destroy(gameObject);

			//LevelManager.UpdateScore += LevelManager_UpdateScore;
			PlayerPhysics.OnUpdateLife += PlayerPhysics_OnUpdateLife;
			CollectibleManager.UpdateArtefacts += CollectibleManager_UpdateArtefacts;

			foreach(Image art in Artefacts)
			{
				art.gameObject.SetActive(false);
			}

			foreach(GameObject obj in mobileControlsInput)
			{
				obj.SetActive(true);
			}

#if !UNITY_ANDROID && !UNITY_IOS
			foreach (GameObject obj in mobileControlsInput)
			{
				obj.SetActive(false);
			}
#endif

			score = 0;
			toScores = new List<float>();
			currentUpdate = null;
			ScoreText.text = score.ToString();
			previousFontSize = ScoreText.fontSize;
			ScoreText.fontSize = previousFontSize;
			notInAnim = false;

			PauseButton.onClick.AddListener(OnPauseButton);

			FrameToChange.sprite = LevelManager.CurrentLevelIndex == 1 ? Level1Frame : Level2Frame;

			base.Start();

			LifeUI = Instantiate(lifePrefab, transform.parent);
			LifeUI.transform.localScale = Vector3.one * 2;
			LifeUI.GetComponent<UILife>().Init(0, 100);

			LevelManager_UpdateScore();

			FinalPos = Artefacts[0].transform.position;
		}


		/// <summary>
		/// 
		/// Initiliase une UILife avec les paramètres passés
		/// 
		/// </summary>
		/// <param name="previousLife"></param>
		/// <param name="Life"></param>
		/// <param name="PlayerPos"></param>
		private void PlayerPhysics_OnUpdateLife(float previousLife, float Life)
		{
			LifeUI.GetComponent<UILife>().Init(previousLife, Life);
		}


		/// <summary>
		/// 
		/// Fait apparaitre temporairement les artefacts ramassés
		/// 
		/// </summary>
		private void CollectibleManager_UpdateArtefacts(int nbr, GameObject artefact)
		{
			StartCoroutine(UpdatrArtefacts(nbr, artefact));
		}

		private IEnumerator UpdatrArtefacts(int nbr, GameObject collectedArtefact)
		{
			foreach (Image item in Artefacts)
			{
				item.gameObject.SetActive(false);
			}

			for (int i = nbr - 1; i >= 0; i--)
			{
				Artefacts[i].gameObject.SetActive(true);
				Artefacts[i].sprite = Sprites[nbr - i - 1];
			}

			if (collectedArtefact != null)
			{
				Artefacts[0].transform.position = Camera.main.WorldToScreenPoint(collectedArtefact.transform.position);

				float ElapsedTime = 0;
				float ratio = 0;

				while (ElapsedTime <= 1.5f)
				{
					ratio = Mathf.Clamp(ElapsedTime / 1.5f, 0f, 1f);
					Artefacts[0].transform.position = Vector3.Lerp(Camera.main.WorldToScreenPoint(collectedArtefact.transform.position), FinalPos, curve.Evaluate(ratio));
					ElapsedTime += Time.fixedDeltaTime;
					yield return new WaitForFixedUpdate();
				}
			}

			foreach (Image item in Artefacts)
			{
				item.gameObject.SetActive(false);
			}

			for (int i = nbr - 1; i >= 0; i--)
			{
				Artefacts[i].gameObject.SetActive(true);
				Artefacts[i].sprite = Sprites[nbr - i - 1];
			}

			yield return null;
		}

		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		private void OnQuitButton()
		{
			IsInPause = false;
			ResumeButton.onClick.RemoveListener(OnResumeButton);
			QuitButton.onClick.RemoveListener(OnQuitButton);
			LevelManager.Resume();
			LevelManager.LeaveLevel();
		}


		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		private void OnResumeButton()
		{
			IsInPause = false;
			ResumeButton.onClick.RemoveListener(OnResumeButton);
			QuitButton.onClick.RemoveListener(OnQuitButton);
			PausePanel.SetActive(false);
			LevelManager.Resume();
		}


		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		private void OnRestartbutton()
		{
			IsInPause = false;
			PausePanel.SetActive(false);
			ResumeButton.onClick.RemoveListener(OnResumeButton);
			QuitButton.onClick.RemoveListener(OnQuitButton);
			RestartButton.onClick.RemoveListener(OnRestartbutton);

			score = 0;
			toScores = new List<float>();
			ScoreText.text = score.ToString();
			previousFontSize = ScoreText.fontSize;
			instance.GetComponent<Animator>().SetTrigger("spawn");
			LevelManager.StartLevel(); 
		}


		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		private void OnPauseButton()
		{
			IsInPause = true;
			LevelManager.Pause();
			PausePanel.SetActive(true);
			ResumeButton.onClick.AddListener(OnResumeButton);
			QuitButton.onClick.AddListener(OnQuitButton);
			RestartButton.onClick.AddListener(OnRestartbutton);
		}


		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Escape))
				if (IsInPause)
					OnResumeButton();
				else
					OnPauseButton();
		}


		/// <summary>
		/// 
		/// Update le Score
		/// 
		/// </summary>
		public static void LevelManager_UpdateScore()
		{
			instance.toScores.Add(LevelManager.Score);
			if (instance.currentUpdate == null) instance.currentUpdate = instance.StartCoroutine(instance.updateScore(instance.toScores[0]));			
		}

		public static void OnRespawn()
		{
			instance.toScores = new List<float>();
			instance.currentUpdate = null;
			instance.StopAllCoroutines();
			instance.ScoreText.text = LevelManager.Score.ToString();
			instance.ScoreText.fontSize = instance.previousFontSize;
			instance.notInAnim = false;
			instance.GetComponent<Animator>().SetTrigger("spawn");
		}



		private bool notInAnim;

		private IEnumerator updateScore(float toScore)
		{
			while (score < toScore)
			{
				if(ScoreText.fontSize <= 100) ScoreText.fontSize++;
				else if (!notInAnim)
				{
					notInAnim = true;
					GetComponent<Animator>().SetTrigger("shake");
				}

				score++;
				ScoreText.text = score.ToString();
				yield return new WaitForSeconds(0.05f);
			}

			toScores.RemoveAt(0);

			if (toScores.Count > 0) currentUpdate = StartCoroutine(updateScore(toScores[0]));
			else
			{
				currentUpdate = null;
				ScoreText.fontSize = previousFontSize;
				GetComponent<Animator>().SetTrigger("idle");
				notInAnim = false;
			}
		}
		
		public void Erase()
		{
			GetComponent<Animator>().SetTrigger("erase");
		}

		/// <summary>
		/// 
		/// 
		/// 
		/// </summary>
		private void OnDestroy()
		{
			instance.currentUpdate = null;
			if (instance == this) instance = null;

			StopAllCoroutines();
			toScores = new List<float>();
			//LevelManager.UpdateScore -= LevelManager_UpdateScore;
			PlayerPhysics.OnUpdateLife -= PlayerPhysics_OnUpdateLife;
			CollectibleManager.UpdateArtefacts -= CollectibleManager_UpdateArtefacts;
		}
	}
}