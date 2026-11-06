using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Commands;
using System.Threading.Tasks;

namespace ShoujoKagekiAijoKaren.src.Core.Vfx;

public partial class NKarenBenefitVfx
{
    public bool UsesEnergyHud => GodotObject.IsInstanceValid(_energyCounter)&&_energyCounter!.IsInsideTree();
    public bool UsesPlayedCardSource => _radianceSourceCanvas.HasValue;
    public int VisualEnergyMotes => Mathf.Min(8,EnergyGain);
    private NCard? _radianceCard;
    private CardModel? _radianceModel;
    private Vector2? _radianceSourceCanvas;
    private Vector2 RadianceSource => _radianceSourceCanvas is {} point
        ? GetGlobalTransformWithCanvas().AffineInverse()*point : Vector2.Zero;
    private Vector2 EnergyTarget => UsesEnergyHud
        ? GetGlobalTransformWithCanvas().AffineInverse()*(_energyCounter!.GetGlobalTransformWithCanvas()*(_energyCounter.Size*.5f))
        : Body+Vector2.Down*35*Unit;

    // Keep the real card visible until the last observed energy mote has left its face.
    public Task WaitForEnergyDeparture()
    {
        if(!Alive||!Confirmed||EnergyGain<=0)return Task.CompletedTask;
        float duration=.22f+(VisualEnergyMotes-1)*.12f;
        return Cmd.CustomScaledWait(duration*.7f,duration);
    }

    private void CaptureRadianceSource()
    {
        if (Design!=BenefitKind.Radiance||Confirmed||!GodotObject.IsInstanceValid(_radianceCard)
            ||!_radianceCard!.IsInsideTree()||_radianceCard.Model!=_radianceModel)return;
        var portrait=_radianceCard.GetNode<TextureRect>("%Portrait");
        _radianceSourceCanvas=portrait.GetGlobalTransformWithCanvas()*(portrait.Size*.5f);
    }
    private void DrawRadianceCore()
    {
        if(!UsesPlayedCardSource)return;
        var center=RadianceSource;
        Glow(_hud,center,185,Blue,StoryAlpha*.12f);
        float a=Confirmed?1-P(ResultAge,0,.3f):P(Age,.1f,.48f);
        Star(_hud,center,17,0,Ivory,a*.7f);
        for(int j=0;j<18;j += KarenVfxQuality.ParticleStride){
            float u=P(Age,0,.6f),angle=Random(j)*Mathf.Tau;
            var point=center+Polar(angle,130*(1-Ease(u))+12,.7f);
            Grain(_hud,point,3+Random(j+5)*3,Blue,Mathf.Sin(u*Mathf.Pi)*.7f,angle);
        }
    }
    private void DrawEnergyDelivery()
    {
        if(!Confirmed||EnergyGain<=0||!UsesPlayedCardSource)return;
        var source=RadianceSource;var target=EnergyTarget;
        int count=VisualEnergyMotes;
        // The observed gain determines the count. Two/four arrivals are readable at normal speed.
        for(int i=0;i<count;i++){
            float t=ResultAge-i*.12f;
            if(t<0)continue;
            float u=Ease(P(t,.12f,.84f));
            var origin=source+new Vector2((i-(count-1)*.5f)*14,0)*Unit;
            if(t<.86f){
                float a=Window(t,0,.88f,.06f,.12f);
                var point=Arc(origin,target,u,95+i*8);
                Trail(_hud,origin,target,u,Blue,a,95+i*8);
                Star(_hud,point,13,0,Ivory,a);
                Glow(_hud,point,52,Blue,a*.8f);
            }
            float hit=Window(t,.72f,1.08f,.05f,.22f);
            Glow(_hud,target,115,Blue,hit*.55f);
            Star(_hud,target,22,0,Blue,hit*.6f);
        }
    }
}
