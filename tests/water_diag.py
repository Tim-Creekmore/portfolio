import socket, json, sys, io

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.settimeout(30)
s.connect(('127.0.0.1', 13000))

def cmd(c):
    s.sendall((c + '\n').encode())
    buf = b''
    while b'\n' not in buf:
        buf += s.recv(8192)
    return json.loads(buf.split(b'\n')[0])

print('=== PING ===')
print(cmd('PING'))

print('\n=== STATS ===')
r = cmd('STATS')
print('FPS: %s' % r.get('fps'))
print('Camera: %s' % r.get('cameraPosition'))

print('\n=== FIND WATER ===')
print(cmd('FIND WaterMesh'))

print('\n=== FIND PLAYER ===')
r = cmd('FIND Player')
print(r)

print('\n=== MESHINFO Terrain ===')
print(json.dumps(cmd('MESHINFO Terrain'), indent=2))

print('\n=== POND TERRAIN SAMPLES ===')
samples = [(30,10), (28,10), (32,10), (30,8), (30,12), (25,10), (35,10)]
for x, z in samples:
    r = cmd('TERRAIN %d %d' % (x, z))
    h = float(r.get('height', 0))
    pond = r.get('isPond', False)
    pd = float(r.get('pondDistance', 0))
    biome = r.get('biome', '?')
    tag = ' ** WATER **' if pond else ''
    print('  (%2d,%2d) h=%5.2f dist=%5.2f biome=%-6s%s' % (x, z, h, pd, biome, tag))

print('\n=== PATH: SPAWN -> POND ===')
for step in range(11):
    fx = 20.0 + (30.0 - 20.0) * step / 10.0
    fz = 20.0 + (10.0 - 20.0) * step / 10.0
    r = cmd('TERRAIN %.1f %.1f' % (fx, fz))
    h = float(r.get('height', 0))
    pond = r.get('isPond', False)
    tag = ' ** WATER **' if pond else ''
    print('  (%5.1f,%5.1f) h=%5.2f%s' % (fx, fz, h, tag))

print('\n=== LOGS (errors/warnings only) ===')
r = cmd('LOG 50')
for e in r.get('entries', []):
    el = e.lower()
    if 'error' in el or 'warn' in el:
        print('  %s' % e)

s.close()
print('\nDone.')
