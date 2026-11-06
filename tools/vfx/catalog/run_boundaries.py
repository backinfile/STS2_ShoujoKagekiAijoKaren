"""Run targeted boundary scenes with the existing isolated MCP harness.

The catalog recorder is a script, not an importable library. Add guarded scene
hooks in memory so this task does not edit the shared recorder (which may have
user changes in the original checkout).
"""
import hashlib,json,pathlib,subprocess,sys

record=pathlib.Path(__file__).with_name('record.py')
source=record.read_text(encoding='utf-8-sig')
original=source
anchor="    if wanted != {'pile-view'}: fight()"
assert source.count(anchor)==1,'Recorder initial-fight hook changed; inspect before running'
source=source.replace(anchor,"    if wanted != {'pile-view'} and not any(n.startswith('boundary-') for n in wanted): fight()")
anchor="    exec(compile((main/'tools/vfx/catalog/music_scenes.py')"
assert source.count(anchor)==1,'Recorder scene hook changed; inspect before running'
hook="""    if any(n.startswith('boundary-') for n in wanted):
        exec(compile((main/'tools/vfx/catalog/boundary_scenes.py').read_text(encoding='utf-8-sig'), 'boundary_scenes.py', 'exec'))
    if wanted.intersection({'boundary-states','boundary-actors','boundary-enemies','boundary-fixes'}) or any(n.startswith(('boundary-state-','boundary-actor-')) for n in wanted):
        exec(compile((main/'tools/vfx/catalog/adaptation_scenes.py').read_text(encoding='utf-8-sig'), 'adaptation_scenes.py', 'exec'))
"""
hook+="""    if 'boundary-enemies' in wanted:
        exec(compile((main/'tools/vfx/catalog/enemy_scenes.py').read_text(encoding='utf-8-sig'), 'enemy_scenes.py', 'exec'))
"""
hook+="""    if 'boundary-fixes' in wanted:
        exec(compile((main/'tools/vfx/catalog/fix_scenes.py').read_text(encoding='utf-8-sig'), 'fix_scenes.py', 'exec'))
"""
source=source.replace(anchor,hook+anchor)
if '--output' in sys.argv:
    out=pathlib.Path(sys.argv[sys.argv.index('--output')+1]).resolve();out.mkdir(parents=True,exist_ok=True)
    branch=next((a for a in sys.argv[1:] if a in ('stable','beta')),'unknown')
    (out/f'harness-{branch}.json').write_text(json.dumps(dict(recorderSha256=hashlib.sha256(original.encode()).hexdigest(),
        testSources={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in record.parent.glob('*') if p.name in ('boundary_scenes.py','adaptation_scenes.py','enemy_scenes.py','BoundaryProbe.cs','ActorStateProbe.cs','attack_observer.py')},
        probeSha256=hashlib.sha256((out/'probes'/branch/'KarenPromiseTransformTests.dll').read_bytes()).hexdigest(),
        instrumentedSha256=hashlib.sha256(source.encode()).hexdigest(),runnerSha256=hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest(),
        note='Only test scene hooks are injected in memory; shared recorder and production package are unchanged.'),indent=2),encoding='utf-8')
namespace={'__file__':str(record.resolve()),'__name__':'__main__'}
exec(compile(source,str(record),'exec'),namespace)
