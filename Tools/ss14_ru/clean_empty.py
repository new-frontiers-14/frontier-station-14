import os
import logging
from datetime import datetime

def find_top_level_dir(start_dir):
    marker_file = 'SpaceStation14.sln'
    current_dir = os.path.abspath(start_dir)  # Абсолютный путь для надежности
    while True:
        if os.path.isfile(os.path.join(current_dir, marker_file)):  # Проверяем существование файла
            return current_dir
        parent_dir = os.path.dirname(current_dir)
        if parent_dir == current_dir:  # Достигнут корень файловой системы
            logging.error(f"Не удалось найти {marker_file}, начиная с {start_dir}")
            exit(-1)
        current_dir = parent_dir

def setup_logging():
    log_filename = f"cleanup_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log"
    logging.basicConfig(
        filename=log_filename,
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s',
        encoding='utf-8'  # Явно указываем кодировку для избежания проблем на разных платформах
    )
    console = logging.StreamHandler()
    console.setLevel(logging.INFO)
    logging.getLogger('').addHandler(console)
    return log_filename

def remove_empty_files_and_folders(path):
    if not os.path.isdir(path):
        logging.error(f"Директория {path} не существует.")
        return 0, 0

    removed_files = 0
    removed_folders = 0

    # Проходим по директориям снизу вверх
    for root, dirs, files in os.walk(path, topdown=False):
        # Удаление пустых файлов
        for file in files:
            file_path = os.path.join(root, file)
            try:
                if os.path.getsize(file_path) == 0:
                    os.remove(file_path)
                    logging.info(f"Удален пустой файл: {file_path}")
                    removed_files += 1
            except PermissionError as e:
                logging.error(f"Нет прав для удаления файла {file_path}: {e}")
            except FileNotFoundError:
                logging.warning(f"Файл {file_path} уже удален или недоступен.")
            except Exception as e:
                logging.error(f"Ошибка при удалении файла {file_path}: {e}")

        # Удаление пустых папок
        try:
            if not os.listdir(root):  # Проверяем, пуста ли папка
                os.rmdir(root)
                logging.info(f"Удалена пустая папка: {root}")
                removed_folders += 1
        except PermissionError as e:
            logging.error(f"Нет прав для удаления папки {root}: {e}")
        except FileNotFoundError:
            logging.warning(f"Папка {root} уже удалена или недоступна.")
        except Exception as e:
            logging.error(f"Ошибка при удалении папки {root}: {e}")

    return removed_files, removed_folders

if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    main_folder = find_top_level_dir(script_dir)
    root_dir = os.path.join(main_folder, "Resources", "Locale")  # Используем прямые слэши для совместимости

    log_file = setup_logging()
    logging.info(f"Начало очистки в директории: {root_dir}")
    
    files_removed, folders_removed = remove_empty_files_and_folders(root_dir)
    logging.info(f"Очистка завершена. Удалено файлов: {files_removed}, удалено папок: {folders_removed}")
    print(f"Лог операций сохранен в файл: {log_file}")