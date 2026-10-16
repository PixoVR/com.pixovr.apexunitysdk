#!/usr/bin/env python3
"""Creates Unity .meta files, with new GUIDs, for files and folders that lack one.

Usage: python3 "Tools~/make_metas.py" . Runtime Editor Tests

Native plugins (.dll, .so) are skipped: their .meta files carry import settings and are written by
Tools~/update_core.py.
"""
import os, sys, uuid
root = sys.argv[1]
TEMPLATES = {
    "folder": "fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
    ".cs": "fileFormatVersion: 2\nguid: {guid}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
    "text": "fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
    "default": "fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
}
TEXT_EXTENSIONS = {".md", ".txt", ".json", ".xml", ".h"}
def write_meta(path, kind):
    meta = path + ".meta"
    if os.path.exists(meta):
        return
    with open(meta, "w", newline="\n") as handle:
        handle.write(TEMPLATES[kind].format(guid=uuid.uuid4().hex))
    print("created", os.path.relpath(meta, root))
for directory in sys.argv[2:]:
    top = os.path.join(root, directory)
    if os.path.isfile(top):
        extension = os.path.splitext(top)[1]
        write_meta(top, ".cs" if extension == ".cs" else "text" if extension in TEXT_EXTENSIONS else "default")
        continue
    if not directory.endswith("~"):
        write_meta(top, "folder")
    for current, folders, files in os.walk(top):
        folders[:] = [name for name in folders if not name.startswith(".") and not name.endswith("~")]
        for name in folders:
            write_meta(os.path.join(current, name), "folder")
        for name in files:
            if name.endswith(".meta") or name.startswith("."):
                continue
            extension = os.path.splitext(name)[1]
            if extension in (".so", ".dll", ".bundle"):
                continue  # native plugins get importer settings written by hand
            write_meta(os.path.join(current, name), ".cs" if extension == ".cs" else "text" if extension in TEXT_EXTENSIONS else "default")
