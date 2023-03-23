
all: build run

build:
	docker build .. -f ./Dockerfile -t apexsdk-unity-docs

run:
	docker run --rm -it -p 3000:3000 apexsdk-unity-docs
