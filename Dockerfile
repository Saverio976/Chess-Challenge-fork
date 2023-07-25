FROM docker.io/archlinux:latest
RUN pacman-key --init
RUN pacman -Syu --noconfirm \
        dotnet-sdk-6.0      \
        mesa                \
    && pacman -Scc
COPY . /app/
WORKDIR /app/Chess-Challenge/
RUN cd /app/ && dotnet build
CMD dotnet run
