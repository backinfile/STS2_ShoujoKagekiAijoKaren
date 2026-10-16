"""Fail on runtime errors; separately retain the pre-existing beta startup worker-pool diagnostic."""
import re

def audit(text):
    startup=True;known=[];unexpected=[];lines=text.splitlines()
    for number,line in enumerate(lines,1):
        if "Preloading 'Common' Complete:" in line or ' playing card ' in line:startup=False
        if line.startswith('ERROR:') or line.startswith('[ERROR]') or 'Unhandled exception' in line:
            item={'line':number,'message':line}
            source_index=number
            # Worker threads can interleave identical errors and Godot may print
            # a managed backtrace before the native source. Stay within this
            # diagnostic block; never scan past ordinary output or other errors.
            while source_index<min(len(lines),number+32) and re.fullmatch(
                r'ERROR: Invalid Task ID|\s*C# backtrace \(most recent call first\):|\s*\[\d+\] .+',
                lines[source_index]):source_index+=1
            next_line=lines[source_index] if source_index<len(lines) else ''
            if startup and line=='ERROR: Invalid Task ID' and 'wait_for_task_completion (core/object/worker_thread_pool.cpp:' in next_line:known.append(item)
            else:unexpected.append(item)
    return {'knownStartupWorkerPoolErrors':known,'unexpectedErrors':unexpected}
