package com.karen.sts2;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;

/** Installs only immutable program assets. Saves/settings are never package destinations. */
public final class BundleInstaller {
    public interface Assets { InputStream open(String name) throws IOException; }
    public interface Progress { void update(long complete,long total); }
    private static final String MARKER=".karen-direct-bundle";
    private BundleInstaller() {}

    public static synchronized void install(File root,Assets assets,Progress progress) throws Exception {
        byte[] manifest;
        try(InputStream input=assets.open("direct/manifest.tsv")){
            ByteArrayOutputStream output=new ByteArrayOutputStream();copy(input,output,null);manifest=output.toByteArray();
        }
        String id=hash(manifest);
        File marker=new File(root,MARKER);
        String previous=marker.isFile()?read(marker):"";
        List<String[]> entries=new ArrayList<>();long total=0;
        try(BufferedReader reader=new BufferedReader(new InputStreamReader(new ByteArrayInputStream(manifest),StandardCharsets.UTF_8))){
            String line;
            while((line=reader.readLine())!=null){
                if(line.isEmpty())continue;
                String[] parts=line.split("\t",-1);
                if(parts.length!=3 || !parts[0].matches("[0-9a-f]{64}"))throw new IOException("Invalid payload manifest");
                resolve(root,parts[2]);long size=Long.parseLong(parts[1]);
                if(size<0 || size>3L*1024*1024*1024)throw new IOException("Invalid payload length");
                total+=size;entries.add(parts);
            }
        }
        if(entries.isEmpty())throw new IOException("Empty payload manifest");
        boolean ready=previous.equals(id);
        for(String[] entry:entries){File file=resolve(root,entry[2]);ready &= file.isFile() && file.length()==Long.parseLong(entry[1]);}
        if(ready){progress.update(total,total);return;}
        long complete=0;
        for(String[] entry:entries){
            File target=resolve(root,entry[2]);long length=Long.parseLong(entry[1]);
            if(target.isFile() && target.length()==length && hash(target).equals(entry[0])){
                complete+=length;progress.update(complete,total);continue;
            }
            File parent=target.getParentFile();
            if(!parent.isDirectory() && !parent.mkdirs() && !parent.isDirectory())throw new IOException("Cannot create program directory");
            File temporary=new File(parent,target.getName()+".installing");
            MessageDigest digest=MessageDigest.getInstance("SHA-256");
            final long base=complete,all=total;final long[] copied={0};long last=System.nanoTime();
            try{
                try(InputStream input=assets.open("direct/files/"+entry[2]);OutputStream output=new BufferedOutputStream(new FileOutputStream(temporary))){
                    byte[] buffer=new byte[1024*1024];int count;
                    while((count=input.read(buffer))!=-1){
                        output.write(buffer,0,count);digest.update(buffer,0,count);copied[0]+=count;
                        if(copied[0]>length)throw new IOException("Payload larger than manifest: "+entry[2]);
                        if(System.nanoTime()-last>100000000L){progress.update(base+copied[0],all);last=System.nanoTime();}
                    }
                }
                if(copied[0]!=length || !hex(digest.digest()).equals(entry[0]))throw new IOException("Payload checksum mismatch: "+entry[2]);
                replace(temporary,target);
            }finally{temporary.delete();}
            complete+=length;progress.update(complete,total);
        }
        root.mkdirs();File temporary=new File(root,MARKER+".installing");
        try(OutputStream stream=new FileOutputStream(temporary)){stream.write(id.getBytes(StandardCharsets.UTF_8));}
        replace(temporary,marker);
    }

    private static File resolve(File root,String path) throws IOException {
        if(path.startsWith("/") || path.contains("\\") || path.contains(":"))throw new IOException("Invalid program path");
        for(String part:path.split("/",-1))if(part.isEmpty() || part.equals("..") || part.equals("."))throw new IOException("Invalid program path");
        boolean allowed=path.startsWith("game/") || path.startsWith(".godot/mono/publish/arm64/") ||
            path.startsWith("mods/BaseLib/") || path.startsWith("mods/ShoujoKagekiAijoKaren/") || path.equals("port_compat.pck");
        if(!allowed)throw new IOException("Manifest path outside program directories");
        File result=new File(root,path).getCanonicalFile();
        if(!result.getPath().startsWith(root.getCanonicalPath()+File.separator))throw new IOException("Program path escaped root");
        return result;
    }

    private static void replace(File temporary,File target) throws IOException {
        if(temporary.renameTo(target)){new File(target.getParentFile(),target.getName()+".previous").delete();return;} // Atomic replace on Android/Linux.
        File backup=new File(target.getParentFile(),target.getName()+".previous");
        if(backup.exists() && !backup.delete())throw new IOException("Cannot retire previous program backup");
        boolean existed=target.exists();
        if(existed && !target.renameTo(backup))throw new IOException("Cannot preserve previous program file");
        if(!temporary.renameTo(target)){
            if(existed && !backup.renameTo(target))throw new IOException("Program replacement/rollback failed; previous file retained");
            throw new IOException("Program replacement failed");
        }
        if(existed)backup.delete();
    }
    private static void copy(InputStream input,OutputStream output,MessageDigest digest)throws IOException{
        byte[] buffer=new byte[65536];int count;while((count=input.read(buffer))!=-1){output.write(buffer,0,count);if(digest!=null)digest.update(buffer,0,count);}
    }
    private static String read(File file)throws IOException{
        try(InputStream input=new FileInputStream(file);ByteArrayOutputStream output=new ByteArrayOutputStream()){
            copy(input,output,null);return new String(output.toByteArray(),StandardCharsets.UTF_8);
        }
    }
    private static String hash(File file)throws Exception{
        MessageDigest digest=MessageDigest.getInstance("SHA-256");
        try(InputStream input=new BufferedInputStream(new FileInputStream(file))){byte[] buffer=new byte[1024*1024];int n;while((n=input.read(buffer))!=-1)digest.update(buffer,0,n);}
        return hex(digest.digest());
    }
    private static String hash(byte[] data)throws Exception{return hex(MessageDigest.getInstance("SHA-256").digest(data));}
    private static String hex(byte[] data){StringBuilder value=new StringBuilder();for(byte b:data)value.append(String.format(Locale.ROOT,"%02x",b&255));return value.toString();}
}
