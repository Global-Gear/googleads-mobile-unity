# カスタム版の管理手順

このリポジトリは googleads-mobile-unity のフォークです。MREC の位置とサイズを外から指定するカスタマイズ (`CustomUpdatePosition`) を管理します。

## ブランチとタグ

| 種類 | 名前の例 | 内容 |
|---|---|---|
| ソースのブランチ | `custom/v11.4.0` | 公式のバージョンにカスタマイズを載せたもの |
| パッケージのブランチ | `upm` | UPM 用に組み立てた生成物だけを置く |
| タグ | `v11.4.0-Custom.0` | `upm` の上に付け、アプリから参照する |

同じバージョンを修正するときは、タグの番号を `.1`、`.2` と上げます。一度使ったタグは動かしません。

## 公式の新しいバージョンへの更新

1. `upstream` からタグを取得します。

   ```bash
   git fetch upstream --tags
   ```

2. 公式のバージョンからソースのブランチを作ります。

   ```bash
   git switch -c custom/v11.5.0 v11.5.0
   ```

3. 前のバージョンのソースのブランチから、公式のバージョンより後のコミットを `cherry-pick` します。
4. 下の手順で unitypackage を作り、UPM パッケージを組み立てます。

## unitypackage の生成

- 環境変数 `UNITY_EXE` に Unity.exe のパスを指定します。
- プロジェクト直下に `temp` フォルダがあると `exportPackage` は失敗します。実行の前に `temp` フォルダを削除してください。
- Android Studio で Gradle > Tasks > other > `exportPackage` を実行します。

## UPM パッケージの組み立て

1. `upm` ブランチの作業フォルダを用意します。

   ```bash
   git worktree add ../googleads-mobile-unity-upm upm
   ```

2. ソースのブランチで、スクリプトを実行します。出力先の中身は `.git` を除いて置き換わります。

   ```bash
   python tools/build_upm_package.py GoogleMobileAds.unitypackage ../googleads-mobile-unity-upm
   ```

3. 出力の `dropped` を確認します。unitypackage にあってソースに無いファイルは、古いビルドの残りとしてパッケージから外します。
4. アプリの `Packages/` に置いて、Android と iOS のビルドと MREC の表示を確認します。
5. `upm` ブランチにコミットしてタグを付け、ブランチとタグをプッシュします。

スクリプトは次の処理も行います。

- `Assets/GoogleMobileAds/Resources` と `Assets/Plugins/Android/AndroidManifest.xml` をパッケージに入れません。
- `GoogleMobileAds`、`Plugins`、`Plugins/Android`、`Plugins/iOS`、`Plugins/Ump` のフォルダの guid を作り直します。アプリの `Assets` に同じ名前のフォルダが残るためです。
- `package.json` の `repository.revision` に、ソースのコミットを記録します。
- `package.json` の依存の `com.unity.external-dependency-manager` を `com.google.external-dependency-manager` に置き換えます。Firebase が `com.google.external-dependency-manager` に依存しているためです。

## アプリでの使い方

`Packages/manifest.json` に次の行を追加します。アダプターは OpenUPM のバージョンを指定します。

```json
"com.google.ads.mobile": "ssh://git@github.com/Global-Gear/googleads-mobile-unity.git#v11.4.0-Custom.0"
```

アプリの `Assets` からは本体のファイルを削除します。`Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset` はアプリの設定なので残します。
