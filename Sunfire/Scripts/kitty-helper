#!/usr/bin/env bash

# Transmits an image in png format via file-mode transmission.
#   $1 file path
#   $2 image id
transmit_file_png() {
	abspath_b64="$(printf -- "$(realpath -- "$1")" | base64 -w0)"
	printf "\e_Gt=f,i=$2,f=100,q=1;$abspath_b64\e\\" >/dev/tty
}

# Displays an already transferred image.
#   $1 image id
#   $2 placement id
#   $3 x, $4 y, $5 w, $6 h
display_img() {
	printf "\e[s" >/dev/tty # save cursor position
	tput cup $4 $3 >/dev/tty # move cursor
	printf "\e_Ga=p,i=$1,p=$2,c=$5,r=$6,q=1\e\\" >/dev/tty
	printf "\e[u" >/dev/tty # restore cursor position
}

# Deletes a displayed image.
#   $1 image id
#   $2 placement id
delete_img() {
	printf "\e_Ga=d,d=I,i=$1,p=$2,q=1\e\\" >/dev/tty
}

# Displays an image within a given area.
# - If the image is larger than the area, it is scaled down to fit.
# - If the image is smaller than the area, it is displayed at its natural size.
#
#   $1 file path
#   $2 image id
#   $3 placement id
#   $4 x position of the area (cell column)
#   $5 y position of the area (cell row)
#   $6 max width of the area (in terminal columns)
#   $7 max height of the area (in terminal rows)
#   $8 width of a single terminal cell in PIXELS
#   $9 height of a single terminal cell in PIXELS
show() {
	local file_path="$1"
	local image_id="$2"
	local placement_id="$3"
	local x_pos="$4"
	local y_pos="$5"
	local max_w_cells="$6"
	local max_h_cells="$7"
	local cell_w_px="${8:-9}"   # Default cell width if not provided
	local cell_h_px="${9:-20}"   # Default cell height if not provided

	# Get image dimensions in pixels using ImageMagick's 'identify'.
	img_dims=$(identify -format "%w %h" "$file_path")
	img_w_px=$(echo "$img_dims" | cut -d' ' -f1)
	img_h_px=$(echo "$img_dims" | cut -d' ' -f2)

	# Calculate the maximum available area in pixels.
	local max_w_px=$((max_w_cells * cell_w_px))
	local max_h_px=$((max_h_cells * cell_h_px))

	local final_w_cells
	local final_h_cells

	# DECISION: Does the image need to be scaled down?
	if [ "$img_w_px" -gt "$max_w_px" ] || [ "$img_h_px" -gt "$max_h_px" ]; then
		# --- CASE 1: Image is LARGER than the pane. Scale it down. ---
		# Compare aspect ratios in pixels to decide whether to fit to width or height.
		if [ $((img_w_px * max_h_px)) -gt $((max_w_px * img_h_px)) ]; then
			# Image is proportionally WIDER than the pane. Fit to pane width.
			final_w_cells=$max_w_cells
			# Calculate proportional height in cells.
			final_h_cells=$((img_h_px * max_w_cells * cell_w_px / (img_w_px * cell_h_px)))
		else
			# Image is proportionally TALLER than the pane. Fit to pane height.
			final_h_cells=$max_h_cells
			# Calculate proportional width in cells.
			final_w_cells=$((img_w_px * max_h_cells * cell_h_px / (img_h_px * cell_w_px)))
		fi
	else
		# --- CASE 2: Image is SMALLER than or fits in the pane. Use its natural size. ---
		# Convert the image's pixel dimensions directly to cell dimensions.
		final_w_cells=$((img_w_px / cell_w_px))
		final_h_cells=$((img_h_px / cell_h_px))
	fi
	
	# Ensure we don't end up with zero-dimension cells for very small images.
	if [ "$final_w_cells" -eq 0 ]; then final_w_cells=1; fi
	if [ "$final_h_cells" -eq 0 ]; then final_h_cells=1; fi

	transmit_file_png "$file_path" "$image_id"
	display_img "$image_id" "$placement_id" "$x_pos" "$y_pos" "$final_w_cells" "$final_h_cells"
}

"$@"
