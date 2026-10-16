#!/usr/bin/env python3
"""Copies an apex-sdk-core release's native libraries into the package.

    python3 "Tools~/update_core.py" v0.1.0               download the release from GitHub
    python3 "Tools~/update_core.py" v0.1.0 --zip FILE    use a release zip already on disk

Writes, with their Unity import settings:
    Plugins/Windows/x86_64/apex_core.dll         Windows x64 player and editor
    Plugins/Android/arm64-v8a/libapex_core.so    Android arm64 (Quest)
and records the tag in Plugins/ApexCoreVersion.txt.

apex-sdk-core is private, so downloading needs a GitHub token: GITHUB_TOKEN or GH_TOKEN, or
`gh auth token` when the GitHub CLI is logged in. The .meta GUIDs are derived from the file
paths, so rerunning the script never changes them.
"""
import hashlib
import io
import json
import os
import subprocess
import sys
import urllib.request
import zipfile

PACKAGE_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CORE_REPOSITORY = "PixoVR/apex-sdk-core"
VERSION_FILE = "Plugins/ApexCoreVersion.txt"

# Release zip path -> (package path, import settings)
LIBRARIES = {
    "lib/windows/x86_64/apex_core.dll": ("Plugins/Windows/x86_64/apex_core.dll", "windows"),
    "lib/android/arm64-v8a/libapex_core.so": ("Plugins/Android/arm64-v8a/libapex_core.so", "android"),
}

PLUGIN_META = """fileFormatVersion: 2
guid: {guid}
PluginImporter:
  externalObjects: {{}}
  serializedVersion: 2
  iconMap: {{}}
  executionOrder: {{}}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      : Any
    second:
      enabled: 0
      settings:
        Exclude Android: {exclude_android}
        Exclude Editor: {exclude_editor}
        Exclude Linux64: 1
        Exclude OSXUniversal: 1
        Exclude Win: 1
        Exclude Win64: {exclude_windows}
        Exclude iOS: 1
  - first:
      Android: Android
    second:
      enabled: {android_enabled}
      settings:
        CPU: ARM64
  - first:
      Any:
    second:
      enabled: 0
      settings: {{}}
  - first:
      Editor: Editor
    second:
      enabled: {editor_enabled}
      settings:
        CPU: x86_64
        DefaultValueInitialized: true
        OS: Windows
  - first:
      Standalone: Win64
    second:
      enabled: {windows_enabled}
      settings:
        CPU: x86_64
  userData:
  assetBundleName:
  assetBundleVariant:
"""

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

TEXT_META = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def stable_guid(package_path):
    """A GUID derived from the path, so reruns keep Unity's references stable."""
    return hashlib.md5(("com.pixovr.apexunitysdk/" + package_path).encode("utf-8")).hexdigest()


def write_text(package_path, text):
    full_path = os.path.join(PACKAGE_ROOT, package_path)
    os.makedirs(os.path.dirname(full_path), exist_ok=True)
    with open(full_path, "w", newline="\n") as output:
        output.write(text)


def write_folder_metas(package_path):
    folder = os.path.dirname(package_path)
    while folder and folder != "Plugins":
        if not os.path.exists(os.path.join(PACKAGE_ROOT, folder + ".meta")):
            write_text(folder + ".meta", FOLDER_META.format(guid=stable_guid(folder)))
        folder = os.path.dirname(folder)


def plugin_meta(package_path, platform):
    is_windows = platform == "windows"
    return PLUGIN_META.format(
        guid=stable_guid(package_path),
        exclude_android=0 if not is_windows else 1,
        exclude_editor=0 if is_windows else 1,
        exclude_windows=0 if is_windows else 1,
        android_enabled=0 if is_windows else 1,
        editor_enabled=1 if is_windows else 0,
        windows_enabled=1 if is_windows else 0,
    )


def github_token():
    for variable in ("GITHUB_TOKEN", "GH_TOKEN"):
        if os.environ.get(variable):
            return os.environ[variable]
    try:
        return subprocess.run(["gh", "auth", "token"], check=True, capture_output=True, text=True).stdout.strip()
    except (OSError, subprocess.CalledProcessError):
        return None


def github_request(url, accept):
    request = urllib.request.Request(url, headers={"Accept": accept, "User-Agent": "apex-unity-update-core"})
    token = github_token()
    if token:
        request.add_header("Authorization", "Bearer " + token)
    with urllib.request.urlopen(request) as response:
        return response.read()


def download_release_zip(tag):
    release = json.loads(github_request(
        f"https://api.github.com/repos/{CORE_REPOSITORY}/releases/tags/{tag}", "application/vnd.github+json"))
    asset_name = f"apex-sdk-core-{tag}.zip"
    for asset in release.get("assets", []):
        if asset["name"] == asset_name:
            print(f"Downloading {asset_name}")
            return github_request(asset["url"], "application/octet-stream")
    raise SystemExit(f"Release {tag} has no asset named {asset_name}.")


def main(arguments):
    if not arguments or arguments[0].startswith("-"):
        print(__doc__)
        return 1

    tag = arguments[0]
    if "--zip" in arguments:
        with open(arguments[arguments.index("--zip") + 1], "rb") as zip_file:
            zip_bytes = zip_file.read()
    else:
        zip_bytes = download_release_zip(tag)

    with zipfile.ZipFile(io.BytesIO(zip_bytes)) as release_zip:
        names = release_zip.namelist()
        for release_path, (package_path, platform) in LIBRARIES.items():
            matches = [name for name in names if name.endswith("/" + release_path)]
            if len(matches) != 1:
                raise SystemExit(f"The release zip has no {release_path}.")
            full_path = os.path.join(PACKAGE_ROOT, package_path)
            os.makedirs(os.path.dirname(full_path), exist_ok=True)
            with open(full_path, "wb") as library:
                library.write(release_zip.read(matches[0]))
            write_text(package_path + ".meta", plugin_meta(package_path, platform))
            write_folder_metas(package_path)
            print(f"{package_path}: {os.path.getsize(full_path)} bytes")

    write_text(VERSION_FILE, tag + "\n")
    if not os.path.exists(os.path.join(PACKAGE_ROOT, VERSION_FILE + ".meta")):
        write_text(VERSION_FILE + ".meta", TEXT_META.format(guid=stable_guid(VERSION_FILE)))
    print(f"apex-sdk-core {tag} installed. Check that ApexNative.kExpectedAbiMinor matches its Apex.h.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
