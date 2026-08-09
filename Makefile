SOLUTION = Astrolabed.sln
PROJECT = src/Astrolabed/Astrolabed.csproj
TESTSDNS = tests/Astrolabed.Dns.Tests/Astrolabed.Dns.Tests.csproj
TESTSDHCP = tests/Astrolabed.Dhcp.Tests/Astrolabed.Dhcp.Tests.csproj
TESTSNTP = tests/Astrolabed.Ntp.Tests/Astrolabed.Ntp.Tests.csproj
BUILD_DIR = bin/
RUNTIME = linux-x64

.PHONY: all build clean run test restore publish metrics dev benchmark format dig docs mkdocs-install

all: restore build

dig:
	dig itv.com @127.0.0.1 -p 1053

mkdocs-install:
	pip install mkdocs --break-system-packages
	pip install mkdocs-material --break-system-packages
	pip install mkdocs-mermaid2-plugin --break-system-packages

docs:
	mkdocs serve --dev-addr 0.0.0.0:8000 --config-file ./mkdocs.yml --watch ./docs

metrics:
	curl http://127.0.0.1:1080/metrics  -v

restore:
	dotnet restore $(SOLUTION)

build:
	dotnet build $(SOLUTION) -c Release

format:
	dotnet format ${SOLUTION}

run:
	dotnet run --project $(PROJECT) -c Release -- --config appsettings.json

dev:
	dotnet run --project ${PROJECT} -c Debug -- --config appsettings.Development.json

benchmark:
	dotnet run -c Release --project tests/Astrolabed.Benchmarks/Astrolabed.Benchmarks.csproj

test:
	dotnet test $(TESTSDNS) -c Release --no-build
	dotnet test $(TESTSDHCP) -c Release --no-build
	dotnet test ${TESTSNTP} -c Release --no-build

clean:
	rm -rf $(BUILD_DIR)
	rm -rf ./BenchmarkDotNet.Artifacts
	rm -rf ./tests/Astrolabed.Dhcp.Tests/bin
	rm -rf ./tests/Astrolabed.Dns.Tests/bin
	rm -rf ./tests/Astrolabed.Benchmarks/bin
	rm -rf ./tests/Astrolabed.Ntp.Tests/bin
	rm -rf ./tests/Astrolabed.Dhcp.Tests/obj
	rm -rf ./tests/Astrolabed.Dns.Tests/obj
	rm -rf ./tests/Astrolabed.Ntp.Tests/obj
	rm -rf ./tests/Astrolabed.Benchmarks/obj
	dotnet clean $(SOLUTION)

publish:
	dotnet publish $(PROJECT) -c Release -r $(RUNTIME) --self-contained false -o publish/

docker-build:
	docker buildx build \
		--file ./Dockerfile \
		--tag docker.io/$(IMAGE_NAME):latest \
		--progress=plain
		.

docker-run:
	docker compose -f ./docker-compose.yml down
	docker compose -f ./docker-compose.yml rm -f
	docker compose -f ./docker-compose.yml build --no-cache
	docker compose -f ./docker-compose.yml up -d
	docker compose -f ./docker-compose.yml logs -f

docker-shell:
	docker compose exec -i -t astrolabed bash

docker-run-dev:
	docker compose -f ./docker-compose-dev.yml down
	docker compose -f ./docker-compose-dev.yml rm -f
	docker compose -f ./docker-compose-dev.yml build --no-cache
	docker compose -f ./docker-compose-dev.yml up -d
	docker compose -f ./docker-compose-dev.yml logs -f

docker-stop:
	docker compose -f ./docker-compose.yml stop
	docker compose -f ./docker-compose.yml rm -f

docker-publish: ## Build the Docker image
	docker buildx build \
		--file ./Dockerfile \
		--tag docker.io/guidcruncher/astrolabed:latest \
		--progress=plain \
		--push \
		.
