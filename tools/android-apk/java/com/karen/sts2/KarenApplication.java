package com.karen.sts2;

/** Uses the upstream Android/Mono runtime and seeds the selected launch profile. */
public final class KarenApplication extends com.godot.game.Sts2Application {
    @Override public void onCreate() {
        super.onCreate();
        KarenBundledMods.initialize(this);
    }
}
