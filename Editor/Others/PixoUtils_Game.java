package com.pixovr.pixosdk;

import android.app.Activity;
import android.app.ActivityManager;
import android.app.ActivityManager.AppTask;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.ContentResolver;
import android.content.ContentValues;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.database.Cursor;
import android.provider.MediaStore;
import android.net.Uri;
import android.os.Bundle;
import android.os.Build;
import android.os.Environment;
import android.util.Log;
import android.util.TimingLogger;
import android.graphics.Bitmap;
import android.graphics.drawable.Drawable;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.Canvas;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerGameActivity;
import java.util.*;
import java.io.BufferedReader;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;

public class PixoUtils {

    Context mContext;
    ActivityManager mActivityManager;

    public PixoUtils(Context context) {
        mContext = context;
        mActivityManager = (ActivityManager) context.getSystemService(Context.ACTIVITY_SERVICE);
    }

    public String getAppFileLocation(String packageName) {
        try {
            PackageManager packageManager = mContext.getPackageManager();
            ApplicationInfo applicationInfo = packageManager.getApplicationInfo(packageName, 0);
            return applicationInfo.sourceDir;
        } catch (PackageManager.NameNotFoundException e) {
            Log.e("PixoUtils", "Failed to get package location.");
            e.printStackTrace();
            return null;
        }
    }

    public boolean fileExists(String fileName) {
        ContentResolver resolver = mContext.getContentResolver();
        Uri contentUri = MediaStore.Files.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY);
        String[] projection = {MediaStore.MediaColumns._ID};
        String selection = MediaStore.MediaColumns.DISPLAY_NAME + "=?";
        String[] selectionArgs = {fileName};

        try (Cursor cursor = resolver.query(contentUri, projection, selection, selectionArgs, null)) {
            return (cursor != null && cursor.moveToFirst());
        }
    }

    public Uri getFileUri(String fileName) {
        ContentResolver resolver = mContext.getContentResolver();
        Uri contentUri = MediaStore.Files.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY);
        String[] projection = {MediaStore.MediaColumns._ID};
        String selection = MediaStore.MediaColumns.DISPLAY_NAME + "=?";
        String[] selectionArgs = {fileName};

        try (Cursor cursor = resolver.query(contentUri, projection, selection, selectionArgs, null)) {
            if (cursor != null && cursor.moveToFirst()) {
                int idColumn = cursor.getColumnIndexOrThrow(MediaStore.MediaColumns._ID);
                long id = cursor.getLong(idColumn);
                return Uri.withAppendedPath(contentUri, String.valueOf(id));
            }
        }
        return null;
    }

    public String writeFileToSharedStorage(String fileName, String content) {
        ContentResolver resolver = mContext.getContentResolver();
        Uri fileUri = getFileUri(fileName);

        if(fileUri == null)
        {
            ContentValues contentValues = new ContentValues();
            contentValues.put(MediaStore.MediaColumns.DISPLAY_NAME, fileName);
            contentValues.put(MediaStore.MediaColumns.MIME_TYPE, "text/plain");

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                contentValues.put(MediaStore.MediaColumns.RELATIVE_PATH, Environment.DIRECTORY_DOCUMENTS);
            } else {
                contentValues.put(MediaStore.MediaColumns.DATA, Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOCUMENTS).getPath() + "/" + fileName);
            }
            fileUri = resolver.insert(MediaStore.Files.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY), contentValues);
        }

        if (fileUri != null) {
            try (OutputStream os = resolver.openOutputStream(fileUri)) {
                if (os != null) {
                    os.write(content.getBytes());
                    return fileUri.toString();
                }
            } catch (IOException e) {
                Log.e("PixoUtils", "Failed to write to file.");
                e.printStackTrace();
            }
        }
        return null;
    }

    public String readFileFromSharedStorage(String fileName) {
        ContentResolver resolver = mContext.getContentResolver();
        Uri fileUri = getFileUri(fileName);

        if (fileUri != null) {
            try (InputStream is = resolver.openInputStream(fileUri);
                 BufferedReader reader = new BufferedReader(new InputStreamReader(is))) {
                
                StringBuilder stringBuilder = new StringBuilder();
                String line;
                while ((line = reader.readLine()) != null) {
                    stringBuilder.append(line).append("\n");
                }
                
                // Remove the last newline if it exists
                if (stringBuilder.length() > 0) {
                    stringBuilder.setLength(stringBuilder.length() - 1);
                }
                
                return stringBuilder.toString();
            } catch (IOException e) {
                Log.e("PixoUtils", "Failed to read file.");
                e.printStackTrace();
            }
        }
        return null;
    }

    public boolean deleteFileFromSharedStorage(String fileName) {
        ContentResolver resolver = mContext.getContentResolver();
        Uri fileUri = getFileUri(fileName);

        if (fileUri != null) {
            try {
                // For Android 10 (API 29) and above
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                    // Delete the file using the file's content URI
                    return resolver.delete(fileUri, null, null) > 0;
                } else {
                    // For Android 9 (Pie) and below
                    // Check if we have permission to delete the file
                    if (mContext.checkSelfPermission(android.Manifest.permission.WRITE_EXTERNAL_STORAGE)
                            == android.content.pm.PackageManager.PERMISSION_GRANTED) {
                        return resolver.delete(fileUri, null, null) > 0;
                    } else {
                        throw new SecurityException("Permission WRITE_EXTERNAL_STORAGE is required to delete files on Android 9 and below.");
                    }
                }
            } catch (SecurityException e) {
                e.printStackTrace();
                return false;
            }
        }
        return false; // File not found
    }

    public boolean launchApp(String packageName, String[] extraKey, String[] extraValue)
    {
        Log.e("PixoUtils", " AndroidThunkJava_Launch");
        Intent intent;
        String intentAction = "";
        String intentComponent = "";

        int intentActionIndex = packageName.indexOf(":");
        if (intentActionIndex >= 0)
        {
            intentAction = packageName.substring(intentActionIndex + 1);
            packageName = packageName.substring(0, intentActionIndex);

            intentActionIndex = intentAction.indexOf(":");
            if (intentActionIndex >= 0)
            {
                intentComponent = intentAction.substring(intentActionIndex + 1);
                intentAction = intentAction.substring(0, intentActionIndex);
            }
        }

        if (!isAppInstalled(packageName))
        {
            return false;
        }

        if (intentAction.equals(""))
        {
            Log.e("PixoUtils", "Does not have intent!");
        
            intent = mContext.getPackageManager().getLaunchIntentForPackage(packageName);
            if (intent == null)
            {
                return false;
            }
        }
        else
        {
            Log.e("PixoUtils", "Has intent!");
            intent = new Intent(intentAction);
            if (intent == null)
            {
                return false;
            }
            intent.setPackage(packageName);
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        }

        if (extraKey.length > 0)
        {                
            for(int extraIndex = 0; extraKey.length > extraIndex; extraIndex++)
            {
                Log.e("PixoUtils", extraKey[extraIndex] + " - " + extraValue[extraIndex]);
                intent.putExtra(extraKey[extraIndex], extraValue[extraIndex]);
            }
        }

        if (!intentComponent.equals(""))
        {
            intent.setComponent(new android.content.ComponentName(packageName, intentComponent));
        }

        if (intent.resolveActivity(mContext.getPackageManager()) != null)
        {
            mContext.startActivity(intent);
            forceQuit();
            return true;
        }

        return false;
    }

    public void openURL(String URL)
    {
        Log.e("PixoUtils", "openURL: URL = " + URL);
        if (!URL.contains("://"))
        {
            // add http:// if there isn't a scheme before a colon
            if (!(URL.indexOf(":") >= 1))
            {
                URL = "http://" + URL;
                Log.e("PixoUtils", "openURL: corrected URL = " + URL);
            }
        }
        try
        {
            Intent BrowserIntent = new Intent(Intent.ACTION_VIEW, android.net.Uri.parse(URL));
            BrowserIntent.addCategory(Intent.CATEGORY_BROWSABLE);

            // open browser on its own task
            //BrowserIntent.addFlags(Intent.FLAG_ACTIVITY_NO_HISTORY | Intent.FLAG_ACTIVITY_CLEAR_WHEN_TASK_RESET);
            BrowserIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        
            // make sure there is a web browser to handle the URL before trying to start activity (or may crash!)
            if (BrowserIntent.resolveActivity(mContext.getPackageManager()) != null)
            {
                Log.e("PixoUtils", "openURL: Starting activity");
                mContext.startActivity(BrowserIntent);
                forceQuit();
            }
            else
            {
                Log.e("PixoUtils", "openURL: Could not find an application to receive the URL intent");
            }
        }
        catch (Exception e)
        {
            Log.e("PixoUtils", "openURL: Failed with exception " + e.getMessage());
        }
    }

    public void forceQuit()
    {
        Log.e("PixoUtils", "Calling ForceQuit");
        System.exit(0);
        Activity activity = (Activity)mContext;
        activity.finishAndRemoveTask();
    }

    public boolean isAppInstalled(String packageName) {
        return getInstalledPackagedVersionCode(packageName) != -1;
    }

    public long getInstalledPackagedVersionCode(String packageName) {
        try {
            PackageInfo pInfo = mContext.getPackageManager().getPackageInfo(packageName, 0);
            // if (Build.VERSION.SDK_INT >= 28) {
            //     return pInfo.getLongVersionCode();
            // }
            return pInfo.versionCode;
        } catch (Exception e){
            Log.e("NativeUtils", e.toString());
            return -1;
        }
    }

    public String getInstalledPackagedVersionName(String packageName) {
        try {
            PackageInfo pInfo = mContext.getPackageManager().getPackageInfo(packageName, 0);
            return pInfo.versionName;
        } catch (Exception e){
            Log.e("NativeUtils", e.toString());
            return null;
        }
    }
}
