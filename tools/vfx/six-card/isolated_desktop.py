"""Run only test games on a private Windows desktop, without switching the user's desktop.

Godot still renders its viewport for the MCP capture API. Closing this object terminates its
owned games and releases the desktop. No other application's window or focus is changed.
"""
import ctypes, os, subprocess, time, uuid
from ctypes import wintypes as W

class StartupInfo(ctypes.Structure):
    _fields_=[('cb',W.DWORD),('lpReserved',W.LPWSTR),('lpDesktop',W.LPWSTR),('lpTitle',W.LPWSTR),
        ('dwX',W.DWORD),('dwY',W.DWORD),('dwXSize',W.DWORD),('dwYSize',W.DWORD),
        ('dwXCountChars',W.DWORD),('dwYCountChars',W.DWORD),('dwFillAttribute',W.DWORD),
        ('dwFlags',W.DWORD),('wShowWindow',W.WORD),('cbReserved2',W.WORD),('lpReserved2',ctypes.c_void_p),
        ('hStdInput',W.HANDLE),('hStdOutput',W.HANDLE),('hStdError',W.HANDLE)]

class ProcessInfo(ctypes.Structure):
    _fields_=[('hProcess',W.HANDLE),('hThread',W.HANDLE),('dwProcessId',W.DWORD),('dwThreadId',W.DWORD)]

kernel=ctypes.WinDLL('kernel32',use_last_error=True)
user=ctypes.WinDLL('user32',use_last_error=True)
user.CreateDesktopW.argtypes=[W.LPCWSTR,W.LPCWSTR,ctypes.c_void_p,W.DWORD,W.DWORD,ctypes.c_void_p]
user.CreateDesktopW.restype=W.HANDLE
user.CloseDesktop.argtypes=[W.HANDLE]
kernel.CreateProcessW.argtypes=[W.LPCWSTR,W.LPWSTR,ctypes.c_void_p,ctypes.c_void_p,W.BOOL,W.DWORD,
    ctypes.c_void_p,W.LPCWSTR,ctypes.POINTER(StartupInfo),ctypes.POINTER(ProcessInfo)]
kernel.CreateProcessW.restype=W.BOOL
kernel.GetExitCodeProcess.argtypes=[W.HANDLE,ctypes.POINTER(W.DWORD)]
kernel.TerminateProcess.argtypes=[W.HANDLE,W.UINT]
kernel.WaitForSingleObject.argtypes=[W.HANDLE,W.DWORD]
kernel.CloseHandle.argtypes=[W.HANDLE]

class GameProcess:
    def __init__(self,info):self.pid=info.dwProcessId;self.handle=info.hProcess
    def poll(self):
        code=W.DWORD()
        if not kernel.GetExitCodeProcess(self.handle,ctypes.byref(code)):raise ctypes.WinError(ctypes.get_last_error())
        return None if code.value==259 else code.value
    def terminate(self):
        if self.poll() is None:kernel.TerminateProcess(self.handle,0)
    def wait(self,timeout=15):
        if kernel.WaitForSingleObject(self.handle,int(timeout*1000))==258:raise TimeoutError('Test game did not stop')
        return self.poll()
    def close(self):
        if self.handle:
            self.terminate();self.wait();kernel.CloseHandle(self.handle);self.handle=None

class TestDesktop:
    def __init__(self):
        self.name='KarenVfxTest_'+uuid.uuid4().hex
        self.handle=user.CreateDesktopW(self.name,None,None,0,0x10000000,None)
        if not self.handle:raise ctypes.WinError(ctypes.get_last_error())
        self.processes=[]
    def launch(self,argv,cwd,env):
        startup=StartupInfo();startup.cb=ctypes.sizeof(startup);startup.lpDesktop='WinSta0\\'+self.name
        startup.dwFlags=1;startup.wShowWindow=0
        command=ctypes.create_unicode_buffer(subprocess.list2cmdline([str(a) for a in argv]))
        environment=ctypes.create_unicode_buffer('\0'.join(f'{k}={v}' for k,v in sorted(env.items(),key=lambda item:item[0].upper()))+'\0\0')
        info=ProcessInfo()
        if not kernel.CreateProcessW(str(argv[0]),command,None,None,False,0x400,environment,str(cwd),ctypes.byref(startup),ctypes.byref(info)):
            raise ctypes.WinError(ctypes.get_last_error())
        kernel.CloseHandle(info.hThread)
        process=GameProcess(info);self.processes.append(process);return process
    def close(self):
        for process in self.processes:process.close()
        if self.handle:user.CloseDesktop(self.handle);self.handle=None
