# File: Makefile
SOLUTION = Astrolabed.sln
PROJECT = src/Astrolabed.Main/Astrolabed.Main.csproj
CONFIG ?= Release
IMAGE_NAME = guidcruncher/astrolabed

.PHONY: all build run clean restore test publish format dev dns ntp dhcp benchmark \
        docker-build docker-run docker-shell docker-run-dev docker-stop docker-publish docker-release \
	db docs

all: build

db:
	sqlite3 ../netdns-runtime/astrolabed.db

docs:
	dotnet tool update -g docfx
	docfx docfx.json --serve --hostname 0.0.0.0 --port 8000
	rm -rf ./_site ./api

restore:
	dotnet restore $(SOLUTION)

build: restore
	dotnet build $(SOLUTION) -c $(CONFIG) --no-restore

run: build
	dotnet run --project $(PROJECT) -c $(CONFIG) --no-build

dev:
	dotnet run --project $(PROJECT) -c Development -- --environment Development

benchmark:
	@python3 ./scripts/benchmark_dns.py --ip 127.0.0.1 --port 1053

dns:
	@dig @127.0.0.1 -p 1053 bbc.com A
	@dig @127.0.0.1 -p 1053 +tcp google.com A
	@dig @127.0.0.1 -p 1053 webtop.lan A
	@dig @127.0.0.1 -p 1053 example.com A
	@dig @127.0.0.1 -p 1053 -x 192.168.1.1

ntp:
	@python3 ./scripts/test_ntp.py --ip 127.0.0.1 --port 1123

dhcp:
	@sudo python3 ./scripts/test_dhcp.py -s 192.168.1.202 -p 1167 -c 68 -m 02:00:00:00:00:01

test:
	dotnet test $(SOLUTION) -c $(CONFIG)

publish:
	dotnet publish $(PROJECT) -c $(CONFIG) -o ./publish

clean:
	dotnet clean $(SOLUTION)
	rm -rf ./publish
	find . -type d \( -name bin -o -name obj \) -exec rm -rf {} +
	rm -rf ./_site ./api

format:
	@for f in *.json; do \
		[ -f "$$f" ] || continue; \
		echo "$$f"; \
		tmp=$$(mktemp) && { jq '.' "$$f" > "$$tmp" && mv "$$tmp" "$$f" || rm -f "$$tmp"; }; \
	done
	dotnet format $(SOLUTION)

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
	docker compose -f ./docker-compose.dev.yml down -v
	docker compose -f ./docker-compose.dev.yml build --no-cache
	docker compose -f ./docker-compose.dev.yml up -d
	docker compose -f ./docker-compose.dev.yml logs -f

docker-stop:
	docker compose -f ./docker-compose.yml down

docker-publish:
	docker buildx create --name astrolabed-builder --use --bootstrap
	-docker buildx build \
		--platform linux/arm64 \
		--builder astrolabed-builder \
		--file ./Dockerfile \
		--tag docker.io/$(IMAGE_NAME):dev \
		--progress=plain \
		--push \
		.
	docker buildx rm -f astrolabed-builder

docker-release:
	docker buildx create --name astrolabed-builder --use --bootstrap
	-docker buildx build \
		--builder astrolabed-builder \
		--platform linux/amd64,linux/arm64 \
		--file ./Dockerfile \
		--tag docker.io/$(IMAGE_NAME):latest \
		--tag docker.io/$(IMAGE_NAME):dev \
		--progress=plain \
		--push \
		.
	docker buildx rm -f astrolabed-builder
