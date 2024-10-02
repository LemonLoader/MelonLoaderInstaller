#!/bin/bash

if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <source_directory> <destination_directory>"
    exit 1
fi

SOURCE_DIR="$1"
DEST_DIR="$2"

if [ ! -d "$SOURCE_DIR" ]; then
    echo "Source directory '$SOURCE_DIR' does not exist, giving up"
    exit 0
fi

mkdir -p "$DEST_DIR"

find "$SOURCE_DIR" -type f | while read filepath; do
    dest_path="$DEST_DIR/${filepath#$SOURCE_DIR/}"
    mkdir -p "$(dirname "$dest_path")"
    dd if="$filepath" of="$dest_path" bs=4K
    chmod 644 "$dest_path"
    if [ $? -eq 0 ]; then
        rm "$filepath"
    else
        echo "Error: Failed to copy $filepath"
    fi
done

find "$SOURCE_DIR" -type d -empty -delete
