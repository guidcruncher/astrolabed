# File: Makefile

SOLUTION = Astrolabed.Dns.sln
PROJECT = src/Astrolabed.Dns/Astrolabed.Dns.csproj
CONFIG ?= Release
IMAGE_NAME = guidcruncher/astrolabed

.PHONY: all build run clean restore test publish format dev dns dhcp benchmark

all: build

restore:
	dotnet restore $(SOLUTION)

build: restore
	dotnet build $(SOLUTION) -c $(CONFIG) --no-restore

run: build
	dotnet run --project $(PROJECT) -c $(CONFIG) --no-build

dev:
	dotnet run --project $(PROJECT) -c Development --environment Development

benchmark:
	@python3 ./scripts/benchmark_dns.py --ip 127.0.0.1 --port 1053
dns:
	@dig @127.0.0.1 -p 1053 bbc.com A
	@dig @127.0.0.1 -p 1053 +tcp google.com A
	@dig @127.0.0.1 -p 1053 webtop.lan A
	@dig @127.0.0.1 -p 1053 example.com A
	@dig @127.0.0.1 -p 1053 -x 192.168.1.1

ntp:
	@python3 ././scripts/test_ntp.py

dhcp:
	@sudo python3 ./scripts/test_dhcp.py --server-port 1067 --client-port 68
	@sudo python3 ./scripts/test_dhcp.py --server-port 1067 --client-port 68 --mac "11:22:33:44:55:66" --hostname "voip-phone-01" --vendor-class "Cisco IP Phone 7940" --timeout 3.0

test:
	dotnet test $(SOLUTION) -c $(CONFIG)

publish:
	dotnet publish $(PROJECT) -c $(CONFIG) -o ./publish

clean:
	dotnet clean $(SOLUTION)
	rm -rf ./publish
	rm -rf src/Astrolabed.Dns/bin src/Astrolabed.Dns/obj

format:
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
	docker buildx build \
		--file ./Dockerfile \
		--tag docker.io/$(IMAGE_NAME):dev \
		--progress=plain \
		--push \
		.
docker-release:
	docker buildx build \
		--file ./Dockerfile \
		--tag docker.io/$(IMAGE_NAME):latest \
		--tag docker.io/${IMAGE_NAME}:dev \
		--progress=plain \
		--push \
		.
