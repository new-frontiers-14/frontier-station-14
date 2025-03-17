import typing
import os
import re
from fluent.syntax import ast, FluentParser, FluentSerializer
import yaml


class File:
    def __init__(self, full_path: str):
        self.full_path = os.path.abspath(full_path)  # Абсолютный путь для надежности

    def read_data(self) -> str:
        try:
            with open(self.full_path, 'r', encoding='utf-8') as file:
                # Удаляем BOM (Byte Order Mark), если присутствует
                return file.read().replace('\ufeff', '')
        except FileNotFoundError:
            raise FileNotFoundError(f"Файл {self.full_path} не найден")
        except Exception as e:
            raise RuntimeError(f"Ошибка чтения файла {self.full_path}: {e}")

    def save_data(self, file_data: str) -> None:
        try:
            os.makedirs(os.path.dirname(self.full_path), exist_ok=True)
            with open(self.full_path, 'w', encoding='utf-8') as file:
                file.write(file_data)
        except Exception as e:
            raise RuntimeError(f"Ошибка записи в файл {self.full_path}: {e}")

    def get_relative_path(self, base_path: str) -> str:
        return os.path.relpath(self.full_path, base_path)

    def get_relative_path_without_extension(self, base_path: str) -> str:
        return self.get_relative_path(base_path).split('.', maxsplit=1)[0]

    def get_relative_parent_dir(self, base_path: str) -> str:
        return os.path.relpath(self.get_parent_dir(), base_path)

    def get_parent_dir(self) -> str:
        return os.path.dirname(self.full_path)

    def get_name(self) -> str:
        return os.path.basename(self.full_path).split('.')[0]


class FluentFile(File):
    def __init__(self, full_path: str):
        super().__init__(full_path)
        self.newline_exceptions_regex = re.compile(r"^\s*[\[\]{}#%^*]")  # Регулярка для исключений
        self.newline_remover_tag = "%ERASE_NEWLINE%"  # Тег для удаления новых строк
        self.newline_remover_regex = re.compile(r"\n?\s*" + self.newline_remover_tag)

    def parse_data(self, file_data: str) -> ast.Resource:
        parser = FluentParser()
        parsed_data = parser.parse(file_data)

        # Обработка элементов в теле файла
        for body_element in parsed_data.body:
            if not isinstance(body_element, (ast.Term, ast.Message)):
                continue

            if not body_element.value or not len(body_element.value.elements):
                continue

            first_element = body_element.value.elements[0]
            if not isinstance(first_element, ast.TextElement):
                continue

            if self.newline_exceptions_regex.match(first_element.value):
                first_element.value = f"{self.newline_remover_tag}{first_element.value}"

        return parsed_data

    def serialize_data(self, parsed_file_data: ast.Resource) -> str:
        serializer = FluentSerializer(with_junk=True)
        serialized_data = serializer.serialize(parsed_file_data)
        # Удаляем тег и лишние пробелы/новые строки
        serialized_data = self.newline_remover_regex.sub(' ', serialized_data)
        return serialized_data

    def read_serialized_data(self) -> str:
        return self.serialize_data(self.parse_data(self.read_data()))

    def read_parsed_data(self) -> ast.Resource:
        return self.parse_data(self.read_data())


class YAMLFile(File):
    def __init__(self, full_path: str):
        super().__init__(full_path)

    def parse_data(self, file_data: str) -> typing.Any:
        try:
            return yaml.load(file_data, Loader=yaml.SafeLoader)  # SafeLoader для безопасности
        except yaml.YAMLError as e:
            raise RuntimeError(f"Ошибка парсинга YAML в файле {self.full_path}: {e}")

    def get_elements(self, parsed_data: typing.Any) -> list:
        from yamlmodels import YAMLElements  # Импорт здесь, если он кастомный

        if isinstance(parsed_data, list):
            elements = YAMLElements(parsed_data).elements
            # Фильтруем None элементы
            return [el for el in elements if el is not None]
        return []


class YAMLFluentFileAdapter(File):
    def __init__(self, full_path: str):
        super().__init__(full_path)

    # Закомментированный метод оставлен как заглушка для будущей реализации
    # def create_fluent_from_yaml_elements(self, yaml_elements):
    #     pass


# Пример использования (для тестирования)
if __name__ == "__main__":
    # Тест для FluentFile
    fluent_file = FluentFile("test.ftl")
    try:
        with open("test.ftl", "w", encoding="utf-8") as f:
            f.write("test-message = Hello\n  [variant] World")
        serialized = fluent_file.read_serialized_data()
        print("Serialized Fluent data:", serialized)
    except Exception as e:
        print(f"Ошибка: {e}")

    # Тест для YAMLFile
    yaml_file = YAMLFile("test.yml")
    try:
        with open("test.yml", "w", encoding="utf-8") as f:
            f.write("- name: Item\n  desc: Description")
        parsed = yaml_file.read_parsed_data()
        elements = yaml_file.get_elements(parsed)
        print("YAML elements:", elements)
    except Exception as e:
        print(f"Ошибка: {e}")