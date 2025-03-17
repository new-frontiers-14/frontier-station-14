import os
import re
import chardet
from datetime import datetime

def find_top_level_dir(start_dir):
    marker_file = 'SpaceStation14.sln'
    current_dir = os.path.abspath(start_dir)  # Убедимся, что путь абсолютный
    while True:
        if os.path.isfile(os.path.join(current_dir, marker_file)):
            return current_dir
        parent_dir = os.path.dirname(current_dir)
        if parent_dir == current_dir:  # Корень достигнут
            print(f"Не удалось найти {marker_file}, начиная с {start_dir}")
            exit(-1)
        current_dir = parent_dir

def find_ftl_files(root_dir):
    if not os.path.isdir(root_dir):
        print(f"Директория {root_dir} не найдена.")
        return []
    ftl_files = []
    for root, _, files in os.walk(root_dir):
        for file in files:
            if file.endswith('.ftl'):
                ftl_files.append(os.path.join(root, file))
    return ftl_files

def detect_encoding(file_path):
    try:
        with open(file_path, 'rb') as file:
            raw_data = file.read()
        result = chardet.detect(raw_data)
        return result['encoding'] or 'utf-8'  # Если encoding None, используем utf-8
    except Exception as e:
        print(f"Ошибка определения кодировки для {file_path}: {e}")
        return 'utf-8'  # Запасной вариант

def parse_ent_blocks(file_path):
    try:
        encoding = detect_encoding(file_path)
        with open(file_path, 'r', encoding=encoding) as file:
            content = file.read()
    except UnicodeDecodeError:
        print(f"Ошибка декодирования {file_path}. Попытка с UTF-8.")
        try:
            with open(file_path, 'r', encoding='utf-8') as file:
                content = file.read()
        except UnicodeDecodeError:
            print(f"Не удалось прочитать {file_path}. Пропускаем.")
            return {}
    except Exception as e:
        print(f"Ошибка чтения {file_path}: {e}")
        return {}

    ent_blocks = {}
    current_ent = None
    current_block = []

    for line in content.splitlines():  # Используем splitlines для единообразия
        line = line.rstrip()  # Удаляем лишние пробелы справа
        if line.startswith('ent-'):
            if current_ent:
                ent_blocks[current_ent] = '\n'.join(current_block)
            current_ent = line.split('=', 1)[0].strip()  # Безопасное разделение
            current_block = [line]
        elif current_ent and (line.startswith('.desc') or line.startswith('.suffix')):
            current_block.append(line)
        elif not line:  # Пустая строка
            if current_ent:
                ent_blocks[current_ent] = '\n'.join(current_block)
                current_ent = None
                current_block = []
        elif current_ent:  # Любая другая строка завершает блок
            ent_blocks[current_ent] = '\n'.join(current_block)
            current_ent = None
            current_block = []

    if current_ent:  # Не забываем последний блок
        ent_blocks[current_ent] = '\n'.join(current_block)

    return ent_blocks

def remove_duplicates(root_dir):
    ftl_files = find_ftl_files(root_dir)
    if not ftl_files:
        print("Файлы .ftl не найдены.")
        return

    all_ents = {}
    removed_duplicates = []

    # Собираем все уникальные ent-блоки
    for file_path in ftl_files:
        ent_blocks = parse_ent_blocks(file_path)
        for ent, block in ent_blocks.items():
            if ent not in all_ents:
                all_ents[ent] = (file_path, block)

    # Удаляем дубликаты
    for file_path in ftl_files:
        try:
            encoding = detect_encoding(file_path)
            with open(file_path, 'r', encoding=encoding) as file:
                content = file.read()

            ent_blocks = parse_ent_blocks(file_path)
            modified = False
            for ent, block in ent_blocks.items():
                if all_ents[ent][0] != file_path:  # Это дубликат
                    content = content.replace(block + '\n', '')  # Удаляем с учетом новой строки
                    removed_duplicates.append((ent, file_path, block))
                    modified = True

            if modified:
                # Убираем лишние пустые строки
                content = re.sub(r'\n{2,}', '\n', content).strip() + '\n'
                with open(file_path, 'w', encoding=encoding) as file:
                    file.write(content)
        except Exception as e:
            print(f"Ошибка обработки {file_path}: {e}")

    # Лог удаленных дубликатов
    if removed_duplicates:
        log_filename = f"removed_duplicates_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log"
        try:
            with open(log_filename, 'w', encoding='utf-8') as log_file:
                for ent, file_path, block in removed_duplicates:
                    log_file.write(f"Удален дубликат: {ent}\n")
                    log_file.write(f"Файл: {file_path}\n")
                    log_file.write("Содержимое:\n")
                    log_file.write(block)
                    log_file.write("\n\n")
            print(f"Лог сохранен в: {log_filename}")
        except Exception as e:
            print(f"Ошибка записи лога: {e}")
    else:
        print("Дубликаты не найдены.")

    print(f"Обработка завершена. Проверено файлов: {len(ftl_files)}")

if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    main_folder = find_top_level_dir(script_dir)
    root_dir = os.path.join(main_folder, "Resources", "Locale", "ru-RU")  # Используем прямые слэши для совместимости
    remove_duplicates(root_dir)