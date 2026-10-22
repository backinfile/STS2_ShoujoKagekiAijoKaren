package com.karen.sts2;
import android.app.Application;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;

public final class BootstrapTests {
    static File base, zip;
    static int passed;
    static Application app(String name) { return new Application(new File(base,name),zip); }
    static File manifest(Application app,String id) { return new File(app.modsRoot,id+"/"+id+".json"); }
    static boolean marker(Application app) { return new File(app.modsRoot,KarenBundledMods.MARKER).isFile(); }
    static void installed(Application app) {
        check(manifest(app,"BaseLib").isFile() && manifest(app,"ShoujoKagekiAijoKaren").isFile(),"both Mods present");
        check(marker(app),"installation marker");
        check(app.cache.list().length==0,"temporary ZIPs removed");
    }
    static void check(boolean okay,String message) { if (!okay) throw new AssertionError(message); }
    static void pass(String message) { passed++; System.out.println("PASS: "+message); }
    static void existing(Application app,String id) throws Exception {
        File file=manifest(app,id);file.getParentFile().mkdirs();Files.write(file.toPath(),"user version".getBytes(StandardCharsets.UTF_8));
    }
    public static void main(String[] args) throws Exception {
        base=new File(args[0]);zip=new File(args[1]);
        Application fresh=app("fresh"); KarenBundledMods.install(fresh); installed(fresh);
        check(fresh.imports==1 && fresh.modsEnabled,"fresh import enables Mods");pass("fresh bundle installation");
        Files.write(manifest(fresh,"BaseLib").toPath(),"upgraded".getBytes(StandardCharsets.UTF_8));fresh.modsEnabled=false;
        KarenBundledMods.install(fresh);check(fresh.imports==1 && !fresh.modsEnabled,"repeat respects settings");
        check(new String(Files.readAllBytes(manifest(fresh,"BaseLib").toPath()),StandardCharsets.UTF_8).equals("upgraded"),"upgrade preserved");
        pass("repeat launch preserves files and disabled setting");
        check(manifest(fresh,"ShoujoKagekiAijoKaren").delete(),"remove own test manifest");KarenBundledMods.install(fresh);
        check(!manifest(fresh,"ShoujoKagekiAijoKaren").exists(),"intentional uninstall respected");pass("marker respects later uninstall");
        Application partial=app("partial");existing(partial,"BaseLib");KarenBundledMods.install(partial);installed(partial);
        check(new String(Files.readAllBytes(manifest(partial,"BaseLib").toPath()),StandardCharsets.UTF_8).equals("user version"),"existing dependency preserved");
        pass("subset import preserves existing BaseLib");
        Application all=app("existing");existing(all,"BaseLib");existing(all,"ShoujoKagekiAijoKaren");KarenBundledMods.install(all);installed(all);
        check(all.imports==0 && !all.modsEnabled,"existing Mods/settings untouched");pass("existing pair avoids import");
        Application retry=app("retry");retry.failNextImport=true;
        try { KarenBundledMods.install(retry);throw new AssertionError("expected failure"); } catch (java.lang.reflect.InvocationTargetException expected) { }
        check(!marker(retry) && retry.cache.list().length==0,"failure leaves no marker/cache");KarenBundledMods.install(retry);installed(retry);
        check(retry.imports==2,"retried import");pass("failed import can retry");
        Application corrupt=app("corrupt");corrupt.assets.zip=new File(base,"bad.zip");Files.write(corrupt.assets.zip.toPath(),new byte[]{1,2,3});
        try { KarenBundledMods.install(corrupt);throw new AssertionError("expected checksum failure"); } catch (IOException expected) { }
        check(!marker(corrupt) && corrupt.imports==0 && corrupt.cache.list().length==0,"corrupt bundle rejected before import");pass("bad checksum rejected");
        Application lifecycle=app("lifecycle");KarenBundledMods.initialize(lifecycle);installed(lifecycle);
        check(lifecycle.callback!=null,"public lifecycle hook registered");pass("application initialization registers profile hook");
        File first=lifecycle.modsRoot;lifecycle.modsRoot=new File(base,"isolated-profile/mods");lifecycle.callback.onActivityPaused(new Object());installed(lifecycle);
        check(new File(first,KarenBundledMods.MARKER).isFile() && lifecycle.imports==2,"new root seeded once");
        lifecycle.callback.onActivityResumed(new Object());check(lifecycle.imports==2,"resume does not repeat import");pass("selected profile receives bundle once before game launch");
        System.out.println("RESULT: "+passed+" bootstrap checks passed (JVM mocks, not Android device)");
    }
}
