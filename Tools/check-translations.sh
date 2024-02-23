#!/bin/env bash
#set -x

locale_path=Resources/Locale

eng_translation_name=en-US
ru_translation_name=ru-RU

eng_translation=$locale_path/$eng_translation_name
ru_translation=$locale_path/$ru_translation_name

eng_files=`find $eng_translation -type f -print`

file_missing=0

echo "Следующие файлы переводов отсутствуют:"
for eng_file in $eng_files
do
    ru_file=$ru_translation${eng_file:${#eng_translation}}
    if [ ! -f "$ru_file" ]; then
        file_missing=1
        echo $ru_file
    fi
done

echo ==================
echo

echo В следующих файлах отсутствуют строки переводов:
translation_missing=0

for eng_file in $eng_files
do
    ru_file=$ru_translation${eng_file:${#eng_translation}}
    if [ ! -f "$ru_file" ]; then
        continue
    fi

    while read -r eng_file_line
    do
        eng_file_line=`sed 's/^ *//' <<<  "$eng_file_line"`
        # if contain only spaces or comment
        if [[ -z "${eng_file_line// }" ]] \
            || [[ "$eng_file_line" == "#"* ]] \
            || [[ "$eng_file_line" == "*"* ]] \
            || [[ "$eng_file_line" == "["* ]] \
            || [[ ! "$eng_file_line" == *" ="* ]]; then
            continue
        fi

        translation_key=`cut -d'=' -f 1 <<< $eng_file_line`
        translation_key=${translation_key// }
        if ! grep -q "$translation_key" "$ru_file"; then
            echo $eng_file ">" $ru_file : $translation_key
            translation_missing=1
        fi
    done < "$eng_file"
done

echo ==================
echo

if [[ $translation_missing == "1" ]] || [[ $file_missing == "1" ]]; then
    exit 1
fi
