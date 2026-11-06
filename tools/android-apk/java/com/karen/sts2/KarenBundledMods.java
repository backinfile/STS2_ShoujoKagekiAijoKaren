package com.karen.sts2;

import java.io.*;
import java.lang.reflect.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;

/** Public Android/upstream APIs only. Reflection keeps the build independent of an Android SDK. */
public final class KarenBundledMods {
    static final String ASSET = "karen/bundled-mods.zip";
    static final String MARKER = ".karen-bundled-v0.1.3-installed";
    private static final Set<String> IDS = new HashSet<>(Arrays.asList("BaseLib", "ShoujoKagekiAijoKaren"));
    private KarenBundledMods() {}

    public static void initialize(final Object application) {
        // The upstream crash screen has its own process; never install recursively there.
        try (InputStream stream = new FileInputStream("/proc/self/cmdline")) {
            byte[] bytes = new byte[256];
            int length = stream.read(bytes);
            if (length > 0 && new String(bytes, 0, length, StandardCharsets.UTF_8).contains(":")) return;
        } catch (IOException ignored) { }
        installSafely(application);
        try {
            Class<?> callbacks = Class.forName("android.app.Application$ActivityLifecycleCallbacks");
            Object listener = Proxy.newProxyInstance(callbacks.getClassLoader(), new Class<?>[]{callbacks},
                (proxy, method, args) -> {
                    String name = method.getName();
                    // Pausing settings precedes game creation, including a newly selected isolated profile.
                    if (name.equals("onActivityPaused") || name.equals("onActivityResumed")) installSafely(application);
                    if (name.equals("hashCode")) return System.identityHashCode(proxy);
                    if (name.equals("equals")) return proxy == args[0];
                    if (name.equals("toString")) return "Karen bundled Mod installer";
                    return null;
                });
            application.getClass().getMethod("registerActivityLifecycleCallbacks", callbacks).invoke(application, listener);
        } catch (Exception exception) { report(application, exception); }
    }

    private static void installSafely(Object context) {
        try { install(context); }
        catch (Exception exception) { report(context, exception); }
    }

    private static void report(Object context, Exception exception) {
        System.err.println("[Karen APK] Bundled Mod installation failed; import the Mod ZIP manually: " + exception);
        exception.printStackTrace();
        try {
            Class<?> toast = Class.forName("android.widget.Toast");
            Object message = toast.getMethod("makeText", Class.forName("android.content.Context"), CharSequence.class, int.class)
                .invoke(null, context, "Karen Mod installation failed. Please import the Mod ZIP manually.", 1);
            toast.getMethod("show").invoke(message);
        } catch (Exception ignored) { }
    }

    static synchronized void install(Object context) throws Exception {
        Class<?> repositoryType = Class.forName("com.godot.game.ExtraSettingsRepository");
        Object repository = repositoryType.getConstructor(Class.forName("android.content.Context")).newInstance(context);
        repositoryType.getMethod("ensureAppDirectories").invoke(repository);
        File root = (File) repositoryType.getMethod("getModsRootDir").invoke(repository);
        File marker = new File(root, MARKER);
        // A marker also remembers an intentional uninstall or disabled Mod setting.
        if (marker.isFile()) return;
        Set<String> missing = new HashSet<>(IDS);
        missing.removeAll(installedIds(repositoryType, repository));
        if (!missing.isEmpty()) {
            File cache = (File) context.getClass().getMethod("getCacheDir").invoke(context);
            File archive = File.createTempFile("karen-bundled-", ".zip", cache);
            File filtered = null;
            try {
                Object assets = context.getClass().getMethod("getAssets").invoke(context);
                try (InputStream input = (InputStream) assets.getClass().getMethod("open", String.class).invoke(assets, ASSET);
                     OutputStream output = new BufferedOutputStream(new FileOutputStream(archive))) {
                    copy(input, output);
                }
                if (!sha256(archive).equals(BuildInfo.MOD_SHA256)) throw new IOException("Bundled ZIP checksum mismatch");
                // Import only missing IDs, so an installed/upgraded BaseLib or Karen is never replaced.
                filtered = File.createTempFile("karen-missing-", ".zip", cache);
                Set<String> found = new HashSet<>();
                try (ZipInputStream input = new ZipInputStream(new BufferedInputStream(new FileInputStream(archive)));
                     ZipOutputStream output = new ZipOutputStream(new BufferedOutputStream(new FileOutputStream(filtered)))) {
                    ZipEntry entry;
                    while ((entry = input.getNextEntry()) != null) {
                        String name = entry.getName();
                        String[] parts = name.split("/", -1);
                        if (name.contains("\\") || name.startsWith("/") || Arrays.asList(parts).contains("..") ||
                            parts.length < 2 || !IDS.contains(parts[0])) throw new IOException("Unexpected bundled ZIP path: " + name);
                        if (!missing.contains(parts[0])) continue;
                        found.add(parts[0]);
                        output.putNextEntry(new ZipEntry(name));
                        if (!entry.isDirectory()) copy(input, output);
                        output.closeEntry();
                    }
                }
                if (!found.containsAll(missing)) throw new IOException("Bundled ZIP lacks required Mods");
                repositoryType.getMethod("importDownloadedModFile", File.class, String.class)
                    .invoke(repository, filtered, "Karen bundled Mods.zip");
                if (!installedIds(repositoryType, repository).containsAll(IDS)) throw new IOException("Imported Mods were not found");
            } finally {
                archive.delete();
                if (filtered != null) filtered.delete();
            }
        }
        try (OutputStream output = new FileOutputStream(marker)) {
            output.write((BuildInfo.MOD_SHA256 + "\n").getBytes(StandardCharsets.UTF_8));
        }
        System.out.println("[Karen APK] Bundled Mods ready for selected profile");
    }

    private static Set<String> installedIds(Class<?> type, Object repository) throws Exception {
        Set<String> ids = new HashSet<>();
        for (Object entry : (Iterable<?>) type.getMethod("listInstalledModManifests").invoke(repository))
            ids.add((String) entry.getClass().getField("modId").get(entry));
        return ids;
    }

    private static void copy(InputStream input, OutputStream output) throws IOException {
        byte[] buffer = new byte[65536];
        int length;
        while ((length = input.read(buffer)) != -1) output.write(buffer, 0, length);
    }

    private static String sha256(File file) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        try (InputStream input = new BufferedInputStream(new FileInputStream(file))) {
            byte[] buffer = new byte[65536];
            int length;
            while ((length = input.read(buffer)) != -1) digest.update(buffer, 0, length);
        }
        StringBuilder result = new StringBuilder();
        for (byte value : digest.digest()) result.append(String.format(Locale.ROOT, "%02x", value & 255));
        return result.toString();
    }
}
