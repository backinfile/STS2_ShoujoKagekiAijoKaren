import json,pathlib,math,hashlib
from PIL import Image
root=pathlib.Path('docs/worktree-tasks/karen-energy-concepts')
p=root/'style-assets/regular-nonagon.png'
im=Image.open(p).convert('RGBA');alpha=im.getchannel('A');mask=alpha.point(lambda a:255 if a>128 else 0);x,y,r,b=mask.getbbox()
points=[]
for yy in range(y,b):
 xs=[xx for xx in range(x,r) if mask.getpixel((xx,yy))]
 if xs:points.extend([(xs[0],yy),(xs[-1],yy)])
points=sorted(set(points))
def cross(o,a,b):return (a[0]-o[0])*(b[1]-o[1])-(a[1]-o[1])*(b[0]-o[0])
lower=[]
for v in points:
 while len(lower)>=2 and cross(lower[-2],lower[-1],v)<=0:lower.pop()
 lower.append(v)
upper=[]
for v in reversed(points):
 while len(upper)>=2 and cross(upper[-2],upper[-1],v)<=0:upper.pop()
 upper.append(v)
hull=lower[:-1]+upper[:-1]
def simplify(ps,tol):
 a,c=ps[0],ps[-1];den=math.dist(a,c)
 ds=[abs(cross(a,c,v))/den if den else math.dist(a,v) for v in ps]
 idx=max(range(len(ds)),key=ds.__getitem__)
 if ds[idx]<=tol:return [a,c]
 return simplify(ps[:idx+1],tol)[:-1]+simplify(ps[idx:],tol)
k=max(range(len(hull)),key=lambda i:math.dist(hull[0],hull[i]))
verts=simplify(hull[:k+1],5)[:-1]+simplify(hull[k:]+[hull[0]],5)[:-1]
lengths=[math.dist(verts[i],verts[(i+1)%len(verts)]) for i in range(len(verts))]
angles=[]
for i,v in enumerate(verts):
 a=verts[i-1];c=verts[(i+1)%len(verts)];u=(a[0]-v[0],a[1]-v[1]);w=(c[0]-v[0],c[1]-v[1]);angles.append(math.degrees(math.acos((u[0]*w[0]+u[1]*w[1])/(math.hypot(*u)*math.hypot(*w)))))
report={'alphaThreshold':128,'simplificationTolerancePixels':5,'principalVertices':verts,'principalSideCount':len(verts),'sideLengths':lengths,'sideLengthRelativeRange':(max(lengths)-min(lengths))/(sum(lengths)/len(lengths)),'interiorAnglesDegrees':angles,'visibleBounds':[x,y,r-x,b-y],'visibleAspect':(r-x)/(b-y),'transparentPixels':alpha.histogram()[0],'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'pixelsModified':False,'geometryGuide':'exact regular nonagon','bitmapNote':'Hand-painted nonagon approximation; bitmap is not a mathematically exact regular polygon.'}
assert len(verts)==9 and report['transparentPixels']>0,report
(root/'nonagon/shape-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'sides':len(verts),'visibleAspect':report['visibleAspect'],'sideLengthRelativeRange':report['sideLengthRelativeRange']}))
