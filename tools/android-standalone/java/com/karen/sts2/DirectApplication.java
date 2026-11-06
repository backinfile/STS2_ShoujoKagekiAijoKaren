package com.karen.sts2;

import android.app.Application;
import android.system.Os;
import android.util.Log;
import java.io.*;

public final class DirectApplication extends Application {
    @Override public void onCreate(){
        super.onCreate();
        File root=getFilesDir();new File(root,".config").mkdirs();new File(root,"tmp").mkdirs();new File(root,"logs").mkdirs();
        try{
            Os.setenv("HOME",root.getAbsolutePath(),true);
            for(String key:new String[]{"TMPDIR","TMP","TEMP"})Os.setenv(key,new File(root,"tmp").getAbsolutePath(),true);
            System.setProperty("java.io.tmpdir",new File(root,"tmp").getAbsolutePath());
        }catch(Exception error){throw new IllegalStateException("Cannot prepare private Mono runtime directories",error);}
        // Keep this process's Java/native diagnostics available without a launcher app.
        new Thread(()->{
            try{
                Process process=new ProcessBuilder("logcat","--pid="+android.os.Process.myPid(),"-v","threadtime").redirectErrorStream(true).start();
                try(InputStream input=process.getInputStream();OutputStream output=new BufferedOutputStream(new FileOutputStream(new File(root,"logs/android.log")))){
                    byte[] buffer=new byte[8192];int length;long total=0;
                    while(total<16L*1024*1024 && (length=input.read(buffer))!=-1){output.write(buffer,0,length);output.flush();total+=length;}
                }finally{process.destroy();}
            }catch(Exception error){Log.w("KarenDirect","Cannot collect application logcat",error);}
        },"Karen diagnostics").start();
    }
}
