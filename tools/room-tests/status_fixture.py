"""Targeted live-page fixture; all sockets close automatically or on stdin 'stop'."""
import argparse
import asyncio
import json
from pathlib import Path
import secrets
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "server/karen-rooms"))
from websockets.asyncio.client import connect

async def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--endpoint",default="wss://47.95.213.214:8443/rooms")
    parser.add_argument("--hold",type=float,default=0)
    args=parser.parse_args()
    async with connect(args.endpoint,subprotocols=["karen-rooms-v1"],proxy=None) as host:
        await host.send(json.dumps({"op":"create","protocol":1,"version":"status-page-fixture","credential":secrets.token_hex(32)}))
        room=json.loads(await host.recv())
        if room.get("op") != "welcome": raise RuntimeError(room)
        async with connect(args.endpoint,subprotocols=["karen-rooms-v1"],proxy=None) as client:
            await client.send(json.dumps({"op":"join","protocol":1,"version":"status-page-fixture","credential":secrets.token_hex(32),"code":room["code"]}))
            joined=json.loads(await client.recv())
            if joined.get("op") != "welcome": raise RuntimeError(joined)
            await host.recv()
            print("READY code="+room["code"]+" players=2",flush=True)
            if args.hold:
                await asyncio.sleep(min(30,args.hold))
                await host.send('{"op":"set_closed","closed":true}')
                print("STARTED",flush=True)
                await asyncio.sleep(max(0,args.hold-30))
                return
            try:
                async with asyncio.timeout(240):
                    while True:
                        command=(await asyncio.to_thread(sys.stdin.readline)).strip()
                        if command=="start":
                            await host.send('{"op":"set_closed","closed":true}'); print("STARTED",flush=True)
                        elif command=="stop" or not command: break
            except TimeoutError: pass
    print("CLOSED",flush=True)

asyncio.run(main())
