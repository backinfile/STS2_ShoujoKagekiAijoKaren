package com.godot.game;
import android.content.Context;
import java.io.*;
import java.util.*;
import java.util.zip.*;

/** Only the four public upstream calls used by the installer, with isolated test IO. */
public class ExtraSettingsRepository {
    private final Context context;
    public ExtraSettingsRepository(Context value) { context=value; }
    public void ensureAppDirectories() { context.modsRoot.mkdirs(); }
    public File getModsRootDir() { return context.modsRoot; }
    public List<ModEntry> listInstalledModManifests() {
        List<ModEntry> entries=new ArrayList<>();
        for (String id : new String[]{"BaseLib", "ShoujoKagekiAijoKaren"})
            if (new File(context.modsRoot, id + "/" + id + ".json").isFile()) entries.add(new ModEntry(id));
        return entries;
    }
    public String importDownloadedModFile(File zip, String name) throws Exception {
        context.imports++;
        if (context.failNextImport) { context.failNextImport=false; throw new IOException("Simulated import failure"); }
        try (ZipInputStream stream=new ZipInputStream(new FileInputStream(zip))) {
            ZipEntry entry;
            byte[] buffer=new byte[8192];
            while ((entry=stream.getNextEntry()) != null) {
                File output=new File(context.modsRoot,entry.getName());
                if (entry.isDirectory()) { output.mkdirs(); continue; }
                output.getParentFile().mkdirs();
                try (OutputStream target=new FileOutputStream(output)) {
                    int count; while ((count=stream.read(buffer)) != -1) target.write(buffer,0,count);
                }
            }
        }
        context.modsEnabled=true;
        return name;
    }
    public static final class ModEntry {
        public final String modId;
        ModEntry(String id) { modId=id; }
    }
}
