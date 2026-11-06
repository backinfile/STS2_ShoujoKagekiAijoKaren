import java.io.*;
import java.util.*;
import org.jf.dexlib2.*;
import org.jf.dexlib2.iface.*;
import org.jf.dexlib2.immutable.ImmutableDexFile;

/** Extract engine/dependency DEX files, with every upstream application class removed. */
public final class StripLauncher {
    public static void main(String[] args) throws Exception {
        File output=new File(args[1]);output.mkdirs();
        MultiDexContainer<? extends DexFile> container=DexFileFactory.loadDexContainer(new File(args[0]),Opcodes.forApi(35));
        int kept=0,removed=0,number=0;
        Map<String,Set<String>> methods=new HashMap<>();
        List<String> names=new ArrayList<>(container.getDexEntryNames());Collections.sort(names);
        for(String name:names){
            DexFile file=container.getEntry(name).getDexFile();
            List<ClassDef> classes=new ArrayList<>();
            for(ClassDef definition:file.getClasses()){
                if(definition.getType().startsWith("Lcom/godot/game/")){removed++;continue;}
                classes.add(definition);kept++;
                Set<String> signatures=new HashSet<>();
                for(Method method:definition.getMethods()){
                    StringBuilder signature=new StringBuilder(method.getName()+"(");
                    for(CharSequence type:method.getParameterTypes())signature.append(type);
                    signatures.add(signature+")"+method.getReturnType());
                }
                methods.put(definition.getType(),signatures);
            }
            if(classes.isEmpty())continue;
            number++;
            DexFileFactory.writeDexFile(new File(output,number==1?"classes.dex":"classes"+number+".dex").toString(),new ImmutableDexFile(file.getOpcodes(),classes));
        }
        if(removed==0)throw new IllegalStateException("No launcher classes found");
        require(methods,"Lorg/godotengine/godot/GodotActivity;", "<init>()V", "onCreate(Landroid/os/Bundle;)V", "getCommandLine()Ljava/util/List;", "getGodot()Lorg/godotengine/godot/Godot;", "onGodotRestartRequested(Lorg/godotengine/godot/Godot;)V");
        require(methods,"Lorg/fmod/FMOD;", "init(Landroid/content/Context;)V", "close()V", "setAudioCompatibilityMode(Z)V");
        if(!methods.containsKey("Lcom/utopiarise/godot/fmod/android/plugin/FmodPlugin;"))throw new IllegalStateException("Missing Godot FMOD plugin");
        if(!methods.containsKey("Lorg/godotengine/godot/utils/ProcessPhoenix;"))throw new IllegalStateException("Missing engine process restart dependency");
        System.out.println("Stripped "+removed+" application classes; retained "+kept+" engine/dependency classes in "+number+" DEX files");
    }
    private static void require(Map<String,Set<String>> methods,String type,String... signatures){
        for(String signature:signatures)if(!methods.containsKey(type)||!methods.get(type).contains(signature))throw new IllegalStateException("Missing runtime API: "+type+"->"+signature);
    }
}
