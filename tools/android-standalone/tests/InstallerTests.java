package com.karen.sts2;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;

public final class InstallerTests {
    static File base;static int passed;
    static final class Fixture implements BundleInstaller.Assets {
        final SortedMap<String,byte[]> files=new TreeMap<>();
        int programOpens;boolean corrupt,fail;
        String override;
        Fixture(){files.put("game/example.pck",bytes("game-data"));files.put("mods/BaseLib/BaseLib.dll",bytes("base"));files.put("mods/ShoujoKagekiAijoKaren/Karen.dll",bytes("karen"));}
        @Override public InputStream open(String name)throws IOException{
            if(name.equals("direct/manifest.tsv")){
                if(override!=null)return new ByteArrayInputStream(bytes(override));
                StringBuilder manifest=new StringBuilder();
                for(Map.Entry<String,byte[]> entry:files.entrySet())manifest.append(hash(entry.getValue())).append('\t').append(entry.getValue().length).append('\t').append(entry.getKey()).append('\n');
                return new ByteArrayInputStream(bytes(manifest.toString()));
            }
            programOpens++;
            if(fail)throw new IOException("Simulated asset IO failure");
            byte[] data=files.get(name.substring("direct/files/".length()));if(data==null)throw new IOException(name);
            return new ByteArrayInputStream(corrupt?bytes("corrupt"):data);
        }
    }
    static byte[] bytes(String value){return value.getBytes(StandardCharsets.UTF_8);}
    static String hash(byte[] value){try{StringBuilder result=new StringBuilder();for(byte b:MessageDigest.getInstance("SHA-256").digest(value))result.append(String.format(Locale.ROOT,"%02x",b&255));return result.toString();}catch(Exception e){throw new RuntimeException(e);}}
    static void check(boolean value,String message){if(!value)throw new AssertionError(message);}
    static void pass(String name){passed++;System.out.println("PASS: "+name);}
    static File root(String name){File file=new File(base,name);file.mkdirs();return file;}
    static String read(File root,String name)throws Exception{return new String(Files.readAllBytes(new File(root,name).toPath()),StandardCharsets.UTF_8);}
    static void install(File root,Fixture source)throws Exception{
        final long[] last={-1};BundleInstaller.install(root,source,(done,total)->{check(done>=0 && done<=total,"bounded progress");last[0]=done;});check(last[0]>=0,"progress delivered");
    }
    static void rejected(File root,Fixture source)throws Exception{try{install(root,source);throw new AssertionError("Expected rejection");}catch(IOException expected){}}
    public static void main(String[] args)throws Exception{
        base=new File(args[0]);
        Fixture fresh=new Fixture();File one=root("fresh");install(one,fresh);check(read(one,"game/example.pck").equals("game-data"),"game installed");check(fresh.programOpens==3,"all assets installed");pass("fresh game and Mods installed with checksums");
        fresh.programOpens=0;install(one,fresh);check(fresh.programOpens==0,"warm launch does not copy payload");pass("repeat launch skips program copies");
        new File(one,"mods/BaseLib/BaseLib.dll").delete();install(one,fresh);check(read(one,"mods/BaseLib/BaseLib.dll").equals("base"),"missing file restored");pass("missing program file repaired");
        Files.write(new File(one,"game/example.pck").toPath(),bytes("short"));install(one,fresh);check(read(one,"game/example.pck").equals("game-data"),"truncation repaired");pass("truncated program file repaired");
        File saves=new File(one,"default/1/current_run.save");saves.getParentFile().mkdirs();Files.write(saves.toPath(),bytes("player-save"));
        fresh.files.put("game/example.pck",bytes("updated-game-data"));install(one,fresh);check(read(one,"game/example.pck").equals("updated-game-data") && new String(Files.readAllBytes(saves.toPath()),StandardCharsets.UTF_8).equals("player-save"),"program updated and save retained");pass("bundle update preserves player saves");
        Fixture bad=new Fixture();bad.corrupt=true;File corrupt=root("corrupt");rejected(corrupt,bad);check(!new File(corrupt,".karen-direct-bundle").exists(),"no success marker on corrupt data");bad.corrupt=false;install(corrupt,bad);pass("checksum failure rejects and retries");
        Fixture io=new Fixture();io.fail=true;File retry=root("retry");rejected(retry,io);check(!new File(retry,"game/example.pck.installing").exists(),"failed staging removed");io.fail=false;install(retry,io);pass("asset IO failure cleans temporary file and retries");
        Fixture traversal=new Fixture();traversal.override=hash(bytes("x"))+"\t1\tgame/../../escape\n";rejected(root("traversal"),traversal);pass("path traversal rejected");
        Fixture settings=new Fixture();settings.override=hash(bytes("x"))+"\t1\tdefault/1/settings.save\n";rejected(root("settings"),settings);pass("save/settings paths forbidden in payload manifest");
        Fixture empty=new Fixture();empty.files.put("game/empty",new byte[0]);File zero=root("empty");install(zero,empty);check(new File(zero,"game/empty").isFile() && new File(zero,"game/empty").length()==0,"empty file installed");pass("zero-byte assets supported");
        Fixture partial=new Fixture();File interrupted=root("interrupted");new File(interrupted,"game").mkdirs();Files.write(new File(interrupted,"game/example.pck.installing").toPath(),bytes("interrupted-partial"));install(interrupted,partial);check(read(interrupted,"game/example.pck").equals("game-data"),"interrupted copy replaced");pass("interrupted first extraction can resume safely");
        Fixture failedUpdate=new Fixture();File rollback=root("failed-update");install(rollback,failedUpdate);failedUpdate.files.put("game/example.pck",bytes("new-revision"));failedUpdate.corrupt=true;rejected(rollback,failedUpdate);check(read(rollback,"game/example.pck").equals("game-data"),"previous installed data retained");pass("failed update preserves previous verified program file");
        System.out.println("RESULT: "+passed+" installer checks passed (JVM; not Android native runtime)");
        if(args.length>1){
            File actual=root("actual-apk");
            try(ZipFile apk=new ZipFile(args[1])){
                BundleInstaller.Assets source=name->{ZipEntry entry=apk.getEntry("assets/"+name);if(entry==null)throw new IOException(name);return apk.getInputStream(entry);};
                final int[] percent={-1};BundleInstaller.install(actual,source,(done,total)->{int value=(int)(done*100/total);if(value/25!=percent[0]/25||value==100){percent[0]=value;System.out.println("Actual APK extraction "+value+"%");}});
                check(new File(actual,"game/SlayTheSpire2.pck").length()>1800000000L,"full base game installed");
                check(new File(actual,".godot/mono/publish/arm64/sts2.dll").isFile(),"game assembly");
                check(new File(actual,"mods/ShoujoKagekiAijoKaren/lib/ShoujoKagekiAijoKaren.Stable.dll").isFile(),"Karen stable implementation");
                check(new File(actual,"mods/BaseLib/BaseLib.dll").isFile(),"BaseLib installed");
            }
            System.out.println("PASS: complete signed APK payload extracted and SHA-256 verified on JVM");
        }
    }
}
