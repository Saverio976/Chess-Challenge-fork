FROM docker.io/archlinux:latest

RUN pacman -Syyu --noconfirm && pacman -S --noconfirm dotnet-sdk-6.0 mesa

COPY . /code

WORKDIR /code
RUN dotnet build

WORKDIR /code/Chess-Challenge
CMD dotnet run
