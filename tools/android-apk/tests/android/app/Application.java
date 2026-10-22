package android.app;
import java.io.File;
public class Application extends android.content.Context {
    public ActivityLifecycleCallbacks callback;
    public Application(File directory, File zip) { super(directory, zip); }
    public void registerActivityLifecycleCallbacks(ActivityLifecycleCallbacks value) { callback=value; }
    public interface ActivityLifecycleCallbacks {
        void onActivityResumed(Object activity);
        void onActivityPaused(Object activity);
    }
}
