# File: Makefile

SOLUTION = Astrolabed.Dns.sln
PROJECT = src/Astrolabed.Dns/Astrolabed.Dns.csproj
CONFIG ?= Release

.PHONY: all build run clean restore test publish format

all: build

restore:
	dotnet restore $(SOLUTION)

build: restore
	dotnet build $(SOLUTION) -c $(CONFIG) --no-restore

run: build
	dotnet run --project $(PROJECT) -c $(CONFIG) --no-build

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
