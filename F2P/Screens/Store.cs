///-----------------------------------------------------------------
/// Author : Tom FALEMPIN
/// Date : 21/04/2020 10:42
///-----------------------------------------------------------------

using Com.IsartDigital.F2P.Managers;
using Com.IsartDigital.F2P.Monetization;
using Com.IsartDigital.F2P.Screens.Stores.OfferCards;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Com.IsartDigital.F2P.Screens.Stores {
	public class Store : AScreen {
		// ============================================================================
		//							  ***** SINGLETON *****
		// ============================================================================
		private static Store instance;
		public static Store Instance { get { return instance; } }

		private void Awake() {
			if (instance) {
				Destroy(gameObject);
				return;
			}

			instance = this;
		}

		// ============================================================================
		//							  ***** DESTROY *****
		// ============================================================================
		private void OnDestroy() {
			if (this == instance) instance = null;
		}

		// ============================================================================
		//							  ***** INSTANCE *****
		// ============================================================================


		[SerializeField] private GameObject ConfirmPurchasedPanel = default;
		[SerializeField] private Button ConfirmPurchasedButton = default;
		[SerializeField] private Button CancelPurchasedButton = default;

		private List<GameObject> offerCards = new List<GameObject>();
		private bool alreadyLoaded;

		private OfferCard currentOffer;

		protected override void Start()
		{
			base.Start();
			ConfirmPurchasedButton.onClick.AddListener(ConfirmPurchase);
			CancelPurchasedButton.onClick.AddListener(CancelPurchase);

			MyIAPManager.Instance.OnPurchaseFail += PurchaseFailed;
			MyIAPManager.Instance.OnPurchaseSuccess += PurchaseSucced;

		}

		private void PurchaseSucced(string id)
		{
			currentOffer.PurchaseConfirm();
			KPIManager.SendTransactionEvent(currentOffer.productName, 1, currentOffer.currency, (int)currentOffer.price);
			ConfirmPurchasedPanel.SetActive(false);
		}

		private void PurchaseFailed(string arg1, PurchaseFailureReason arg2)
		{
			Debug.Log(arg2);
			ConfirmPurchasedPanel.SetActive(false);
		}

		private void CancelPurchase()
		{
			PlayBackSound();
			//throw new NotImplementedException();
			ConfirmPurchasedPanel.SetActive(false);
		}

		private void ConfirmPurchase()
		{
			//throw new NotImplementedException();
			//ConfirmPurchasedPanel.SetActive(false);
			PlayImportantClickSound();

			float price = currentOffer.price;
			bool isInGameCurrencies = currentOffer.isInGameCurrencies;
			bool isSoftCurrencies = currentOffer.isSoftCurrency;


			if (isInGameCurrencies)
			{
				bool canBuyIt = GameManager.PlayerDatas.CheckIfEnoughCurrency((int)price, isSoftCurrencies);

				if (canBuyIt) PurchaseSucced(currentOffer.productName); //MyIAPManager.Instance.BuyProduct(currentOffer.productName);
				else Debug.Log("nope");
				//else currentOffer.PurchaseConfirm();

				//ConfirmPurchasedPanel.SetActive(!canBuyIt);
				//if (canBuyIt) PlaySoundUI(settings.PurchaseSound);
			}
			else MyIAPManager.Instance.BuyProduct(currentOffer.productName);
		}

		public void PurchasedSquad()
		{
			PlaySoundUI(settings.GainUnitSound);
		}


		public void ShowConfirmPurchasePanel(OfferCard offerCard)
		{
			currentOffer = offerCard;
			ConfirmPurchasedPanel.SetActive(true);
		}

		public override void ActivateScreen()
		{
			base.ActivateScreen();
			//MyIAPManager.Instance.Initialised();
			MenuHUD.Instance.HideLowerButtonsPannel(false);
		}
	}
}