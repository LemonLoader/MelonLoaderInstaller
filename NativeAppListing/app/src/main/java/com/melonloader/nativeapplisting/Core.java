package com.melonloader.nativeapplisting;

import android.annotation.SuppressLint;
import android.app.Application;
import android.content.Context;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Matrix;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.Drawable;
import android.os.Build;
import android.os.Looper;
import android.util.Base64;
import org.lsposed.hiddenapibypass.HiddenApiBypass;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.Objects;

import com.melonloader.nativeapplisting.UnityApplicationData.Status;

public class Core {
    @SuppressLint("NewApi")
    public static void main(String[] args) {
        if (Looper.getMainLooper() == null) {
            Looper.prepareMainLooper();
        }

        String[] abis = Build.SUPPORTED_64_BIT_ABIS;
        if (Arrays.stream(abis).noneMatch(a -> Objects.equals(a, "arm64-v8a")))
        {
            System.out.println("DEVICE_NOT_SUPPORTED");
            return;
        }

        try {
            if (Build.VERSION.SDK_INT >= 28) {
                HiddenApiBypass.addHiddenApiExemptions("");
            }

            @SuppressLint("PrivateApi") Class<?> cls = Class.forName("android.app.ActivityThread");
            Object thread = HiddenApiBypass.invoke(cls, null, "systemMain");
            Application app = (Application) HiddenApiBypass.invoke(cls, thread, "getApplication");

            Context ctx = app.getApplicationContext();
            PackageManager pkg = ctx.getPackageManager();

            List<UnityApplicationData> applicationDatas = new ArrayList<>();

            for (ApplicationInfo info : pkg.getInstalledApplications(PackageManager.GET_META_DATA)) {
                File nativeLibDir = Paths.get(info.nativeLibraryDir).toFile();

                if (!nativeLibDir.exists())
                    continue;

                File[] files = nativeLibDir.listFiles();

                boolean isUnity = Arrays.stream(files).anyMatch(f -> f.getName().contains("libunity.so"));
                boolean isIl2Cpp = Arrays.stream(files).anyMatch(f -> f.getName().contains("libil2cpp.so"));
                if (!isUnity || !isIl2Cpp)
                    continue;

                Status status = Status.Unpatched;

                if (!info.nativeLibraryDir.contains("arm64"))
                    status = Status.Unsupported;

                boolean hasBootstrap = Arrays.stream(files).anyMatch(f -> f.getName().contains("libBootstrap.so"));
                boolean hasDobby = Arrays.stream(files).anyMatch(f -> f.getName().contains("libdobby.so"));
                if (hasBootstrap && hasDobby)
                    status = Status.Patched;

                String label = (String) pkg.getApplicationLabel(info);

                List<String> apkPaths = new ArrayList<>();
                if (info.splitPublicSourceDirs != null)
                    apkPaths = new ArrayList<>(Arrays.asList(info.splitPublicSourceDirs));

                apkPaths.add(info.publicSourceDir);

                Drawable icon = info.loadIcon(pkg);

                applicationDatas.add(new UnityApplicationData(label, info.packageName, apkPaths.toArray(new String[0]), status, drawableToBase64(icon)));
            }

            for (UnityApplicationData unity : applicationDatas) {
                System.out.println("-----------------------------------");
                System.out.println(unity.AppName);
                System.out.println(unity.PackageName);
                System.out.println(unity.Status.toString());
                System.out.println(unity.EncodedIcon);
                for (String path : unity.ApkPaths)
                {
                    System.out.println(path);
                }
            }
        }
        catch (Exception ex) {
            ex.printStackTrace();
        }

        System.exit(0);
    }

    public static String drawableToBase64(Drawable drawable) {
        if (drawable instanceof BitmapDrawable) {
            Bitmap bitmap = ((BitmapDrawable) drawable).getBitmap();
            return bitmapToBase64(scaleBitmap(bitmap, 64, 64));
        }

        Bitmap bitmap = convertToBitmap(drawable);
        return bitmapToBase64(scaleBitmap(bitmap, 64, 64));
    }

    private static Bitmap convertToBitmap(Drawable drawable) {
        Bitmap bitmap = Bitmap.createBitmap(drawable.getIntrinsicWidth(), drawable.getIntrinsicHeight(), Bitmap.Config.ARGB_8888);
        Canvas canvas = new Canvas(bitmap);
        drawable.setBounds(0, 0, canvas.getWidth(), canvas.getHeight());
        drawable.draw(canvas);
        return bitmap;
    }

    private static Bitmap scaleBitmap(Bitmap bitmap, int width, int height) {
        int originalWidth = bitmap.getWidth();
        int originalHeight = bitmap.getHeight();

        if (originalWidth <= width && originalHeight <= height) {
            return bitmap;
        }

        float scaleWidth = ((float) width) / originalWidth;
        float scaleHeight = ((float) height) / originalHeight;
        float scaleFactor = Math.min(scaleWidth, scaleHeight);

        Matrix matrix = new Matrix();
        matrix.postScale(scaleFactor, scaleFactor);

        return Bitmap.createBitmap(bitmap, 0, 0, originalWidth, originalHeight, matrix, true);
    }

    @SuppressLint("NewApi")
    private static String bitmapToBase64(Bitmap bitmap) {
        ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();
        bitmap.compress(Bitmap.CompressFormat.JPEG, 75, byteArrayOutputStream);
        byte[] byteArray = byteArrayOutputStream.toByteArray();
        return Base64.encodeToString(byteArray, Base64.DEFAULT).trim().replace("\n", "").replace("\r", "");
    }
}
