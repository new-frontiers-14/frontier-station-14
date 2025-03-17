#!/usr/bin/env python3

# Скрипт актуализирует русскоязычные .ftl файлы на основе английских.
# Создает недостающие файлы и ключи, логирует различия.

import typing
import logging
from pydash import py_
from file import FluentFile
from fluentast import FluentAstAbstract
from fluentformatter import FluentFormatter
from project import Project
from fluent.syntax import ast, FluentParser, FluentSerializer


class RelativeFile:
    def __init__(self, file: FluentFile, locale: str, relative_path_from_locale: str):
        self.file = file
        self.locale = locale
        self.relative_path_from_locale = relative_path_from_locale


class FilesFinder:
    def __init__(self, project: Project):
        self.project = project
        self.created_files: typing.List[FluentFile] = []

    def get_relative_path_dict(self, file: FluentFile, locale: str) -> RelativeFile:
        if locale == 'ru-RU':
            return RelativeFile(file=file, locale=locale,
                               relative_path_from_locale=file.get_relative_path(self.project.ru_locale_dir_path))
        elif locale == 'en-US':
            return RelativeFile(file=file, locale=locale,
                               relative_path_from_locale=file.get_relative_path(self.project.en_locale_dir_path))
        raise ValueError(f"Локаль {locale} не поддерживается")

    def get_file_pair(self, en_file: FluentFile) -> typing.Tuple[FluentFile, FluentFile]:
        ru_file_path = en_file.full_path.replace('en-US', 'ru-RU')
        return en_file, FluentFile(ru_file_path)

    def execute(self) -> typing.List[FluentFile]:
        self.created_files = []
        groups = self.get_files_pairs()
        keys_without_pair = [k for k, g in groups.items() if len(g) < 2]

        for key in keys_without_pair:
            relative_file = groups[key][0]
            if relative_file.locale == 'en-US':
                ru_file = self.create_ru_analog(relative_file)
                self.created_files.append(ru_file)
            elif relative_file.locale == 'ru-RU':
                is_engine_files = "robust-toolbox" in relative_file.file.full_path
                is_corvax_files = "corvax" in relative_file.file.full_path
                if not is_engine_files and not is_corvax_files:
                    self.warn_en_analog_not_exist(relative_file)

        return self.created_files

    def get_files_pairs(self) -> dict:
        en_files = self.project.get_fluent_files_by_dir(self.project.en_locale_dir_path)
        ru_files = self.project.get_fluent_files_by_dir(self.project.ru_locale_dir_path)

        en_relative_files = [self.get_relative_path_dict(f, 'en-US') for f in en_files]
        ru_relative_files = [self.get_relative_path_dict(f, 'ru-RU') for f in ru_files]
        return py_.group_by(en_relative_files + ru_relative_files, 'relative_path_from_locale')

    def create_ru_analog(self, en_relative_file: RelativeFile) -> FluentFile:
        en_file = en_relative_file.file
        try:
            en_file_data = en_file.read_data()
            ru_file_path = en_file.full_path.replace('en-US', 'ru-RU')
            ru_file = FluentFile(ru_file_path)
            ru_file.save_data(en_file_data)
            logging.info(f"Создан файл {ru_file_path} с переводами из английского файла")
            return ru_file
        except Exception as e:
            logging.error(f"Ошибка создания файла {ru_file_path}: {e}")
            raise

    def warn_en_analog_not_exist(self, ru_relative_file: RelativeFile) -> None:
        file = ru_relative_file.file
        en_file_path = file.full_path.replace('ru-RU', 'en-US')
        logging.warning(f"Файл {file.full_path} не имеет английского аналога по пути {en_file_path}")


class KeyFinder:
    def __init__(self, files_dict: dict):
        self.files_dict = files_dict
        self.changed_files: typing.List[FluentFile] = []

    def execute(self) -> typing.List[FluentFile]:
        self.changed_files = []
        for pair in self.files_dict:
            ru_relative = py_.find(self.files_dict[pair], {'locale': 'ru-RU'})
            en_relative = py_.find(self.files_dict[pair], {'locale': 'en-US'})
            if not en_relative or not ru_relative:
                continue
            self.compare_files(en_relative.file, ru_relative.file)
        return self.changed_files

    def compare_files(self, en_file: FluentFile, ru_file: FluentFile) -> None:
        try:
            ru_file_parsed = ru_file.parse_data(ru_file.read_data())
            en_file_parsed = en_file.parse_data(en_file.read_data())
            self.write_to_ru_files(ru_file, ru_file_parsed, en_file_parsed)
            self.log_not_exist_en_files(en_file, ru_file_parsed, en_file_parsed)
        except Exception as e:
            logging.error(f"Ошибка сравнения файлов {en_file.full_path} и {ru_file.full_path}: {e}")

    def write_to_ru_files(self, ru_file: FluentFile, ru_file_parsed: ast.Resource, en_file_parsed: ast.Resource) -> None:
        for idx, en_message in enumerate(en_file_parsed.body):
            if isinstance(en_message, (ast.ResourceComment, ast.GroupComment, ast.Comment)):
                continue

            ru_idx = py_.find_index(ru_file_parsed.body, lambda m: self._is_duplicate_message(m, en_message))
            have_changes = False

            if ru_idx != -1 and getattr(en_message, 'attributes', None):
                ru_msg = ru_file_parsed.body[ru_idx]
                if not ru_msg.attributes:
                    ru_msg.attributes = en_message.attributes
                    have_changes = True
                else:
                    for en_attr in en_message.attributes:
                        if not py_.find(ru_msg.attributes, lambda a: a.id.name == en_attr.id.name):
                            ru_msg.attributes.append(en_attr)
                            have_changes = True

            if ru_idx == -1:
                ru_file_parsed = (self.append_message(ru_file_parsed, en_message, idx)
                                 if len(ru_file_parsed.body) >= idx + 1 else
                                 self.push_message(ru_file_parsed, en_message))
                have_changes = True

            if have_changes:
                serialized = FluentSerializer(with_junk=True).serialize(ru_file_parsed)
                self.save_and_log_file(ru_file, serialized, en_message)

    def log_not_exist_en_files(self, en_file: FluentFile, ru_file_parsed: ast.Resource, en_file_parsed: ast.Resource) -> None:
        for ru_message in ru_file_parsed.body:
            if isinstance(ru_message, (ast.ResourceComment, ast.GroupComment, ast.Comment)):
                continue
            if not py_.find(en_file_parsed.body, lambda m: self._is_duplicate_message(m, ru_message)):
                key = FluentAstAbstract.get_id_name(ru_message)
                logging.warning(f"Ключ '{key}' не имеет английского аналога в {en_file.full_path}")

    def append_message(self, ru_file_parsed: ast.Resource, en_message: ast.Message, idx: int) -> ast.Resource:
        ru_file_parsed.body = ru_file_parsed.body[:idx] + [en_message] + ru_file_parsed.body[idx:]
        return ru_file_parsed

    def push_message(self, ru_file_parsed: ast.Resource, en_message: ast.Message) -> ast.Resource:
        ru_file_parsed.body.append(en_message)
        return ru_file_parsed

    def save_and_log_file(self, file: FluentFile, file_data: str, message: ast.Message) -> None:
        file.save_data(file_data)
        key = FluentAstAbstract.get_id_name(message)
        logging.info(f"В файл {file.full_path} добавлен ключ '{key}'")
        self.changed_files.append(file)

    def _is_duplicate_message(self, ru_message: ast.Message, en_message: ast.Message) -> bool:
        ru_id = FluentAstAbstract.get_id_name(ru_message)
        en_id = FluentAstAbstract.get_id_name(en_message)
        return ru_id == en_id and ru_id is not None


# Настройка и выполнение
if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
    project = Project()
    files_finder = FilesFinder(project)
    key_finder = KeyFinder(files_finder.get_files_pairs())

    print("Проверка актуальности файлов...")
    created_files = files_finder.execute()
    if created_files:
        print("Форматирование созданных файлов...")
        FluentFormatter.format(created_files)

    print("Проверка актуальности ключей...")
    changed_files = key_finder.execute()
    if changed_files:
        print("Форматирование изменённых файлов...")
        FluentFormatter.format(changed_files)
    print("Процесс завершён.")