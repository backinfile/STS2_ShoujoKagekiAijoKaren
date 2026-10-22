package com.karen.sts2;

import android.app.*;
import android.content.Intent;
import android.os.Bundle;
import android.graphics.Color;
import android.view.Gravity;
import android.widget.*;
import java.io.*;

/** One automatic preparation screen, followed directly by the game. */
public final class PrepareGameActivity extends Activity {
    private TextView status;
    @Override public void onCreate(Bundle state){
        super.onCreate(state);
        LinearLayout layout=new LinearLayout(this);layout.setOrientation(LinearLayout.VERTICAL);layout.setGravity(Gravity.CENTER);
        layout.setPadding(48,32,48,32);layout.setBackgroundColor(Color.rgb(21,19,25));
        status=new TextView(this);status.setText("正在准备《塔2 · 华恋》…");status.setTextColor(Color.WHITE);status.setTextSize(22);status.setGravity(Gravity.CENTER);
        layout.addView(status);ProgressBar bar=new ProgressBar(this);layout.addView(bar);setContentView(layout);
        new Thread(()->{
            try{
                BundleInstaller.install(getFilesDir(),name->getAssets().open(name),(done,total)->runOnUiThread(()->{
                    if(!isFinishing() && !isDestroyed())status.setText("正在准备《塔2 · 华恋》… "+(total==0?0:done*100/total)+"%\n首次启动需要解包，之后将直接进入游戏");
                }));
                initializeSettings();
                runOnUiThread(()->{if(!isFinishing() && !isDestroyed()){startActivity(new Intent(this,GameHost.class));finish();}});
            }catch(Exception error){
                android.util.Log.e("KarenDirect","Game preparation failed",error);
                runOnUiThread(()->{if(!isFinishing() && !isDestroyed())new AlertDialog.Builder(this).setTitle("游戏准备失败").setMessage(error.getMessage()).setPositiveButton("重试",(d,w)->{recreate();}).setNegativeButton("退出",(d,w)->finish()).setCancelable(false).show();});
            }
        },"Karen program installation").start();
    }
    private void initializeSettings()throws Exception{
        File account=new File(getFilesDir(),"default/1");account.mkdirs();File settings=new File(account,"settings.save");
        if(settings.exists())return;
        File temporary=new File(account,"settings.save.initializing");
        try(InputStream input=getAssets().open("direct/default-settings.json");OutputStream output=new FileOutputStream(temporary)){
            byte[] buffer=new byte[4096];int count;while((count=input.read(buffer))!=-1)output.write(buffer,0,count);
        }
        if(!temporary.renameTo(settings))throw new IOException("Cannot initialize game settings");
    }
}
