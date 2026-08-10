// Copyright (C) 2020 Google LLC.
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
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GoogleMobileAds.Unity
{
    public class BannerClient : IBannerClient
    {
        public event Action<AdValue> OnPaidEvent;
        public event Action OnAdClicked;
        public event Action OnAdImpressionRecorded;
        public event Action OnAdLoaded;
        public event Action<LoadAdErrorClientEventArgs> OnAdFailedToLoad;
        public event Action OnAdOpening;
        public event Action OnAdClosed;
        public long PlacementId { get; set; }

        private bool _isHidden = false;
        private bool _didImpress = false;
        private bool _loaded = false;
        private GameObject _gameObject;
        private GameObject _prefab;
        private string _adUnitId;

        protected internal Dictionary<AdSize, string> _prefabAds = new Dictionary<AdSize, string>()
        {
            {AdSize.Banner, "PlaceholderAds/Banners/BANNER"},
            {AdSize.SmartBanner, "PlaceholderAds/Banners/SMART_BANNER" },
            {AdSize.MediumRectangle, "PlaceholderAds/Banners/MEDIUM_RECTANGLE" },
            {AdSize.IABBanner, "PlaceholderAds/Banners/FULL_BANNER" },
            {AdSize.Leaderboard, "PlaceholderAds/Banners/LEADERBOARD" },
            {new AdSize (320,100), "PlaceholderAds/Banners/LARGE_BANNER" }
        };

        public void CreateBannerView(string adUnitId, AdSize adSize, AdPosition position)
        {
            if(CreateBannerView(adSize, position))
            {
                _adUnitId = adUnitId;

                if (adSize == AdSize.SmartBanner || adSize.AdType == AdSize.Type.AnchoredAdaptive || adSize.AdType == AdSize.Type.LargeAnchoredAdaptive)
                {
                    SetAndStretchAd(position, adSize);
                }
                else
                {
                    AnchorAd(position);
                }

                _gameObject.SetActive(false);
            }
        }

        public void CreateBannerView(string adUnitId, AdSize adSize, int x, int y)
        {
            if(CreateBannerView(adSize, null))
            {
                _adUnitId = adUnitId;
                RectTransform rect = GetRectTransform();
                if (adSize == AdSize.SmartBanner || adSize.AdType == AdSize.Type.AnchoredAdaptive || adSize.AdType == AdSize.Type.LargeAnchoredAdaptive)
                {
                    SetAndStretchAd(0, adSize);
                    rect.anchoredPosition = new Vector3(0, y, 1);
                }
                else
                {
                    AnchorAd(x, y);
                }

                _gameObject.SetActive(false);
            }
        }

        bool CreateBannerView(AdSize adSize, AdPosition? position)
        {
            string prefabName;

            if (adSize == AdSize.SmartBanner || adSize.AdType == AdSize.Type.AnchoredAdaptive || adSize.AdType == AdSize.Type.LargeAnchoredAdaptive)
            {
                if (position.HasValue && position.Value == AdPosition.Center)
                {
                     prefabName = "PlaceholderAds/Banners/CENTER";
                }
                else
                {
                    prefabName = "PlaceholderAds/Banners/ADAPTIVE";
                }
            }
            else if(!_prefabAds.TryGetValue(adSize, out prefabName))
            {
                Debug.LogError("No Prefab found");
                return false;
            }
            _prefab = Resources.Load(prefabName) as GameObject;
            if (_prefab == null)
            {
                Debug.LogError("No Prefab found");
                return false;
            }
            _gameObject = GameObject.Instantiate(_prefab);
            Object.DontDestroyOnLoad(_gameObject);

            // Setting the maximum sortingOrder ensures highest priority for rendering the ad.
            Canvas canvas = _gameObject.GetComponent<Canvas>();
            if (canvas != null)
            {
                // sortingOrder is a 16 bit int so the maximum value is 32767.
                canvas.sortingOrder = 32767;
            }

            // Wire button.
            var button = _gameObject.GetComponentInChildren<Button>();
            button.onClick.AddListener(() =>
            {
                if (OnAdClicked != null)
                {
                    OnAdClicked();
                }
                if (OnAdOpening != null)
                {
                    OnAdOpening();
                }
                Application.OpenURL("https://google.com");
                if (OnPaidEvent != null)
                {
                    OnPaidEvent(new AdValue{
                        Value = 0,
                        CurrencyCode = "Google",
                        Precision = AdValue.PrecisionType.Unknown});
                }
                if (OnAdClosed != null)
                {
                    OnAdClosed();
                }
            });

            return true;
        }

        public void LoadAd(AdRequest request)
        {
            if (_gameObject == null)
            {
                return;
            }

            if (request == null || string.IsNullOrEmpty(_adUnitId))
            {
                if(OnAdFailedToLoad != null)
                {
                    OnAdFailedToLoad.Invoke(new LoadAdErrorClientEventArgs {
                            LoadAdErrorClient = new LoadAdErrorClient()
                        });
                }
            }
            else
            {
                _loaded = true;
                _didImpress = false;

                if(OnAdLoaded != null)
                {
                    OnAdLoaded.Invoke();
                }

                if (!_isHidden)
                {
                    _gameObject.SetActive(true);

                    if(OnAdImpressionRecorded != null)
                    {
                        OnAdImpressionRecorded();
                    }
                    _didImpress = true;
                }
            }
        }

        public void ShowBannerView()
        {
            if(_gameObject != null)
            {
                _gameObject.SetActive(_loaded);

                if (!_didImpress)
                {
                    if(OnAdImpressionRecorded != null)
                    {
                        OnAdImpressionRecorded();
                    }
                }
            }
            _isHidden = false;
        }

        public void HideBannerView()
        {
            if(_gameObject != null)
            {
                _gameObject.SetActive(false);
            }
            _isHidden = true;
        }

        public void DestroyBannerView()
        {
            if (_gameObject != null)
            {
                if (Application.isPlaying)
                {
                    GameObject.Destroy(_gameObject);
                }
                else
                {
                    GameObject.DestroyImmediate(_gameObject);
                }
                _gameObject = null;
            }
        }

        public string GetAdUnitID()
        {
            return _adUnitId;
        }

        public float GetHeightInPixels()
        {
            var rect = GetRectTransform();
            return rect.sizeDelta.y;
        }

        public float GetWidthInPixels()
        {
            var rect = GetRectTransform();
            return rect.sizeDelta.x;
        }

        public void SetPosition(AdPosition adPosition)
        {
            if (_gameObject != null)
            {
                AnchorAd(adPosition);
            }
        }

        public void SetPosition(int x, int y)
        {
            if (_gameObject != null)
            {
                AnchorAd(x, y);
            }
        }

        public bool IsCollapsible()
        {
            return false;
        }

        public IResponseInfoClient GetResponseInfoClient()
        {
            return new ResponseInfoClient();
        }

        private RectTransform GetRectTransform()
        {
            Image myImage = _gameObject.GetComponentInChildren<Image>();
            if (myImage != null)
            {
                return myImage.GetComponent<RectTransform>();
            }
            return null;
        }

        private void SetAndStretchAd(AdPosition pos, AdSize adSize)
        {
            if (_gameObject != null)
            {
                RectTransform rect = GetRectTransform();

                rect.pivot = new Vector2(0.5f, 0.5f);

                Text adText = _gameObject.GetComponentInChildren<Text>();
                if (adText != null)
                {
                    if (adSize.AdType == AdSize.Type.AnchoredAdaptive)
                    {
                        adText.text = "This is a Test Adaptive Banner";
                    }
                    else if (adSize.AdType == AdSize.Type.LargeAnchoredAdaptive)
                    {
                        adText.text = "This is a Test Large Adaptive Banner";
                    }
                    else if (adSize == AdSize.SmartBanner)
                    {
                        adText.text = "This is a Test Smart Banner";
                    }
                }

                if (pos == AdPosition.Bottom || pos == AdPosition.BottomLeft || pos == AdPosition.BottomRight)
                {
                    rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, rect.sizeDelta.y);
                    rect.anchoredPosition = new Vector2(0, (float)rect.sizeDelta.y/2);
                }
                else if (pos == AdPosition.Top || pos == AdPosition.TopLeft || pos == AdPosition.TopRight)
                {
                    rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, rect.sizeDelta.y);
                    rect.anchoredPosition = new Vector2(0, -(float)rect.sizeDelta.y/2);
                }
                else if (pos == AdPosition.Center)
                {
                    if (adSize.AdType != AdSize.Type.AnchoredAdaptive &&
                        adSize.AdType != AdSize.Type.LargeAnchoredAdaptive &&
                        adSize != AdSize.SmartBanner)
                    {
                        rect.anchoredPosition = new Vector2(0, 0);
                    }
                }
                else
                {
                    rect.anchoredPosition = rect.position;
                }
            }
            else
            {
                Debug.Log("Invalid Placeholder Ad");
            }
        }

        private void AnchorAd(int x, int y)
        {
            var rect = GetRectTransform();
            // Account for banner size and refactor coordinates
            float xWithOffset = (float)rect.sizeDelta.x/2 + x;
            float yWithOffset = (float)rect.sizeDelta.y/2 + y;

            // Anchor the banner relative to the top left
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(xWithOffset, -yWithOffset);
        }

        private void AnchorAd(AdPosition position)
        {
            if (_gameObject != null) 
            {
                Image myImage = _gameObject.GetComponentInChildren<Image>();
                RectTransform rect = myImage.GetComponentInChildren<RectTransform>();

                float x = (float)rect.sizeDelta.x/2;
                float y = (float)rect.sizeDelta.y/2;

                switch (position)
                {
                    case (AdPosition.TopLeft):
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchorMin = new Vector2(0, 1);
                        rect.anchorMax = new Vector2(0, 1);
                        rect.anchoredPosition = new Vector2(x, -y);
                        break;
                    case (AdPosition.TopRight):
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchorMin = new Vector2(1, 1);
                        rect.anchorMax = new Vector2(1, 1);
                        rect.anchoredPosition = new Vector2(-x, -y);
                        break;
                    case (AdPosition.Top):
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchorMin = new Vector2(0.5f, 1);
                        rect.anchorMax = new Vector2(0.5f, 1);
                        rect.anchoredPosition = new Vector2(0, -y);
                        break;
                    case (AdPosition.Bottom):
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchorMin = new Vector2(0.5f, 0);
                        rect.anchorMax = new Vector2(0.5f, 0);
                        rect.anchoredPosition = new Vector2(0, y);
                        break;
                    case (AdPosition.BottomRight):
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchorMin = new Vector2(1, 0);
                        rect.anchorMax = new Vector2(1, 0);
                        rect.anchoredPosition = new Vector2(-x, y);
                        break;
                    case (AdPosition.BottomLeft):
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchorMin = new Vector2(0, 0);
                        rect.anchorMax = new Vector2(0, 0);
                        rect.anchoredPosition = new Vector2(x, y);
                        break;
                    default:
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.anchoredPosition = new Vector2(0, 0);
                        break;
                }
            }
            else
            {
                Debug.Log("Invalid Placeholder Ad");
            }
        }

        #region MREC用カスタム関数群

        public void CreateBannerView(string adUnitId, RectTransform target)
        {
            if(CreateBannerView(AdSize.MediumRectangle, null))
            {
                _adUnitId = adUnitId;

                if (target != null)
                {
                    CustomUpdatePosition(target);
                }

                _gameObject.SetActive(false);
            }
        }

        public void CustomUpdatePosition(RectTransform target)
        {
            RectTransform self = GetRectTransform();

            MatchImageTransform(target,self);
        }

        /// <summary>
        /// スクリーンピクセル指定でプレースホルダーの位置とサイズを更新する
        /// </summary>
        /// <remarks>
        /// 引数の意味は Android / iOS 版と同一。x, y は左上原点・Y軸下向きのスクリーン座標
        /// (Platforms/Android/BannerClient.cs の ConvertRectToViewCenter が出力する形式) で、
        /// バナーの中心を指す。高さは実機と同じく MREC (300x250) を等倍スケールして求める。
        /// </remarks>
        /// <param name="width">バナーの幅 (スクリーンピクセル)</param>
        /// <param name="x">バナー中心のX座標 (スクリーンピクセル、左原点)</param>
        /// <param name="y">バナー中心のY座標 (スクリーンピクセル、上原点・下向き)</param>
        public void CustomUpdatePosition(float width, float x, float y)
        {
            if (_gameObject == null)
            {
                Debug.LogError("CustomUpdatePosition: バナーが未生成です。無視します。");
                return;
            }

            if (width <= 0f)
            {
                Debug.LogError("CustomUpdatePosition: width が 0 以下です。無視します。");
                return;
            }

            RectTransform self = GetRectTransform();
            if (self == null)
            {
                Debug.LogError("プレースホルダーのRectTransformが見つかりません");
                return;
            }

            Canvas canvas = self.GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                Debug.LogError("プレースホルダーのImageがCanvas配下にありません");
                return;
            }

            RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
            Camera camera = GetCanvasCamera(canvas);

            // 実機側は 300x250 のビューを等倍スケールするだけなので、高さは幅から求める
            AdSize mrec = AdSize.MediumRectangle;
            float height = width * mrec.Height / mrec.Width;

            // 左上原点・Y軸下向き -> Unityのスクリーン座標 (左下原点・Y軸上向き)
            Vector2 screenCenter = new Vector2(x, Screen.height - y);
            Vector2 halfSize = new Vector2(width, height) / 2f;

            // Canvasのローカル座標に変換する (CanvasScalerによる拡縮もここで吸収される)
            Vector2 localMin;
            Vector2 localMax;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform, screenCenter - halfSize, camera, out localMin) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform, screenCenter + halfSize, camera, out localMax))
            {
                Debug.LogError("スクリーン座標をCanvas座標に変換できませんでした");
                return;
            }

            Vector2 localCenter = (localMin + localMax) / 2f;
            Vector2 localSize = new Vector2(Mathf.Abs(localMax.x - localMin.x),
                                            Mathf.Abs(localMax.y - localMin.y));

            // アンカーを中央に設定
            self.anchorMin = new Vector2(0.5f, 0.5f);
            self.anchorMax = new Vector2(0.5f, 0.5f);
            self.pivot = new Vector2(0.5f, 0.5f);

            // 位置とサイズを設定
            // anchoredPositionは親のpivotに依存するため、ワールド座標を経由して中心を合わせる
            self.sizeDelta = localSize;
            self.position = canvasRectTransform.TransformPoint(localCenter);
        }

        /// <summary>
        /// A側のImageに合わせて、B側のImageの位置とサイズを設定する
        /// </summary>
        /// <param name="sourceRectTransform">基準となるA側のRectTransform</param>
        /// <param name="targetRectTransform">合わせたいB側のRectTransform</param>
        public static void MatchImageTransform(RectTransform sourceRectTransform, RectTransform targetRectTransform)
        {
            if (sourceRectTransform == null || targetRectTransform == null)
            {
                Debug.LogError("RectTransformがnullです");
                return;
            }

            // B側のCanvasを取得
            Canvas targetCanvas = targetRectTransform.GetComponentInParent<Canvas>(true);
            if (targetCanvas == null)
            {
                Debug.LogError("B側のImageがCanvas配下にありません");
                return;
            }

            // A側のワールド座標での四隅を取得
            Vector3[] worldCorners = new Vector3[4];
            sourceRectTransform.GetWorldCorners(worldCorners);

            // A側のCanvasとカメラを取得
            Canvas sourceCanvas = sourceRectTransform.GetComponentInParent<Canvas>();
            Camera sourceCamera = GetCanvasCamera(sourceCanvas);

            // B側のCanvasのカメラとRectTransformを取得
            Camera targetCamera = GetCanvasCamera(targetCanvas);
            RectTransform canvasRectTransform = targetCanvas.GetComponent<RectTransform>();

            // ワールド座標をB側のCanvas座標系に変換
            Vector2[] localCorners = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldCorners[i]);
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    screenPoint,
                    targetCamera,
                    out localPoint
                );
                localCorners[i] = localPoint;
            }

            // 中心座標を計算
            Vector2 center = (localCorners[0] + localCorners[2]) / 2f;

            // サイズを計算
            float width = Vector2.Distance(localCorners[0], localCorners[3]);
            float height = Vector2.Distance(localCorners[0], localCorners[1]);

            // アンカーを中央に設定
            targetRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            targetRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            targetRectTransform.pivot = new Vector2(0.5f, 0.5f);

            // 位置とサイズを設定
            targetRectTransform.anchoredPosition = center;
            targetRectTransform.sizeDelta = new Vector2(width, height);
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