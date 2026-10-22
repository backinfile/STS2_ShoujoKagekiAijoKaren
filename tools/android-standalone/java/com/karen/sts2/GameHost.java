package com.karen.sts2;

import android.app.AlertDialog;
import android.content.pm.ActivityInfo;
import android.os.Bundle;
import org.godotengine.godot.Godot;
import org.godotengine.godot.GodotActivity;
import org.fmod.FMOD;
import org.json.JSONObject;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/** Minimal Godot host; no launcher activity, account, import, or profile manager. */
public final class GameHost extends GodotActivity {
    private static volatile GameHost current;
    private static volatile boolean resumed,focused;
    static {
        System.loadLibrary("fmod");System.loadLibrary("fmodstudio");
        System.loadLibrary("monomod_android_libc_shim");
        System.loadLibrary("System.Security.Cryptography.Native.Android");
    }
    @Override public void onCreate(Bundle state){
        current=this;
        try{FMOD.setAudioCompatibilityMode(settings().optBoolean("audio_compatibility_mode",false));FMOD.init(this);}
        catch(Exception error){throw new IllegalStateException("FMOD initialization failed",error);}
        super.onCreate(state);applySelectedScreenOrientationFromGame();
    }
    @Override public List<String> getCommandLine(){
        File root=getFilesDir();
        return new ArrayList<>(Arrays.asList("--rendering-method","gl_compatibility","--rendering-driver","opengl3",
            "--main-pack",new File(root,"game/SlayTheSpire2.pck").getAbsolutePath(),
            "--log-file",new File(root,"logs/godot.log").getAbsolutePath(),"--force-steam","off"));
    }
    @Override public void onResume(){super.onResume();resumed=true;}
    @Override public void onPause(){resumed=false;super.onPause();}
    @Override public void onWindowFocusChanged(boolean value){focused=value;super.onWindowFocusChanged(value);}
    @Override public void onDestroy(){if(current==this)current=null;super.onDestroy();FMOD.close();}
    public static boolean isGameWindowInteractive(){return resumed&&focused;}
    public static String getGodotDataDir(){return current==null?"":current.getFilesDir().getAbsolutePath();}
    public static String getSelectedGameDir(){return current==null?"":new File(current.getFilesDir(),"game").getAbsolutePath();}
    public static String getSelectedAccountRootDir(){return current==null?"":new File(current.getFilesDir(),"default/1").getAbsolutePath();}
    public static String getSelectedModsDir(){return current==null?"":new File(current.getFilesDir(),"mods").getAbsolutePath();}
    public static String getSelectedLogsDir(){return current==null?"":new File(current.getFilesDir(),"logs").getAbsolutePath();}
    public static boolean isAndroidCompatPackEnabled(){return true;}
    public static String getSelectedCompatOverlayPck(){return current==null?"":new File(current.getFilesDir(),"port_compat.pck").getAbsolutePath();}
    // The original Quit path saves and closes the game instead of reopening a shell.
    public static boolean restartToSettingsFromGame(){return false;}
    public static boolean restartGameFromGame(){
        GameHost host=current;if(host==null)return false;
        host.runOnUiThread(()->host.onGodotRestartRequested(host.getGodot()));return true;
    }
    public static void applySelectedScreenOrientationFromGame(){
        GameHost host=current;if(host==null)return;
        host.runOnUiThread(()->{
            try{host.setRequestedOrientation(host.settings().optBoolean("android_flip_screen_180",false)?ActivityInfo.SCREEN_ORIENTATION_REVERSE_LANDSCAPE:ActivityInfo.SCREEN_ORIENTATION_SENSOR_LANDSCAPE);}
            catch(Exception error){android.util.Log.w("KarenDirect","Cannot apply screen orientation",error);}
        });
    }
    // This small in-game dialog owns its settings; it never launches another app/shell.
    public static boolean launchGameSettingsFromGame(){
        GameHost host=current;if(host==null)return false;
        host.runOnUiThread(()->{
            try{
                JSONObject settings=host.settings();String[] keys={"shader_compatibility_mode","audio_compatibility_mode","android_flip_screen_180"};
                boolean[] checked={settings.optBoolean(keys[0],true),settings.optBoolean(keys[1],false),settings.optBoolean(keys[2],false)};
                new AlertDialog.Builder(host).setTitle("Android 设置（重启游戏后生效）")
                    .setMultiChoiceItems(new String[]{"画面兼容","声音兼容","横屏翻转 180°"},checked,(dialog,which,value)->{
                        try{JSONObject latest=host.settings();latest.put(keys[which],value);host.saveSettings(latest);}catch(Exception error){android.util.Log.e("KarenDirect","Cannot save Android setting",error);}
                    }).setPositiveButton("关闭",null).show();
            }catch(Exception error){android.util.Log.e("KarenDirect","Cannot open Android settings",error);}
        });return true;
    }
    private File settingsFile(){return new File(getFilesDir(),"default/1/settings.save");}
    private JSONObject settings()throws Exception{
        try(InputStream input=new FileInputStream(settingsFile());ByteArrayOutputStream output=new ByteArrayOutputStream()){
            byte[] buffer=new byte[4096];int n;while((n=input.read(buffer))!=-1)output.write(buffer,0,n);
            return new JSONObject(new String(output.toByteArray(),StandardCharsets.UTF_8));
        }
    }
    private void saveSettings(JSONObject settings)throws Exception{
        File file=settingsFile(),temporary=new File(file.getParentFile(),"settings.save.direct-tmp");
        try(OutputStream output=new FileOutputStream(temporary)){output.write(settings.toString(2).getBytes(StandardCharsets.UTF_8));}
        if(!temporary.renameTo(file))throw new IOException("Cannot save Android settings");
    }
}
