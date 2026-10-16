"""Decode an in-game wall clock from the actual video, rather than trusting MP4 FPS."""
import argparse,json,pathlib,subprocess
import numpy as np

p=argparse.ArgumentParser();p.add_argument('folder',type=pathlib.Path);a=p.parse_args()
meta=json.loads((a.folder/'recording.json').read_text(encoding='utf-8'))
schema=json.loads((a.folder/'clock.json').read_text(encoding='utf-8'))
assert meta.get('encoder') in ['h264_nvenc','libx264'] and 'frames_dropped' in meta,'Updated MCP recording telemetry is missing'
assert meta['status']=='completed' and meta['capture_fps']>=27,meta
assert meta['frames_repeated']<=meta['frames_written']*.1 and meta['frames_dropped']<=meta['frames_captured']*.1,meta
info=json.loads(subprocess.check_output(['ffprobe','-v','error','-show_streams','-show_frames','-select_streams','v','-of','json',str(a.folder/'game.mp4')],text=True))
stream=info['streams'][0];width,height=stream['width'],stream['height']
raw=subprocess.check_output(['ffmpeg','-hide_banner','-loglevel','error','-i',str(a.folder/'game.mp4'),'-an','-pix_fmt','rgb24','-f','rawvideo','-'])
frames=np.frombuffer(raw,dtype=np.uint8).reshape((-1,height,width,3))
scale=np.array([width,height])/np.array(schema['viewport'])
xs=np.rint((schema['position'][0]+schema['cellCenter'][0]+np.arange(schema['bits'])*schema['cellStep'])*scale[0]).astype(int)
y=round((schema['position'][1]+schema['cellCenter'][1])*scale[1])
values=frames[:,y,xs,:].mean(axis=2)>128
gray=(values*(1<<np.arange(schema['bits']))).sum(axis=1).astype(np.int64)
binary=gray.copy();shift=gray>>1
while np.any(shift): binary^=shift;shift>>=1
clock=binary*schema['stepMs']/1000
pts=np.array([float(f['best_effort_timestamp_time']) for f in info['frames']])
assert len(pts)==len(clock) and np.all(np.diff(pts)>0),'Non-monotonic video timestamps'
assert np.all(np.diff(clock)>=0),'Decoded clock goes backwards; inspect the clock crop or captured frames'
changes=np.flatnonzero(np.r_[True,np.diff(clock)!=0,True])
longest_run=int(np.diff(changes).max())
gaps=np.diff(clock)
report={'encoder':meta['encoder'],'captureFps':meta['capture_fps'],'repeatedFrames':meta['frames_repeated'],'droppedFrames':meta['frames_dropped'],
        'decodedFrames':len(clock),'distinctClockValues':len(set(clock.tolist())),'longestUnchangedClockSeconds':longest_run/30,
        'largestClockJumpSeconds':float(gaps.max()),'clockSeconds':float(clock[-1]-clock[0]),'videoSeconds':float(pts[-1]-pts[0]),
        'timelineDriftSeconds':float(abs((clock[-1]-clock[0])-(pts[-1]-pts[0]))),'strictlyIncreasingPts':True}
(a.folder/'capture-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
assert report['longestUnchangedClockSeconds']<=.2 and report['largestClockJumpSeconds']<=.2,report
assert report['timelineDriftSeconds']<.15,report
print(json.dumps(report,indent=2))
