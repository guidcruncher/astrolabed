# Docker Compose

The Container image can be pulled from guidcruncher/astrolabed

```bash
docker pull docker.io/guidcruncher/astrolabed:latest
```

## Docker Run

```bash
docker run \
	-p 53:53/tcp \
	-p 53:53/udp \
	-p 67:67/udp \
        -p 68:68 \
	-p 123:123/udp \
        -p 1080:1080 \
	-v ./appsettings.json:/app/appsettings.json:ro \
	-v ./dnsforwarder:/var/lib/dnsforwarder \
        -v ./hosts:/dns-hosts \
        -v ./rules:/dns-rules \
	--cap-add NET_ADMIN \
	--cap-add SYS_TIME \
	--cap-add SYS_NICE \ 
        --cap-add CHOWN \
        --cap-add NET_BIND_SERVICE \
        --cap-add NET_RAW \
	docker.io/guidcruncher/astrolabed:latest
```

## Docker Compose 

```yaml
services:
  astrolabed:
    image: guidcruncher/astrolabed:latest
    container_name: astrolabed
    hostname: astrolabed
    restart: unless-stopped
    # DNS uses UDP port 53
    ports:
      - "53:53/tcp"
      - "53:53/udp"
      - "67:67/udp"
      - "68:68"
      - "123:123/udp"
      - "1080:1080"
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
      - ./dnsforwarder:/var/lib/dnsforwarder
      - ./hosts:/dns-hosts:ro
      - ./rules:/dns-rules:ro
    cap_add:
      - NET_ADMIN
      - SYS_TIME
      - SYS_NICE
      - CHOWN
      - NET_BIND_SERVICE
      - NET_RAW

    # Optional: run with host networking for maximum performance
    # network_mode: host
```
