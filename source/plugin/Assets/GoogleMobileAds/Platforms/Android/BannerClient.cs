// Copyright (C) 2015 Google, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;
using UnityEngine.UI;

namespace GoogleMobileAds.Android
{
    public class BannerClient : AndroidJavaProxy, IBannerClient
    {
        protected internal AndroidJavaObject bannerView;

        protected internal BannerClient(string className) : base(className) {}

        public BannerClient() : base(Utils.UnityAdListenerClassName)
        {
            AndroidJavaClass playerClass = new AndroidJavaClass(Utils.UnityActivityClassName);
            AndroidJavaObject activity =
                    playerClass.GetStatic<AndroidJavaObject>("currentActivity");
            this.bannerView = new AndroidJavaObject(
                Utils.BannerViewClassName, activity, this);
        }

        public event EventHandler<EventArgs> OnAdLoaded;

        public event EventHandler<LoadAdErrorClientEventArgs> OnAdFailedToLoad;

        public event EventHandler<EventArgs> OnAdOpening;

        public event EventHandler<EventArgs> OnAdClosed;

        public event Action<AdValue> OnPaidEvent;

        public event Action OnAdClicked;

        public event Action OnAdImpressionRecorded;

        // Creates a banner view.
        public void CreateBannerView(string adUnitId, AdSize adSize, AdPosition position)
        {
            this.bannerView.Call(
                    "create",
                    new object[3] { adUnitId, Utils.GetAdSizeJavaObject(adSize), (int)position });
        }

        // Creates a banner view with a custom position.
        public void CreateBannerView(string adUnitId, AdSize adSize, int x, int y)
        {
            this.bannerView.Call(
                "create",
                new object[4] { adUnitId, Utils.GetAdSizeJavaObject(adSize), x, y });
        }

        // Loads an ad.
        public virtual void LoadAd(AdRequest request)
        {
            this.bannerView.Call("loadAd", Utils.GetAdRequestJavaObject(request));
        }

        // Displays the banner view on the screen.
        public void ShowBannerView()
        {
            this.bannerView.Call("show");
        }

        // Hides the banner view from the screen.
        public void HideBannerView()
        {
            this.bannerView.Call("hide");
        }

        // Destroys the banner view.
        public void DestroyBannerView()
        {
            this.bannerView.Call("destroy");
        }

        /// Returns the ad unit ID.
        public string GetAdUnitID()
        {
            return this.bannerView.Call<string>("getAdUnitId");
        }

        // Returns the height of the BannerView in pixels.
        public float GetHeightInPixels()
        {
            return this.bannerView.Call<float>("getHeightInPixels");
        }

        // Returns the width of the BannerView in pixels.
        public float GetWidthInPixels()
        {
            return this.bannerView.Call<float>("getWidthInPixels");
        }

        // Set the position of the banner view using standard position.
        public void SetPosition(AdPosition adPosition)
        {
            this.bannerView.Call("setPosition", (int)adPosition);
        }

        // Set the position of the banner view using custom position.
        public void SetPosition(int x, int y)
        {
            this.bannerView.Call("setPosition", x, y);
        }

        // Indicates whether the last loaded ad is a collapsible banner.
        public bool IsCollapsible()
        {
            return this.bannerView.Call<bool>("isCollapsible");
        }

        // A long integer provided by the AdMob UI for the configured placement.
        public long PlacementId
        {
            get
            {
                return this.bannerView.Call<long>("getPlacementId");
            }
            set
            {
                this.bannerView.Call("setPlacementId", value);
            }
        }

        public IResponseInfoClient GetResponseInfoClient()
        {
            var responseInfoJavaObject = bannerView.Call<AndroidJavaObject>("getResponseInfo");
            return new ResponseInfoClient(ResponseInfoClientType.AdLoaded, responseInfoJavaObject);
        }

        #region Callbacks from UnityBannerAdListener.

        public void onAdLoaded()
        {
            if (this.OnAdLoaded != null)
            {
                this.OnAdLoaded(this, EventArgs.Empty);
            }
        }

        public void onAdFailedToLoad(AndroidJavaObject error)
        {
            if (this.OnAdFailedToLoad != null)
            {
                LoadAdErrorClientEventArgs args = new LoadAdErrorClientEventArgs()
                {
                    LoadAdErrorClient = new LoadAdErrorClient(error)
                };
                this.OnAdFailedToLoad(this, args);
            }
        }

        public void onAdOpened()
        {
            if (this.OnAdOpening != null)
            {
                this.OnAdOpening(this, EventArgs.Empty);
            }
        }

        public void onAdClosed()
        {
            if (this.OnAdClosed != null)
            {
                this.OnAdClosed(this, EventArgs.Empty);
            }
        }

        public void onPaidEvent(int precision, long valueInMicros, string currencyCode)
        {
            if (this.OnPaidEvent != null)
            {
                AdValue adValue = new AdValue()
                {
                    Precision = (AdValue.PrecisionType)precision,
                    Value = valueInMicros,
                    CurrencyCode = currencyCode
                };
                this.OnPaidEvent(adValue);
            }
        }


        internal void onAdClicked()
        {
            if (this.OnAdClicked != null)
            {
                this.OnAdClicked();
            }
        }

        internal void onAdImpression()
        {
            if (this.OnAdImpressionRecorded != null)
            {
                this.OnAdImpressionRecorded();
            }
        }

        #endregion

        #region MREC用カスタム関数群
        public void CreateBannerView(string adUnitId, RectTransform target)
        {
            this.bannerView.Call(
                "create",
                new object[4] { adUnitId, Utils.GetAdSizeJavaObject(AdSize.MediumRectangle), 0, 0 });

            if (null != target)
            {
                CustomUpdatePosition(target);
            }
        }

        public void CustomUpdatePosition(RectTransform target)
        {
            ConvertRectToViewCenter(target, out float x, out float y, out float width, out _);
            CustomUpdatePosition(width, x, y);
        }

        // Update the position of the banner view using custom position and width.
        public void CustomUpdatePosition(float width, float x, float y)
        {
            this.bannerView.Call(
                "customUpdatePosition",
                new object[3] { width, x, y });
        }

        public static void ConvertRectToViewCenter(RectTransform rectTrans, out float centerX, out float centerY, out float width, out float height)
        {
            if (rectTrans == null)
            {
                Debug.LogError("RectTransformがnullです");
                centerX = 0;
                centerY = 0;
                width = 0;
                height = 0;
                return;
            }

            // Canvasを取得
            Canvas canvas = rectTrans.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvasが見つかりません");
                centerX = 0;
                centerY = 0;
                width = 0;
                height = 0;
                return;
            }

            // Canvas解像度とScreen解像度の比率を計算
            float canvasWidth = Screen.width;
            float canvasHeight = Screen.height;
            CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
            if (canvasScaler != null && canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                canvasWidth = canvasScaler.referenceResolution.x;
                canvasHeight = canvasScaler.referenceResolution.y;
            }
            float scaleX = canvasWidth / Screen.width;
            float scaleY = canvasHeight / Screen.height;

            // Safe Areaのオフセットを取得
            Rect safeArea = Screen.safeArea;
            float safeAreaTopOffset = Screen.height - safeArea.yMax;

            // ワールド座標での四隅を取得
            Vector3[] worldCorners = new Vector3[4];
            rectTrans.GetWorldCorners(worldCorners);

            Vector2[] screenCorners = new Vector2[4];

            // Overlay Canvasの場合は直接Screen座標を使用
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Overlayの場合、worldCornersは既にScreen座標
                for (int i = 0; i < 4; i++)
                {
                    screenCorners[i] = new Vector2(worldCorners[i].x, worldCorners[i].y);
                }
            }
            else
            {
                // Camera/World Spaceの場合はカメラを使って変換
                Camera camera = GetCanvasCamera(canvas);
                for (int i = 0; i < 4; i++)
                {
                    screenCorners[i] = RectTransformUtility.WorldToScreenPoint(camera, worldCorners[i]);
                }
            }

            // 最小・最大座標を取得
            float minX = Mathf.Min(screenCorners[0].x, screenCorners[1].x, screenCorners[2].x, screenCorners[3].x);
            float maxX = Mathf.Max(screenCorners[0].x, screenCorners[1].x, screenCorners[2].x, screenCorners[3].x);
            float minY = Mathf.Min(screenCorners[0].y, screenCorners[1].y, screenCorners[2].y, screenCorners[3].y);
            float maxY = Mathf.Max(screenCorners[0].y, screenCorners[1].y, screenCorners[2].y, screenCorners[3].y);

            // サイズ
            width = (maxX - minX);
            height = (maxY - minY);

            // Unity Screen座標での中心（左下原点）
            float screenCenterX = (minX + maxX) / 2f;
            float screenCenterY = (minY + maxY) / 2f;

            // Java View座標系に変換（左上原点、Y軸下向き
            centerX = screenCenterX;
            // Safe Areaのtopオフセット分を引く
            // centerY = (Screen.height - screenCenterY - safeAreaTopOffset);
            centerY = (Screen.height - screenCenterY);
        }

        /// <summary>
        /// Canvasに対応するカメラを取得
        /// </summary>
        private static Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null)
                return null;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }
            else if (canvas.worldCamera != null)
            {
                return canvas.worldCamera;
            }
            else
            {
                return Camera.main;
            }
        }

        #endregion
    }
}
