# README

## unitypackage 生成

- 環境変数に「UNITY_EXE」を作成し、Unity.exe のパスを指定する。  
  例) C:\Program Files\Unity\Hub\Editor\2021.3.12f1\Editor\Unity.exe

- Android Studio で googleads-mobile-unity プロジェクトを開き、Gradle > Tasks > other > exportPackage を実行する。

- プロジェクト直下に temp フォルダが存在すると exportPackage は実行失敗する。  
  exportPackage 前に temp フォルダを削除すること。  
  (build.gradle の末尾の clearTempBuildFolder の実行をコメントインすれば解消されるが、Unity.exe 実行エラー時のログが temp 内にありそれがエラー時に削除されると調査できないので、コメントアウトしたままのほうがいいかも)
