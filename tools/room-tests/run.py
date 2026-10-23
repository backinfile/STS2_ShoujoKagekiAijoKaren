"""Run the actual .NET WebSocket adapter against the actual Python relay."""
import asyncio
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "server/karen-rooms"))
from server import RoomService, SUBPROTOCOL, MAX_FRAME
from websockets.asyncio.server import serve


async def main():
    service = RoomService()
    async with serve(service.handle, "127.0.0.1", 0, subprotocols=[SUBPROTOCOL],
                     compression=None, max_size=MAX_FRAME, close_timeout=.2) as server:
        port = server.sockets[0].getsockname()[1]
        process = await asyncio.create_subprocess_exec("dotnet", "run", "--project",
            str(ROOT / "tools/room-tests/RoomSocketTests.csproj"), "-c", "Release", "--no-build",
            "--", f"ws://127.0.0.1:{port}/rooms", cwd=ROOT)
        return await asyncio.wait_for(process.wait(), 45)


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
