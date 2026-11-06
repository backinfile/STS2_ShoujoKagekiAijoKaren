"""Passive target bounds and child ownership checks for real native attacks."""
def watch_attack(card,label):
    if card=='KAREN_SHINE_STRIKE_BARRAGE':cmd('barrage_probe watch '+str((case/(label+'-hit.png')).resolve()))
    if card=='KAREN_BASEBALL':cmd('baseball_probe watch')

def verify_attack(card,label):
    if card=='KAREN_SHINE_STRIKE_BARRAGE':
        report=json.loads(cmd('barrage_probe status')['message']);save(label+'-vfx-anchors',report)
        impacts=[n for n in report['effects'] if n['impact']]
        assert impacts and report['live']==report['childLive']==0,report
        assert all(n['exited'] and n['frames']>0 and n['maxAnchorError']<.2 for n in report['effects']),report
        assert all(n['maxBeyondSilhouette']>0 for n in impacts),report
    if card=='KAREN_BASEBALL':
        report=json.loads(cmd('baseball_probe status')['message']);save(label+'-vfx-anchors',report)
        assert report['live']==0 and report['casts'],report
        assert any(n['confirmed']>0 for n in report['casts']),report
        assert all(n['exited'] and n['maxAnchorError']<.2 for n in report['casts']),report
