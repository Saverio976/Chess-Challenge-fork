#!/bin/bash

sudo docker build . -t chess

sudo docker run --rm --name chess          \
    --network host                         \
    -e XAUTHORITY=/app/.Xauthority         \
    -v "$XAUTHORITY:/app/.Xauthority:ro"   \
    --device /dev/dri/                     \
    -e DISPLAY                             \
    -e XDG_RUNTIME_DIR                     \
    -v /dev/shm/:/dev/shm/                 \
    -v /tmp/.X11-unix/:/tmp/.X11-unix/     \
    -v "$XDG_RUNTIME_DIR:$XDG_RUNTIME_DIR" \
    -v "$HOME/GUI/steam/:/app/"            \
    chess
