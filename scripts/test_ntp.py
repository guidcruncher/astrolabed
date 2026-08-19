import socket, struct, time
s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
s.settimeout(3.0)
msg = b'\x1b' + 47 * b'\x00'
try:
    s.sendto(msg, ('127.0.0.1', 1123))
    data, _ = s.recvfrom(1024)
    if data:
        sec = struct.unpack('!I', data[40:44])[0] - 2208988800
        print(f'Response: {time.ctime(sec)}')
except Exception as e:
    print(f'Query failed: {e}')
