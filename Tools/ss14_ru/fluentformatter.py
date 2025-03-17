#!/usr/bin/env python3

# Форматтер, приводящий fluent-файлы (.ftl) к единому стилю.
# Использует путь к папке с файлами для форматирования. Для обработки всего проекта замените путь на root_dir_path.

import typing
from fluent.syntax import ast, FluentParser, FluentSerializer
from file import FluentFile
from project import Project


class FluentFormatter:
    @classmethod
    def format(cls, fluent_files: typing.List[FluentFile]) -> None:
        """
        Форматирует список Fluent-файлов и сохраняет изменения.

        Args:
            fluent_files: Список объектов FluentFile для форматирования.
        """
        for file in fluent_files:
            try:
                file_data = file.read_data()
                parsed_file_data = file.parse_data(file_data)
                serialized_file_data = file.serialize_data(parsed_file_data)
                file.save_data(serialized_file_data)
            except Exception as e:
                print(f"Ошибка при форматировании файла {file.full_path}: {e}")

    @classmethod
    def format_serialized_file_data(cls, file_data: str) -> str:
        """
        Форматирует строковые данные Fluent и возвращает сериализованный результат.

        Args:
            file_data: Строковые данные в формате Fluent.

        Returns:
            Отформатированная строка в формате Fluent.
        """
        try:
            parsed_data = FluentParser().parse(file_data)
            return FluentSerializer(with_junk=True).serialize(parsed_data)
        except Exception as e:
            raise ValueError(f"Ошибка форматирования данных: {e}")


# Инициализация проекта и получение файлов
project = Project()
fluent_files = project.get_fluent_files_by_dir(project.ru_locale_dir_path)

# Запуск форматирования
if __name__ == "__main__":
    FluentFormatter.format(fluent_files)
    print("Форматирование завершено.")