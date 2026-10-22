package android.content;
import java.io.*;
public class Context {
    public File modsRoot;
    public final File cache;
    public final Assets assets;
    public boolean modsEnabled, failNextImport;
    public int imports;
    public Context(File directory, File zip) {
        modsRoot = new File(directory, "mods");
        cache = new File(directory, "cache"); cache.mkdirs();
        assets = new Assets(zip);
    }
    public File getCacheDir() { return cache; }
    public Assets getAssets() { return assets; }
    public static final class Assets {
        public File zip;
        public Assets(File source) { zip=source; }
        public InputStream open(String name) throws IOException {
            if (!name.equals("karen/bundled-mods.zip")) throw new IOException(name);
            return new FileInputStream(zip);
        }
    }
}
