#!/usr/bin/env bash
# NUM="16"; convert "shcable_$NUM.png" -page -2+0 -background none -flatten -gamma 0.4:0.4:0.4 "shcable_$NUM.png"
#

#left
nums=(0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 )

for NUM in "${nums[@]}"
do
  input_file="lvcable_${NUM}.png"
  convert "$input_file" -page -5+0 -background none -flatten -gamma 0.4:0.4:0.4 "shcable_${NUM}.png"
  echo "Processed $input_file"
done
