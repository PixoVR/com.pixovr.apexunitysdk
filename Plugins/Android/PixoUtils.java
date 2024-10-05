package com.pixovr.pixosdk;

import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.util.Log;
import android.os.Bundle;
import android.os.Build;
import android.app.ActivityManager;
import android.graphics.Bitmap;
import android.graphics.drawable.Drawable;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.Canvas;

import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;
import java.util.*;
import java.io.ByteArrayOutputStream;
import android.util.TimingLogger;
import android.util.Log;
import android.net.Uri;

public class PixoUtils {

    Context mContext;
    ActivityManager mActivityManager;

    public PixoUtils(Context context) {
        mContext = context;
        mActivityManager = (ActivityManager) context.getSystemService(Context.ACTIVITY_SERVICE);
    }

    public boolean launchApp(String packageName, String[] extraKey, String[] extraValue)
    {
        Log.d("PixoUtils", " AndroidThunkJava_Launch");
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
            Log.d("PixoUtils", "Does not have intent!");
        
            intent = mContext.getPackageManager().getLaunchIntentForPackage(packageName);
            if (intent == null)
            {
                return false;
            }
        }
        else
        {
            Log.d("PixoUtils", "Has intent!");
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
                Log.d("PixoUtils", extraKey[extraIndex] + " - " + extraValue[extraIndex]);
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

        System.exit(0);
        // finish();
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
