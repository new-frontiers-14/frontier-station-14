import os
import logging
from fluent.syntax import FluentParser, FluentSerializer
from file import YAMLFile, FluentFile
from fluentast import FluentSerializedMessage, FluentAstAttributeFactory
from fluentformatter import FluentFormatter
from project import Project


class YAMLExtractor:
    def __init__(self, yaml_files: list[YAMLFile]):
        """
        Инициализирует извлекатель данных из YAML-файлов для создания Fluent-файлов.

        Args:
            yaml_files: Список объектов YAMLFile.
        """
        self.yaml_files = yaml_files

    def execute(self) -> None:
        """
        Извлекает данные из YAML-файлов и создаёт соответствующие .ftl файлы для en-US и ru-RU.
        """
        for yaml_file in self.yaml_files:
            try:
                yaml_elements = yaml_file.get_elements(yaml_file.parse_data(yaml_file.read_data()))
                if not yaml_elements:
                    continue

                fluent_file_serialized = self.get_serialized_fluent_from_yaml_elements(yaml_elements)
                if not fluent_file_serialized:
                    continue

                pretty_fluent_file_serialized = FluentFormatter.format_serialized_file_data(fluent_file_serialized)
                relative_parent_dir = yaml_file.get_relative_parent_dir(project.prototypes_dir_path).lower()
                file_name = yaml_file.get_name()

                en_fluent_file_path = self.create_en_fluent_file(relative_parent_dir, file_name, pretty_fluent_file_serialized)
                self.create_ru_fluent_file(en_fluent_file_path)

            except Exception as e:
                logging.error(f"Ошибка обработки файла {yaml_file.full_path}: {e}")

    @classmethod
    def serialize_yaml_element(cls, element: typing.Any) -> typing.Optional[str]:
        """
        Сериализует элемент YAML в строку формата Fluent.

        Args:
            element: Элемент YAML с атрибутами id, name, parent_id и др.

        Returns:
            Сериализованная строка или None, если сериализация невозможна.
        """
        parent_id = element.parent_id
        if isinstance(parent_id, list):
            parent_id = parent_id[0] if parent_id else None  # Упрощена логика обработки списка

        return FluentSerializedMessage.from_yaml_element(
            element.id,
            element.name,
            FluentAstAttributeFactory.from_yaml_element(element),
            parent_id
        )

    def get_serialized_fluent_from_yaml_elements(self, yaml_elements: list) -> typing.Optional[str]:
        """
        Преобразует список YAML-элементов в сериализованную Fluent-строку.

        Args:
            yaml_elements: Список элементов YAML.

        Returns:
            Сериализованная строка Fluent или None, если нет валидных сообщений.
        """
        fluent_serialized_messages = [self.serialize_yaml_element(el) for el in yaml_elements]
        valid_messages = [m for m in fluent_serialized_messages if m]
        return '\n'.join(valid_messages) if valid_messages else None

    def create_en_fluent_file(self, relative_parent_dir: str, file_name: str, file_data: str) -> str:
        """
        Создаёт английский .ftl файл и сохраняет в него данные.

        Args:
            relative_parent_dir: Относительный путь к родительской директории.
            file_name: Имя файла без расширения.
            file_data: Сериализованные данные Fluent.

        Returns:
            Полный путь к созданному файлу.
        """
        en_new_dir_path = os.path.join(project.en_locale_prototypes_dir_path, relative_parent_dir)
        en_fluent_file = FluentFile(os.path.join(en_new_dir_path, f"{file_name}.ftl"))
        en_fluent_file.save_data(file_data)
        return en_fluent_file.full_path

    def create_ru_fluent_file(self, en_analog_file_path: str) -> typing.Optional[str]:
        """
        Создаёт русский .ftl файл, если он ещё не существует, копируя данные из английского.

        Args:
            en_analog_file_path: Путь к английскому файлу.

        Returns:
            Полный путь к русскому файлу или None, если файл уже существует.
        """
        ru_file_full_path = en_analog_file_path.replace('en-US', 'ru-RU')
        if os.path.isfile(ru_file_full_path):
            return None

        try:
            en_file = FluentFile(en_analog_file_path)
            ru_file = FluentFile(ru_file_full_path)
            ru_file.save_data(en_file.read_data())
            logging.info(f"Создан файл русской локали {ru_file_full_path}")
            return ru_file_full_path
        except Exception as e:
            logging.error(f"Ошибка создания русского файла {ru_file_full_path}: {e}")
            return None


# Настройка и выполнение
if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s")
    project = Project()
    formatter = FluentFormatter()
    yaml_files_paths = project.get_files_paths_by_dir(project.prototypes_dir_path, 'yml')
    yaml_files = [YAMLFile(path) for path in yaml_files_paths]

    logging.info("Поиск YAML-файлов...")
    YAMLExtractor(yaml_files).execute()
    logging.info("Обработка завершена.")