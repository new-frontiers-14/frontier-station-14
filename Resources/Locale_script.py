import os

def find_files_with_tabs(directory, output_file):
    files_with_tabs = []

    # Проходим по всем файлам в указанной директории и её поддиректориях
    for root, dirs, files in os.walk(directory):
        for file in files:
            file_path = os.path.join(root, file)
            try:
                # Открываем файл и проверяем наличие табуляции
                with open(file_path, 'r', encoding='utf-8') as f:
                    for line in f:
                        if '\t' in line:
                            files_with_tabs.append(file_path)
                            break
            except Exception as e:
                # Если файл не удалось открыть (например, двоичный файл), игнорируем его
                print(f"Не удалось открыть файл {file_path}: {e}")

    # Сохраняем пути к файлам с табуляцией в текстовый документ
    with open(output_file, 'w', encoding='utf-8') as out_file:
        for file_path in files_with_tabs:
            out_file.write(file_path + '\n')

# Указываем директорию и файл для сохранения результатов
directory = 'Locale'
output_file = 'files_with_tabs.txt'

# Запускаем функцию
find_files_with_tabs(directory, output_file)