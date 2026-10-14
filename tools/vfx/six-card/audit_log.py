"""Fail on runtime errors; separately retain the pre-existing beta startup worker-pool diagnostic."""
def audit(text):
    before_play=True;known=[];unexpected=[]
    for number,line in enumerate(text.splitlines(),1):
        if ' playing card ' in line:before_play=False
        if line.startswith('ERROR:') or line.startswith('[ERROR]') or 'Unhandled exception' in line:
            item={'line':number,'message':line}
            if before_play and line=='ERROR: Invalid Task ID':known.append(item)
            else:unexpected.append(item)
    return {'knownStartupWorkerPoolErrors':known,'unexpectedErrors':unexpected}
