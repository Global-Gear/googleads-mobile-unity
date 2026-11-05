#!/usr/bin/env python3
"""exportPackage で作った unitypackage から、UPM パッケージの形を組み立てる。

使い方:
    python tools/build_upm_package.py <unitypackage> <出力先>

カレントの HEAD をソースのコミットとして扱う。出力先の中身は .git を除いて置き換える。
"""
import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile

PACKAGE_NAME = "com.google.ads.mobile"
SOURCE_ROOT = "source/plugin/Assets/"
REPOSITORY_URL = "https://github.com/Global-Gear/googleads-mobile-unity"
LEGACY_EDM_VERSION = "1.2.188"

INCLUDE_PREFIXES = ("Assets/GoogleMobileAds", "Assets/Plugins")
EXCLUDE_PATHS = (
    # 設定はアプリごとのものなので Assets に残す
    "Assets/GoogleMobileAds/Resources",
    # アプリ側の AndroidManifest.xml と重なる
    "Assets/Plugins/Android/AndroidManifest.xml",
)

# Assets 側にも同じ名前のフォルダが残るので、guid を重ねない
REGENERATE_GUID_FOLDERS = (
    "GoogleMobileAds",
    "Plugins",
    "Plugins/Android",
    "Plugins/iOS",
    "Plugins/Ump",
)

ROOT_FILE_META = """fileFormatVersion: 2
guid: {guid}
{importer}:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def git(*args):
    return subprocess.run(
        ["git", *args], check=True, capture_output=True, text=True, encoding="utf-8"
    ).stdout


def stable_guid(rel_path):
    return hashlib.md5(f"{PACKAGE_NAME}:{rel_path}".encode("utf-8")).hexdigest()


def is_excluded(path):
    return any(path == e or path.startswith(e + "/") for e in EXCLUDE_PATHS)


def read_entries(extracted):
    entries = {}
    for guid in os.listdir(extracted):
        pathname = os.path.join(extracted, guid, "pathname")
        if not os.path.exists(pathname):
            continue
        with open(pathname, encoding="utf-8") as f:
            path = f.read().split("\n")[0].strip()
        entries[path] = os.path.join(extracted, guid)
    return entries


def clear_output(out_dir):
    os.makedirs(out_dir, exist_ok=True)
    for name in os.listdir(out_dir):
        if name == ".git":
            continue
        target = os.path.join(out_dir, name)
        if os.path.isdir(target):
            shutil.rmtree(target)
        else:
            os.remove(target)


def write_meta(src, dst, rel_path):
    with open(src, encoding="utf-8", newline="") as f:
        text = f.read()
    if rel_path in REGENERATE_GUID_FOLDERS:
        text, count = re.subn(r"(?m)^guid: \w+", f"guid: {stable_guid(rel_path)}", text, 1)
        if count != 1:
            sys.exit(f"guid が見つかりません: {src}")
    with open(dst, "w", encoding="utf-8", newline="") as f:
        f.write(text)


def write_package_json(out_dir, revision):
    package = json.loads(git("show", f"HEAD:packages/{PACKAGE_NAME}/package.json"))
    package["displayName"] += " (Custom)"
    package.pop("samples", None)
    package.pop("publishConfig", None)
    # Firebase が旧 EDM に依存しているので、新しい EDM と併用しない
    dependencies = package["dependencies"]
    if dependencies.pop("com.unity.external-dependency-manager", None):
        dependencies["com.google.external-dependency-manager"] = LEGACY_EDM_VERSION
    package["repository"] = {"type": "git", "url": REPOSITORY_URL, "revision": revision}
    with open(os.path.join(out_dir, "package.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(package, f, ensure_ascii=False, indent=2)
        f.write("\n")
    return package["version"]


def write_root_files(out_dir):
    with open(os.path.join(out_dir, "LICENSE.md"), "w", encoding="utf-8", newline="\n") as f:
        f.write(git("show", "HEAD:LICENSE"))
    for name, importer in (
        ("package.json", "PackageManifestImporter"),
        ("LICENSE.md", "TextScriptImporter"),
    ):
        meta = ROOT_FILE_META.format(guid=stable_guid(name), importer=importer)
        with open(os.path.join(out_dir, name + ".meta"), "w", encoding="utf-8", newline="\n") as f:
            f.write(meta)


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("unitypackage")
    parser.add_argument("out_dir")
    args = parser.parse_args()

    revision = git("rev-parse", "HEAD").strip()
    source_files = {
        line[len(SOURCE_ROOT):]
        for line in git("ls-tree", "-r", "--name-only", "HEAD", SOURCE_ROOT).splitlines()
    }
    source_dirs = {
        "/".join(f.split("/")[:i]) for f in source_files for i in range(1, f.count("/") + 1)
    }

    with tempfile.TemporaryDirectory() as extracted:
        with tarfile.open(args.unitypackage, "r:gz") as tar:
            tar.extractall(extracted)
        entries = read_entries(extracted)

        clear_output(args.out_dir)
        copied, stale = 0, []
        for path, entry in sorted(entries.items()):
            if not path.startswith(INCLUDE_PREFIXES) or is_excluded(path):
                continue
            rel_path = path[len("Assets/"):]
            asset = os.path.join(entry, "asset")
            is_file = os.path.exists(asset)
            # 古いビルドの temp から混ざったファイルを落とす
            if rel_path not in (source_files if is_file else source_dirs):
                stale.append(rel_path)
                continue
            dst = os.path.join(args.out_dir, rel_path)
            if is_file:
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                shutil.copyfile(asset, dst)
                copied += 1
            else:
                os.makedirs(dst, exist_ok=True)
            meta = os.path.join(entry, "asset.meta")
            if os.path.exists(meta):
                write_meta(meta, dst + ".meta", rel_path)

    version = write_package_json(args.out_dir, revision)
    write_root_files(args.out_dir)

    print(f"version: {version}")
    print(f"source: {revision}")
    print(f"copied: {copied}")
    print(f"dropped: {len(stale)}")
    for path in stale:
        print(f"  {path}")


if __name__ == "__main__":
    main()
