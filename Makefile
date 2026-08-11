SOLUTION = Astrolabed.sln
PROJECT = src/Astrolabed/Astrolabed.csproj
BUILD_DIR = bin/
RUNTIME = linux-x64
IMAGE_NAME ?= guidcruncher/astrolabed

NTP_HOST ?= 127.0.0.1
NTP_PORT ?= 1123

export QUERY_NTP
define QUERY_NTP
import socket, struct, time

s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
s.settimeout(3.0)
msg = b'\x1b' + 47 * b'\x00'

try:
    s.sendto(msg, ('$(NTP_HOST)', $(NTP_PORT)))
    data, _ = s.recvfrom(1024)
    if data:
        sec = struct.unpack('!I', data[40:44])[0] - 2208988800
        print(f'Response: {time.ctime(sec)}')
except Exception as e:
    print(f'Query failed: {e}')
endef

.PHONY: all build clean run dev test restore publish metrics benchmark format format-json dig docs mkdocs-install ntp docker-build docker-run docker-shell docker-run-dev docker-stop docker-publish

all: restore build

dig:
	dig itv.com @127.0.0.1 -p 1053
	dig +tcp google.com @127.0.0.1 -p 1053

ntp:
	@python3 -c "$$QUERY_NTP"

mkdocs-install:
	pip install --break-system-packages mkdocs mkdocs-material mkdocs-mermaid2-plugin

docs:
	mkdocs serve --dev-addr 0.0.0.0:8000 --config-file ./mkdocs.yml --watch ./docs

metrics:
	curl -v http://127.0.0.1:1080/metrics

restore:
	dotnet restore $(SOLUTION)

build:
	dotnet build $(SOLUTION) -c Release

format:
	dotnet format $(SOLUTION)

format-json:
	@for f in ./src/Astrolabed/*.json; do \
		[ -f "$$f" ] || continue; \
		echo "$$f"; \
		tmp=$$(mktemp) && { jq '.' "$$f" > "$$tmp" && mv "$$tmp" "$$f" || rm -f "$$tmp"; }; \
	done

run:
	dotnet run --project $(PROJECT) -c Release -- --config appsettings.json

dev:
	dotnet run --project $(PROJECT) -c Debug -- --config appsettings.Development.json

benchmark:
	dotnet run -c Release --project tests/Astrolabed.Benchmarks/Astrolabed.Benchmarks.csproj

test:
	dotnet test $(SOLUTION) -c Release

clean:
	dotnet clean $(SOLUTION)
	rm -rf $(BUILD_DIR) publish/ BenchmarkDotNet.Artifacts
	find . -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} +

publish:
	dotnet publish $(PROJECT) -c Release -r $(RUNTIME) --self-contained false -o publish/

docker-build:
	docker buildx build \
		--file ./Dockerfile \
		--tag docker.io/$(IMAGE_NAME):latest \
		--progress=plain \
		.

docker-run:
	docker compose -f ./docker-compose.yml down -v
	docker compose -f ./docker-compose.yml build --no-cache
	docker compose -f ./docker-compose.yml up -d
	docker compose -f ./docker-compose.yml logs -f

docker-shell:
	docker compose exec -it astrolabed bash

docker-run-dev:
	docker compose -f ./docker-compose-dev.yml down -v
	docker compose -f ./docker-compose-dev.yml build --no-cache
	docker compose -f ./docker-compose-dev.yml up -d
	docker compose -f ./docker-compose-dev.yml logs -f

docker-stop:
	docker compose -f ./docker-compose.yml down

docker-publish:
	docker buildx build \
		--file ./Dockerfile \
		--tag docker.io/$(IMAGE_NAME):latest \
		--progress=plain \
		--push \
		.
