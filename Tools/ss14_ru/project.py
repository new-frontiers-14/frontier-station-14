import pathlib
import os
import glob
from file import FluentFile


class Project:
    def __init__(self):
        """
        Инициализирует объект Project с путями к основным директориям проекта.
        """
        # Базовая директория проекта (два уровня вверх от текущей)
        self.base_dir_path = pathlib.Path(os.path.abspath(os.curdir)).parent.parent.resolve()
        self.resources_dir_path = os.path.join(self.base_dir_path, 'Resources')
        self.locales_dir_path = os.path.join(self.resources_dir_path, 'Locale')
        self.ru_locale_dir_path = os.path.join(self.locales_dir_path, 'ru-RU')
        self.en_locale_dir_path = os.path.join(self.locales_dir_path, 'en-US')
        self.prototypes_dir_path = os.path.join(self.resources_dir_path, "Prototypes")
        self.en_locale_prototypes_dir_path = os.path.join(self.en_locale_dir_path, 'ss14-ru', 'prototypes')
        self.ru_locale_prototypes_dir_path = os.path.join(self.ru_locale_dir_path, 'ss14-ru', 'prototypes')

    def get_files_paths_by_dir(self, dir_path: str, files_extension: str) -> list[str]:
        """
        Возвращает список путей к файлам с указанным расширением в заданной директории.

        Args:
            dir_path: Путь к директории для поиска.
            files_extension: Расширение файлов (без точки, например, 'ftl').

        Returns:
            Список строк с полными путями к файлам.
        """
        return glob.glob(f"{dir_path}/**/*.{files_extension}", recursive=True)

    def get_fluent_files_by_dir(self, dir_path: str) -> list[FluentFile]:
        """
        Возвращает список объектов FluentFile для всех .ftl файлов в директории.

        Args:
            dir_path: Путь к директории для поиска .ftl файлов.

        Returns:
            Список объектов FluentFile.
        """
        files = []
        files_paths_list = glob.glob(f"{dir_path}/**/*.ftl", recursive=True)

        for file_path in files_paths_list:
            try:
                files.append(FluentFile(file_path))
            except Exception as e:
                print(f"Ошибка создания FluentFile для {file_path}: {e}")
                continue

        return files


# Пример использования
if __name__ == "__main__":
    project = Project()
    print(f"Base dir: {project.base_dir_path}")
    print(f"RU locale dir: {project.ru_locale_dir_path}")
    print(f"EN locale dir: {project.en_locale_dir_path}")

    # Тестовый поиск .ftl файлов (предполагая наличие FluentFile)
    fluent_files = project.get_fluent_files_by_dir(project.ru_locale_dir_path)
    print(f"Найдено .ftl файлов: {len(fluent_files)}")
    for file in fluent_files[:5]:  # Первые 5 для примера
        print(f"Файл: {file.full_path}")